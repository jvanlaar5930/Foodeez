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
import { useNavigation, useRoute } from '@react-navigation/native';
import type { NativeStackNavigationProp, NativeStackScreenProps } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';
import { FoodSearchResultRow } from '@/components/meal/FoodSearchResultRow';
import { NutritionBar } from '@/components/ui/NutritionBar';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { useAuthStore } from '@/store/authStore';
import { useMealStore } from '@/store/mealStore';
import { searchFoodItems } from '@/services/foodItemService';
import { Colors, BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { MealType, type FoodItemDto } from '@/types';
import { formatApiDate } from '@/utils/dateUtils';
import type { MealLogStackParamList } from '@/navigation/types';

type Props = NativeStackScreenProps<MealLogStackParamList, 'AddMeal'>;
type AddMealNav = NativeStackNavigationProp<MealLogStackParamList, 'AddMeal'>;

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

export function AddMealScreen() {
  const navigation = useNavigation<AddMealNav>();
  const route = useRoute<Props['route']>();
  const user = useAuthStore((state) => state.user);
  const { logMeal, selectedDate, isLoading } = useMealStore();

  const [selectedMealType, setSelectedMealType] = useState<MealType>(
    route.params?.mealType ?? MealType.Breakfast,
  );
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<FoodItemDto[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [selectedItems, setSelectedItems] = useState<SelectedItem[]>([]);
  const searchTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

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
    if (searchTimer.current) clearTimeout(searchTimer.current);
    searchTimer.current = setTimeout(() => {
      performSearch(searchQuery);
    }, 300);
    return () => {
      if (searchTimer.current) clearTimeout(searchTimer.current);
    };
  }, [searchQuery, performSearch]);

  const handleSelectFood = (item: FoodItemDto) => {
    const exists = selectedItems.find((s) => s.foodItem.id === item.id);
    if (exists) {
      setSelectedItems((prev) => prev.filter((s) => s.foodItem.id !== item.id));
    } else {
      setSelectedItems((prev) => [...prev, { foodItem: item, quantity: 1 }]);
    }
  };

  const updateQuantity = (foodId: string, delta: number) => {
    setSelectedItems((prev) =>
      prev.map((s) =>
        s.foodItem.id === foodId
          ? { ...s, quantity: Math.max(0.5, s.quantity + delta) }
          : s,
      ),
    );
  };

  const removeItem = (foodId: string) => {
    setSelectedItems((prev) => prev.filter((s) => s.foodItem.id !== foodId));
  };

  const totalNutrition = selectedItems.reduce(
    (acc, item) => {
      const ratio = item.quantity;
      return {
        calories: acc.calories + item.foodItem.nutritionalInfo.calories * ratio,
        protein: acc.protein + item.foodItem.nutritionalInfo.protein * ratio,
        carbs: acc.carbs + item.foodItem.nutritionalInfo.carbohydrates * ratio,
        fat: acc.fat + item.foodItem.nutritionalInfo.fat * ratio,
      };
    },
    { calories: 0, protein: 0, carbs: 0, fat: 0 },
  );

  const handleSave = async () => {
    if (!user || selectedItems.length === 0) {
      Alert.alert('No items', 'Please select at least one food item.');
      return;
    }

    try {
      await logMeal({
        userId: user.id,
        logDate: selectedDate,
        mealType: selectedMealType,
        items: selectedItems.map((s) => ({
          foodItemId: s.foodItem.id,
          quantity: s.quantity,
          unit: s.foodItem.servingUnit,
        })),
      });
      navigation.goBack();
    } catch {
      Alert.alert('Error', 'Failed to log meal. Please try again.');
    }
  };

  const isItemSelected = (id: string) => selectedItems.some((s) => s.foodItem.id === id);

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        {/* Meal Type Selector */}
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.mealTypeRow}
          style={styles.mealTypeScroll}
        >
          {MEAL_TYPES.map((mt) => (
            <TouchableOpacity
              key={mt.value}
              style={[
                styles.mealTypeChip,
                selectedMealType === mt.value && styles.mealTypeChipActive,
              ]}
              onPress={() => setSelectedMealType(mt.value)}
            >
              <Text
                style={[
                  styles.mealTypeLabel,
                  selectedMealType === mt.value && styles.mealTypeLabelActive,
                ]}
              >
                {mt.label}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>

        {/* Search Bar */}
        <View style={styles.searchBar}>
          <Ionicons name="search" size={20} color={Colors.textSecondary} />
          <TextInput
            style={styles.searchInput}
            value={searchQuery}
            onChangeText={setSearchQuery}
            placeholder="Search food items..."
            placeholderTextColor={Colors.textHint}
            autoCorrect={false}
            clearButtonMode="while-editing"
          />
          {isSearching && <ActivityIndicator size="small" color={Colors.primary} />}
        </View>

        <View style={styles.body}>
          {/* Search Results */}
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
            </View>
          )}

          {/* Selected Items */}
          {selectedItems.length > 0 && (
            <View style={styles.selectedSection}>
              <Text style={styles.sectionLabel}>Selected ({selectedItems.length})</Text>
              {selectedItems.map((s) => (
                <View key={s.foodItem.id} style={styles.selectedRow}>
                  <View style={styles.selectedLeft}>
                    <Text style={styles.selectedName} numberOfLines={1}>
                      {s.foodItem.name}
                    </Text>
                    <Text style={styles.selectedCal}>
                      {Math.round(s.foodItem.nutritionalInfo.calories * s.quantity)} kcal
                    </Text>
                  </View>
                  <View style={styles.quantityControl}>
                    <TouchableOpacity
                      onPress={() => updateQuantity(s.foodItem.id, -0.5)}
                      style={styles.qtyBtn}
                    >
                      <Text style={styles.qtyBtnText}>−</Text>
                    </TouchableOpacity>
                    <Text style={styles.qtyValue}>{s.quantity}</Text>
                    <TouchableOpacity
                      onPress={() => updateQuantity(s.foodItem.id, 0.5)}
                      style={styles.qtyBtn}
                    >
                      <Text style={styles.qtyBtnText}>+</Text>
                    </TouchableOpacity>
                  </View>
                  <TouchableOpacity
                    onPress={() => removeItem(s.foodItem.id)}
                    style={styles.removeBtn}
                    hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                  >
                    <Ionicons name="close-circle" size={20} color={Colors.textHint} />
                  </TouchableOpacity>
                </View>
              ))}

              {/* Nutrition Preview */}
              <Card style={styles.nutritionPreview}>
                <Text style={styles.nutritionPreviewTitle}>Meal Totals</Text>
                <View style={styles.nutritionRow}>
                  {[
                    { label: 'Calories', value: `${Math.round(totalNutrition.calories)}`, unit: 'kcal', color: Colors.primary },
                    { label: 'Protein', value: `${Math.round(totalNutrition.protein)}`, unit: 'g', color: Colors.info },
                    { label: 'Carbs', value: `${Math.round(totalNutrition.carbs)}`, unit: 'g', color: Colors.secondary },
                    { label: 'Fat', value: `${Math.round(totalNutrition.fat)}`, unit: 'g', color: '#FFC107' },
                  ].map((m) => (
                    <View key={m.label} style={styles.nutritionItem}>
                      <Text style={[styles.nutritionValue, { color: m.color }]}>
                        {m.value}
                        <Text style={styles.nutritionUnit}>{m.unit}</Text>
                      </Text>
                      <Text style={styles.nutritionLabel}>{m.label}</Text>
                    </View>
                  ))}
                </View>
              </Card>
            </View>
          )}
        </View>

        {/* Save Button */}
        <View style={styles.footer}>
          <Button
            title={`Save ${selectedMealType.replace('_', ' ')}`}
            size="lg"
            fullWidth
            loading={isLoading}
            disabled={selectedItems.length === 0}
            onPress={handleSave}
          />
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: Colors.background,
  },
  flex: {
    flex: 1,
  },
  mealTypeScroll: {
    flexGrow: 0,
    backgroundColor: Colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: Colors.divider,
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
    backgroundColor: Colors.background,
    borderWidth: 1.5,
    borderColor: Colors.divider,
  },
  mealTypeChipActive: {
    backgroundColor: Colors.primary,
    borderColor: Colors.primary,
  },
  mealTypeLabel: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.medium,
    color: Colors.textSecondary,
  },
  mealTypeLabelActive: {
    color: Colors.surface,
  },
  searchBar: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.surface,
    marginHorizontal: Spacing.md,
    marginVertical: Spacing.sm,
    paddingHorizontal: Spacing.md,
    borderRadius: BorderRadius.lg,
    borderWidth: 1,
    borderColor: Colors.divider,
    height: 48,
    gap: Spacing.sm,
  },
  searchInput: {
    flex: 1,
    fontSize: FontSize.md,
    color: Colors.text,
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
    color: Colors.textSecondary,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
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
    backgroundColor: Colors.surface,
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
    color: Colors.text,
  },
  selectedCal: {
    fontSize: FontSize.sm,
    color: Colors.textSecondary,
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
    backgroundColor: Colors.primaryLight,
    justifyContent: 'center',
    alignItems: 'center',
  },
  qtyBtnText: {
    fontSize: FontSize.lg,
    color: Colors.primaryDark,
    fontWeight: FontWeight.bold,
    lineHeight: 22,
  },
  qtyValue: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.semibold,
    color: Colors.text,
    minWidth: 24,
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
    color: Colors.text,
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
    color: Colors.textSecondary,
    marginTop: 2,
  },
  footer: {
    padding: Spacing.md,
    backgroundColor: Colors.surface,
    borderTopWidth: 1,
    borderTopColor: Colors.divider,
  },
});
