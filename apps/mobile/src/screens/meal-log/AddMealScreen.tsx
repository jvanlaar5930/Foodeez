import React, { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';
import { CustomFoodModal } from '@/components/meal/CustomFoodModal';
import { FoodSearchPanel } from '@/components/meal/FoodSearchPanel';
import { MealAnalysisCard } from '@/components/meal/MealAnalysisCard';
import { MealTypeChips } from '@/components/meal/MealTypeChips';
import { QuickAddBar } from '@/components/meal/QuickAddBar';
import { SaveMealModal } from '@/components/meal/SaveMealModal';
import { SelectedFoodRow } from '@/components/meal/SelectedFoodRow';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { MacroRow } from '@/components/ui/MacroRow';
import { useFoodSearch } from '@/hooks/useFoodSearch';
import { useMealAnalysis } from '@/hooks/useMealAnalysis';
import { useMealItems } from '@/hooks/useMealItems';
import { useAuthStore } from '@/store/authStore';
import { useMealStore } from '@/store/mealStore';
import { mealTemplateService } from '@/services/mealTemplateService';
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import {
  MEAL_TYPE_SHORT_LABELS,
  MealType,
  type FoodItemDto,
  type QuickAddResultDto,
} from '@/types';
import type { MealLogStackParamList } from '@/navigation/types';

type Props = NativeStackScreenProps<MealLogStackParamList, 'AddMeal'>;

export function AddMealScreen({ navigation, route }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  const user = useAuthStore((state) => state.user);
  const logMeal = useMealStore((state) => state.logMeal);
  const updateMealLog = useMealStore((state) => state.updateMealLog);
  const selectedDate = useMealStore((state) => state.selectedDate);
  const isLoading = useMealStore((state) => state.isLoading);

  const editingMealLog = route.params?.mealLog;

  const [mealType, setMealType] = useState<MealType>(
    editingMealLog?.mealType ?? route.params?.mealType ?? MealType.Breakfast,
  );

  const meal = useMealItems(
    editingMealLog?.items.map((item) => ({
      foodItem: item.foodItem,
      quantity: item.quantity,
    })) ?? [],
  );
  const search = useFoodSearch();
  const analysis = useMealAnalysis(editingMealLog?.analysis ?? null);

  const [showCustomFood, setShowCustomFood] = useState(false);
  const [showSaveMeal, setShowSaveMeal] = useState(false);
  const [isSavingMeal, setIsSavingMeal] = useState(false);
  const [saveMealError, setSaveMealError] = useState<string | null>(null);
  /**
   * Guards handleSave against a double tap firing two overlapping requests - the store's own
   * isLoading only flips after the first request has started, which is one render too late to
   * block a second tap in the same frame.
   */
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { reset: resetMeal, put: putItems, markEdited } = meal;

  // Seeded on open rather than on mount: one screen serves both logging and editing.
  useEffect(() => {
    setMealType(editingMealLog?.mealType ?? route.params?.mealType ?? MealType.Breakfast);
    resetMeal(
      editingMealLog?.items.map((item) => ({
        foodItem: item.foodItem,
        quantity: item.quantity,
      })) ?? [],
    );
    analysis.set(editingMealLog?.analysis ?? null);
    // `analysis` is rebuilt every render; depending on it would re-seed the screen constantly.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [editingMealLog, route.params?.mealType, resetMeal]);

  // What the photo scanner handed over. Kept separate from the reset above so arriving from a
  // scan fills the screen in rather than clearing it.
  const scannedItems = route.params?.parsedItems;
  useEffect(() => {
    putItems(
      (scannedItems ?? []).map((scanned) => ({
        foodItem: scanned.foodItem,
        quantity: scanned.quantity,
        source: scanned.source,
      })),
    );
  }, [scannedItems, putItems]);

  /** Any change to the meal makes a stored score stale - drop it rather than show a wrong one. */
  const changeMealType = useCallback(
    (next: MealType) => {
      setMealType(next);
      markEdited();
      analysis.invalidate();
      // eslint-disable-next-line react-hooks/exhaustive-deps
    },
    [markEdited],
  );

  const selectFood = useCallback(
    (item: FoodItemDto) => {
      meal.toggle(item);
      analysis.invalidate();
      // eslint-disable-next-line react-hooks/exhaustive-deps
    },
    [meal.toggle],
  );

  const adjustQuantity = useCallback(
    (foodItemId: string, delta: number) => {
      meal.adjust(foodItemId, delta);
      analysis.invalidate();
      // eslint-disable-next-line react-hooks/exhaustive-deps
    },
    [meal.adjust],
  );

  const setQuantity = useCallback(
    (foodItemId: string, quantity: number) => {
      meal.setQuantity(foodItemId, quantity);
      analysis.invalidate();
      // eslint-disable-next-line react-hooks/exhaustive-deps
    },
    [meal.setQuantity],
  );

  const removeItem = useCallback(
    (foodItemId: string) => {
      meal.remove(foodItemId);
      analysis.invalidate();
      // eslint-disable-next-line react-hooks/exhaustive-deps
    },
    [meal.remove],
  );

  /**
   * Quick add's items land in the same list a searched-for food does, so everything already
   * built on that list - amounts, removal, totals, saving - keeps working untouched.
   */
  function applyQuickAdd(result: QuickAddResultDto): void {
    meal.put(
      result.items.map((parsed) => ({
        foodItem: parsed.foodItem,
        quantity: parsed.quantity,
        source: parsed.source,
      })),
    );

    // Only a saved meal knows which meal it is; a described one leaves the choice alone.
    if (result.mealType) {
      setMealType(result.mealType);
    }

    analysis.invalidate();
  }

  /** Bumped after saving a meal, so the quick-add bar reloads and shows the new name. */
  const [savedMealsToken, setSavedMealsToken] = useState(0);

  async function saveAsTemplate(name: string): Promise<void> {
    if (!user || meal.items.length === 0) return;

    setIsSavingMeal(true);
    setSaveMealError(null);

    try {
      await mealTemplateService.save({
        userId: user.id,
        name,
        mealType,
        items: meal.items.map((entry) => ({
          foodItemId: entry.foodItem.id,
          quantity: entry.quantity,
          unit: entry.foodItem.servingUnit,
        })),
      });

      setSavedMealsToken((token) => token + 1);
      setShowSaveMeal(false);
      Alert.alert('Saved', `"${name}" is in Quick add now.`);
    } catch {
      // The likely cause is a name already in use, which is the one thing the reader can fix.
      setSaveMealError('That could not be saved. Try a different name.');
    } finally {
      setIsSavingMeal(false);
    }
  }

  async function handleSave(): Promise<void> {
    if (!user || isSubmitting) return;

    if (meal.items.length === 0) {
      Alert.alert('No items', 'Please select at least one food item.');
      return;
    }

    setIsSubmitting(true);

    const payload = {
      userId: user.id,
      // Editing keeps the log's own date - `selectedDate` is the Meal Log tab's separately
      // selected day, which is the wrong date whenever this screen was reached from somewhere
      // else (the calendar, in particular) showing a different day entirely.
      logDate: editingMealLog?.logDate ?? selectedDate,
      mealType,
      items: meal.items.map((entry) => ({
        foodItemId: entry.foodItem.id,
        quantity: entry.quantity,
        unit: entry.foodItem.servingUnit,
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
    } finally {
      setIsSubmitting(false);
    }
  }

  function runAnalysis(): void {
    if (!user) return;

    analysis.run({
      userId: user.id,
      mealType,
      items: meal.items,
      // A saved meal that has not been touched here can have its stored score for free.
      storedMealLogId: editingMealLog && !meal.edited ? editingMealLog.id : null,
    });
  }

  return (
    // A plain View, not a SafeAreaView: the tab bar below this screen already sits on the
    // home indicator's inset, so insetting here as well left the save button floating a
    // gesture bar's height above the navigation.
    <View style={styles.safe}>
      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <ScrollView
          style={styles.flex}
          contentContainerStyle={styles.scrollContent}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <MealTypeChips value={mealType} onChange={changeMealType} />

          <FoodSearchPanel
            query={search.query}
            onQueryChange={search.setQuery}
            results={search.results}
            isSearching={search.isSearching}
            isSelected={meal.has}
            onSelect={selectFood}
            onAddCustom={() => setShowCustomFood(true)}
          />

          {/* Below the search box: searching by name is the more direct path, and this is
              the fallback for "I don't want to look each item up". */}
          <QuickAddBar
            onApplied={applyQuickAdd}
            reloadToken={savedMealsToken}
            onScanPress={() => navigation.navigate('FoodScan')}
          />

          {meal.items.length > 0 ? (
            <View style={styles.selected}>
              <Text style={styles.label}>Selected ({meal.items.length})</Text>

              {meal.items.map((entry) => (
                <SelectedFoodRow
                  key={entry.foodItem.id}
                  entry={entry}
                  onAdjust={adjustQuantity}
                  onSetQuantity={setQuantity}
                  onRemove={removeItem}
                />
              ))}

              <Card style={styles.totals}>
                <Text style={styles.totalsTitle}>Meal Totals</Text>
                <MacroRow totals={meal.totals} />
              </Card>

              <MealAnalysisCard
                analysis={analysis.analysis}
                isAnalyzing={analysis.isAnalyzing}
                error={analysis.error}
                streamedText={analysis.streamText}
                analyzeLabel={analysis.label}
                onAnalyze={runAnalysis}
              />
            </View>
          ) : null}
        </ScrollView>

        <View style={styles.footer}>
          {meal.items.length > 0 ? (
            <TouchableOpacity
              style={styles.saveMealLink}
              onPress={() => {
                setSaveMealError(null);
                setShowSaveMeal(true);
              }}
            >
              <Ionicons name="bookmark-outline" size={16} color={C.primary} />
              <Text style={styles.saveMealLinkText}>Eat this often? Save it as a meal</Text>
            </TouchableOpacity>
          ) : null}

          <Button
            // The shared short label, not the enum name: `replace('_', ' ')` on a
            // PascalCase value left the button reading "Save MorningSnack".
            title={editingMealLog ? 'Update Meal' : `Save ${MEAL_TYPE_SHORT_LABELS[mealType]}`}
            size="lg"
            fullWidth
            loading={isLoading || isSubmitting}
            disabled={meal.items.length === 0 || isSubmitting}
            onPress={handleSave}
          />
        </View>
      </KeyboardAvoidingView>

      <SaveMealModal
        visible={showSaveMeal}
        itemCount={meal.items.length}
        isSaving={isSavingMeal}
        error={saveMealError}
        onClose={() => setShowSaveMeal(false)}
        onSave={saveAsTemplate}
      />

      <CustomFoodModal
        visible={showCustomFood}
        initialName={search.query.trim()}
        onClose={() => setShowCustomFood(false)}
        onCreated={(item) => {
          setShowCustomFood(false);
          selectFood(item);
          search.clear();
        }}
      />
    </View>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    safe: { flex: 1, backgroundColor: C.background },
    flex: { flex: 1 },
    scrollContent: { paddingBottom: Spacing.lg },
    selected: { paddingHorizontal: Spacing.md, paddingTop: Spacing.sm },
    label: {
      fontSize: FontSize.sm,
      fontWeight: FontWeight.semibold,
      color: C.textSecondary,
      marginBottom: Spacing.xs,
    },
    totals: { marginTop: Spacing.sm },
    totalsTitle: {
      fontSize: FontSize.md,
      fontWeight: FontWeight.semibold,
      color: C.text,
      marginBottom: Spacing.sm,
    },
    footer: {
      padding: Spacing.md,
      backgroundColor: C.surface,
      borderTopWidth: 1,
      borderTopColor: C.divider,
      gap: Spacing.sm,
    },
    saveMealLink: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      gap: Spacing.xs,
    },
    saveMealLinkText: { fontSize: FontSize.sm, color: C.primary, fontWeight: FontWeight.medium },
  });
