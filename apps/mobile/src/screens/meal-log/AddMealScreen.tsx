import React, { useCallback, useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';
import { FoodSearchResultRow } from '@/components/meal/FoodSearchResultRow';
import { CustomFoodModal } from '@/components/meal/CustomFoodModal';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { useAuthStore } from '@/store/authStore';
import { useMealStore } from '@/store/mealStore';
import { searchFoodItems } from '@/services/foodItemService';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import { MealType, type FoodItemDto } from '@/types';
import type { MealLogStackParamList } from '@/navigation/types';

type Props = NativeStackScreenProps<MealLogStackParamList, 'AddMeal'>;

const MEAL_TYPES = [
  { value: MealType.Breakfast, label: 'Breakfast' },
  { value: MealType.MorningSnack, label: 'AM Snack' },
  { value: MealType.Lunch, label: 'Lunch' },
  { value: MealType.AfternoonSnack, label: 'PM Snack' },
  { value: MealType.Dinner, label: 'Dinner' },
  { value: MealType.EveningSnack, label: 'Evening' },
];

interface SelectedItem {
  foodItem: FoodItemDto;
  quantity: number;
}

export function AddMealScreen({ navigation, route }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const user = useAuthStore((state) => state.user);
  const { logMeal, updateMealLog, selectedDate, isLoading } = useMealStore();
  const editingMealLog = route.params?.mealLog;

  const [selectedMealType, setSelectedMealType] = useState<MealType>(
    editingMealLog?.mealType ?? route.params?.mealType ?? MealType.Breakfast,
  );
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<FoodItemDto[]>([]);
  const [showCustomFood, setShowCustomFood] = useState(false);
  const [isSearching, setIsSearching] = useState(false);
  const [selectedItems, setSelectedItems] = useState<SelectedItem[]>(
    editingMealLog?.items.map((item) => ({
      foodItem: item.foodItem,
      quantity: item.quantity,
    })) ?? [],
  );
  const searchTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    setSelectedMealType(editingMealLog?.mealType ?? route.params?.mealType ?? MealType.Breakfast);
    setSelectedItems(
      editingMealLog?.items.map((item) => ({
        foodItem: item.foodItem,
        quantity: item.quantity,
      })) ?? [],
    );
  }, [editingMealLog, route.params?.mealType]);

  const performSearch = useCallback(async (query: string) => {
    if (!query.trim()) {
      setSearchResults([]);
      return;
    }
    setIsSearching(true);
    try {
      const results = await searchFoodItems(query.trim());
      setSearchResults(results);
    } catch {
      setSearchResults([]);
    } finally {
      setIsSearching(false);
    }
  }, []);

  useEffect(() => {
    if (searchTimer.current) {
      clearTimeout(searchTimer.current);
    }
    searchTimer.current = setTimeout(() => {
      performSearch(searchQuery);
    }, 300);
    return () => {
      if (searchTimer.current) {
        clearTimeout(searchTimer.current);
      }
    };
  }, [searchQuery, performSearch]);

  const handleSelectFood = (item: FoodItemDto) => {
    const exists = selectedItems.find((selectedItem) => selectedItem.foodItem.id === item.id);
    if (exists) {
      setSelectedItems((prev) => prev.filter((selectedItem) => selectedItem.foodItem.id !== item.id));
    } else {
      setSelectedItems((prev) => [...prev, { foodItem: item, quantity: item.servingSize || 1 }]);
    }
  };

  const updateQuantity = (foodId: string, delta: number) => {
    setSelectedItems((prev) =>
      prev.map((selectedItem) => {
        if (selectedItem.foodItem.id !== foodId) {
          return selectedItem;
        }

        const step = selectedItem.foodItem.servingSize > 0 ? Math.max(selectedItem.foodItem.servingSize / 2, 0.5) : 0.5;
        return {
          ...selectedItem,
          quantity: Math.max(step, Math.round((selectedItem.quantity + delta) * 100) / 100),
        };
      }),
    );
  };

  const removeItem = (foodId: string) => {
    setSelectedItems((prev) => prev.filter((selectedItem) => selectedItem.foodItem.id !== foodId));
  };

  const totalNutrition = selectedItems.reduce(
    (acc, selectedItem) => {
      const ratio = selectedItem.foodItem.servingSize > 0
        ? selectedItem.quantity / selectedItem.foodItem.servingSize
        : selectedItem.quantity;

      return {
        calories: acc.calories + selectedItem.foodItem.nutritionalInfo.calories * ratio,
        protein: acc.protein + selectedItem.foodItem.nutritionalInfo.protein * ratio,
        carbs: acc.carbs + selectedItem.foodItem.nutritionalInfo.carbohydrates * ratio,
        fat: acc.fat + selectedItem.foodItem.nutritionalInfo.fat * ratio,
      };
    },
    { calories: 0, protein: 0, carbs: 0, fat: 0 },
  );

  const handleSave = async () => {
    if (!user || selectedItems.length === 0) {
      Alert.alert('No items', 'Please select at least one food item.');
      return;
    }

    const payload = {
      userId: user.id,
      logDate: selectedDate,
      mealType: selectedMealType,
      items: selectedItems.map((selectedItem) => ({
        foodItemId: selectedItem.foodItem.id,
        quantity: selectedItem.quantity,
        unit: selectedItem.foodItem.servingUnit,
      })),
    };

    try {
      if (editingMealLog) {
        await updateMealLog(editingMealLog.id, payload);
      } else {
        await logMeal(payload);
      }
      navigation.goBack();
    } catch {
      Alert.alert('Error', `Failed to ${editingMealLog ? 'update' : 'log'} meal. Please try again.`);
    }
  };

  const isItemSelected = (id: string) => selectedItems.some((selectedItem) => selectedItem.foodItem.id === id);

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.mealTypeRow}
          style={styles.mealTypeScroll}
        >
          {MEAL_TYPES.map((mealTypeOption) => (
            <TouchableOpacity
              key={mealTypeOption.value}
              style={[
                styles.mealTypeChip,
                selectedMealType === mealTypeOption.value && styles.mealTypeChipActive,
              ]}
              onPress={() => setSelectedMealType(mealTypeOption.value)}
            >
              <Text
                style={[
                  styles.mealTypeLabel,
                  selectedMealType === mealTypeOption.value && styles.mealTypeLabelActive,
                ]}
              >
                {mealTypeOption.label}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>

        <View style={styles.searchBar}>
          <Ionicons name="search" size={20} color={C.textSecondary} />
          <TextInput
            style={styles.searchInput}
            value={searchQuery}
            onChangeText={setSearchQuery}
            placeholder="Search food items..."
            placeholderTextColor={C.textHint}
            autoCorrect={false}
            clearButtonMode="while-editing"
          />
          {isSearching && <ActivityIndicator size="small" color={C.primary} />}
        </View>

        <View style={styles.body}>
          {(searchQuery.trim().length > 0 || isSearching) && (
            <View style={styles.resultsContainer}>
              <Text style={styles.sectionLabel}>
                {isSearching ? 'Searching...' : `${searchResults.length} results`}
              </Text>
              <FlatList
                data={searchResults}
                keyExtractor={(item) => item.id}
                renderItem={({ item }) => (
                  <FoodSearchResultRow
                    item={item}
                    onSelect={handleSelectFood}
                    isSelected={isItemSelected(item.id)}
                  />
                )}
                style={styles.resultsList}
                keyboardShouldPersistTaps="handled"
              />
              {/* Homemade food will never be in a nutrition database, so offer to enter it. */}
              {!isSearching && (
                <TouchableOpacity
                  style={styles.addCustomButton}
                  onPress={() => setShowCustomFood(true)}
                >
                  <Ionicons name="add-circle-outline" size={18} color={C.primary} />
                  <Text style={styles.addCustomText}>
                    {searchResults.length === 0
                      ? `Can't find it? Add "${searchQuery.trim()}" as a homemade food`
                      : 'None of these? Add a homemade food'}
                  </Text>
                </TouchableOpacity>
              )}
            </View>
          )}

          {selectedItems.length > 0 && (
            <View style={styles.selectedSection}>
              <Text style={styles.sectionLabel}>Selected ({selectedItems.length})</Text>
              {selectedItems.map((selectedItem) => {
                const ratio = selectedItem.foodItem.servingSize > 0
                  ? selectedItem.quantity / selectedItem.foodItem.servingSize
                  : selectedItem.quantity;

                const step = selectedItem.foodItem.servingSize > 0
                  ? Math.max(selectedItem.foodItem.servingSize / 2, 0.5)
                  : 0.5;

                return (
                  <View key={selectedItem.foodItem.id} style={styles.selectedRow}>
                    <View style={styles.selectedLeft}>
                      <Text style={styles.selectedName} numberOfLines={1}>
                        {selectedItem.foodItem.name}
                      </Text>
                      <Text style={styles.selectedCal}>
                        {Math.round(selectedItem.foodItem.nutritionalInfo.calories * ratio)} kcal
                      </Text>
                    </View>
                    <View style={styles.quantityControl}>
                      <TouchableOpacity
                        onPress={() => updateQuantity(selectedItem.foodItem.id, -step)}
                        style={styles.qtyBtn}
                      >
                        <Text style={styles.qtyBtnText}>-</Text>
                      </TouchableOpacity>
                      <Text style={styles.qtyValue}>{selectedItem.quantity}</Text>
                      <TouchableOpacity
                        onPress={() => updateQuantity(selectedItem.foodItem.id, step)}
                        style={styles.qtyBtn}
                      >
                        <Text style={styles.qtyBtnText}>+</Text>
                      </TouchableOpacity>
                    </View>
                    <TouchableOpacity
                      onPress={() => removeItem(selectedItem.foodItem.id)}
                      style={styles.removeBtn}
                      hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                    >
                      <Ionicons name="close-circle" size={20} color={C.textHint} />
                    </TouchableOpacity>
                  </View>
                );
              })}

              <Card style={styles.nutritionPreview}>
                <Text style={styles.nutritionPreviewTitle}>Meal Totals</Text>
                <View style={styles.nutritionRow}>
                  {[
                    { label: 'Calories', value: `${Math.round(totalNutrition.calories)}`, unit: 'kcal', color: C.primary },
                    { label: 'Protein', value: `${Math.round(totalNutrition.protein)}`, unit: 'g', color: C.info },
                    { label: 'Carbs', value: `${Math.round(totalNutrition.carbs)}`, unit: 'g', color: C.secondary },
                    { label: 'Fat', value: `${Math.round(totalNutrition.fat)}`, unit: 'g', color: '#FFC107' },
                  ].map((macro) => (
                    <View key={macro.label} style={styles.nutritionItem}>
                      <Text style={[styles.nutritionValue, { color: macro.color }]}>
                        {macro.value}
                        <Text style={styles.nutritionUnit}>{macro.unit}</Text>
                      </Text>
                      <Text style={styles.nutritionLabel}>{macro.label}</Text>
                    </View>
                  ))}
                </View>
              </Card>
            </View>
          )}
        </View>

        <View style={styles.footer}>
          <Button
            title={editingMealLog ? 'Update Meal' : `Save ${selectedMealType.replace('_', ' ')}`}
            size="lg"
            fullWidth
            loading={isLoading}
            disabled={selectedItems.length === 0}
            onPress={handleSave}
          />
        </View>
      </KeyboardAvoidingView>
      <CustomFoodModal
        visible={showCustomFood}
        initialName={searchQuery.trim()}
        onClose={() => setShowCustomFood(false)}
        onCreated={(item) => {
          setShowCustomFood(false);
          handleSelectFood(item);
        }}
      />
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: C.background,
  },
  flex: {
    flex: 1,
  },
  mealTypeScroll: {
    flexGrow: 0,
    backgroundColor: C.surface,
    borderBottomWidth: 1,
    borderBottomColor: C.divider,
  },
  mealTypeRow: {
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
    gap: Spacing.sm,
  },
  mealTypeChip: {
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs + 2,
    borderRadius: BorderRadius.full,
    backgroundColor: C.background,
    borderWidth: 1.5,
    borderColor: C.divider,
  },
  mealTypeChipActive: {
    backgroundColor: C.primary,
    borderColor: C.primary,
  },
  mealTypeLabel: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.medium,
    color: C.textSecondary,
  },
  mealTypeLabelActive: {
    color: C.surface,
  },
  searchBar: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: C.surface,
    marginHorizontal: Spacing.md,
    marginVertical: Spacing.sm,
    paddingHorizontal: Spacing.md,
    borderRadius: BorderRadius.lg,
    borderWidth: 1,
    borderColor: C.divider,
    height: 48,
    gap: Spacing.sm,
  },
  searchInput: {
    flex: 1,
    fontSize: FontSize.md,
    color: C.text,
    height: '100%',
  },
  body: {
    flex: 1,
  },
  resultsContainer: {
    flex: 1,
  },
  sectionLabel: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.medium,
    color: C.textSecondary,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
  },
  addCustomButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.xs,
    borderWidth: 1,
    borderStyle: 'dashed',
    borderColor: C.primary,
    borderRadius: BorderRadius.md,
    paddingVertical: Spacing.sm,
    paddingHorizontal: Spacing.md,
    marginTop: Spacing.sm,
  },
  addCustomText: {
    fontSize: FontSize.sm,
    color: C.primary,
    fontWeight: FontWeight.medium,
    flexShrink: 1,
  },
  resultsList: {
    flex: 1,
  },
  selectedSection: {
    flex: 1,
    paddingHorizontal: Spacing.md,
  },
  selectedRow: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: C.surface,
    borderRadius: BorderRadius.md,
    padding: Spacing.sm,
    marginBottom: Spacing.xs,
    gap: Spacing.sm,
  },
  selectedLeft: {
    flex: 1,
  },
  selectedName: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.medium,
    color: C.text,
  },
  selectedCal: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
  },
  quantityControl: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.sm,
  },
  qtyBtn: {
    width: 28,
    height: 28,
    borderRadius: 14,
    backgroundColor: C.primaryLight,
    justifyContent: 'center',
    alignItems: 'center',
  },
  qtyBtnText: {
    fontSize: FontSize.lg,
    color: C.primaryDark,
    fontWeight: FontWeight.bold,
    lineHeight: 22,
  },
  qtyValue: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.semibold,
    color: C.text,
    minWidth: 36,
    textAlign: 'center',
  },
  removeBtn: {
    padding: 2,
  },
  nutritionPreview: {
    marginTop: Spacing.sm,
    marginBottom: Spacing.md,
    padding: Spacing.md,
  },
  nutritionPreviewTitle: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.semibold,
    color: C.text,
    marginBottom: Spacing.sm,
  },
  nutritionRow: {
    flexDirection: 'row',
    justifyContent: 'space-around',
  },
  nutritionItem: {
    alignItems: 'center',
  },
  nutritionValue: {
    fontSize: FontSize.xl,
    fontWeight: FontWeight.bold,
  },
  nutritionUnit: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.regular,
  },
  nutritionLabel: {
    fontSize: FontSize.xs,
    color: C.textSecondary,
    marginTop: 2,
  },
  footer: {
    padding: Spacing.md,
    backgroundColor: C.surface,
    borderTopWidth: 1,
    borderTopColor: C.divider,
  },
});
