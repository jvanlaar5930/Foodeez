import React, { useCallback, useEffect, useState } from 'react';
import { ScrollView, StyleSheet, Text, TextInput, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { addDays, format, isSameDay, isToday, startOfWeek } from 'date-fns';
import { MealTypeChips } from '@/components/meal/MealTypeChips';
import { ModalActions } from '@/components/ui/ModalActions';
import { ModalSheet } from '@/components/ui/ModalSheet';
import { useAuthStore } from '@/store/authStore';
import { useMealPlanStore } from '@/store/mealPlanStore';
import { MEAL_TYPE_SHORT_LABELS, MealType, type RecipeDto } from '@/types';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

interface AddToMealPlanSheetProps {
  visible: boolean;
  recipe: RecipeDto;
  onClose: () => void;
  /** Somewhere to send the reader once the meal is in the calendar. */
  onViewPlan?: () => void;
}

/**
 * Puts a recipe into a slot in the weekly plan.
 *
 * The recipe screen used to answer this with "coming soon". Everything it needs already
 * exists - the plan store creates a week's plan on demand and the API replaces whatever is
 * in a slot - so all that was missing was somewhere to say which day.
 */
export function AddToMealPlanSheet({ visible, recipe, onClose, onViewPlan }: AddToMealPlanSheetProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  const user = useAuthStore((state) => state.user);
  const plans = useMealPlanStore((state) => state.plans);
  const ensurePlanFor = useMealPlanStore((state) => state.ensurePlanFor);
  const saveEntry = useMealPlanStore((state) => state.saveEntry);
  const clearError = useMealPlanStore((state) => state.clearError);

  const [weekStart, setWeekStart] = useState(() => startOfWeek(new Date(), { weekStartsOn: 1 }));
  const [selectedDay, setSelectedDay] = useState(() => new Date());
  const [mealType, setMealType] = useState<MealType>(MealType.Dinner);
  const [servings, setServings] = useState('1');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  /** The day the recipe landed on, which turns the sheet into a confirmation. */
  const [savedOn, setSavedOn] = useState<Date | null>(null);

  // Seeded on open rather than on mount: the sheet outlives any one visit to it.
  useEffect(() => {
    if (!visible) return;
    const today = new Date();
    setWeekStart(startOfWeek(today, { weekStartsOn: 1 }));
    setSelectedDay(today);
    setMealType(MealType.Dinner);
    setServings('1');
    setError(null);
    setSavedOn(null);
    clearError();
    // The meal-plan tab may never have been opened, and without the plans there is nothing to
    // say what a slot already holds. Read through the store rather than subscribing: `hasLoaded`
    // flipping is not a reason to re-run this effect and wipe out what has been picked.
    const { hasLoaded, fetchPlans } = useMealPlanStore.getState();
    const userId = useAuthStore.getState().user?.id;
    if (userId && !hasLoaded) void fetchPlans(userId);
  }, [visible, clearError]);

  const weekDays = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i));

  /**
   * What is already in the chosen slot. The API replaces whatever occupies one, so saying so
   * up front is the difference between adding a meal and quietly losing one.
   */
  const occupant = useCallback((): string | undefined => {
    const dateStr = format(selectedDay, 'yyyy-MM-dd');
    const plan = plans.find((p) => p.startDate <= dateStr && p.endDate >= dateStr);
    const entry = plan?.entriesByDate?.[dateStr]?.find((e) => e.mealType === mealType);
    if (!entry) return undefined;
    return entry.recipeName ?? entry.foodItemName ?? entry.notes ?? 'A meal';
  }, [plans, selectedDay, mealType]);

  const handleSave = async () => {
    if (!user?.id) return;
    const parsedServings = parseFloat(servings);
    if (!(parsedServings > 0)) {
      setError('Servings must be greater than zero.');
      return;
    }

    setSaving(true);
    setError(null);
    try {
      // The day picked here can sit in a week that was never planned, so make a plan for that
      // week rather than refusing the save - the same fallback the calendar itself uses.
      const planWeekStart = startOfWeek(selectedDay, { weekStartsOn: 1 });
      const plan = await ensurePlanFor(
        user.id,
        format(planWeekStart, 'yyyy-MM-dd'),
        format(addDays(planWeekStart, 6), 'yyyy-MM-dd'),
        `Week of ${format(planWeekStart, 'MMM d, yyyy')}`,
      );

      const dateStr = format(selectedDay, 'yyyy-MM-dd');
      const existing = plan.entriesByDate?.[dateStr]?.find((e) => e.mealType === mealType);
      // No notes: a linked recipe carries its own name.
      await saveEntry(user.id, plan.id, existing?.id ?? null, {
        entryDate: dateStr,
        mealType,
        recipeId: recipe.id,
        servings: parsedServings,
      });
      setSavedOn(selectedDay);
    } catch {
      // Read fresh rather than the error captured when this closure was created - the store
      // only sets it once the save actually fails, after this handler already started.
      setError(useMealPlanStore.getState().error ?? 'That meal could not be added. Please try again.');
      clearError();
    } finally {
      setSaving(false);
    }
  };

  const alreadyPlanned = savedOn ? undefined : occupant();

  return (
    <ModalSheet
      visible={visible}
      onClose={onClose}
      variant="bottom"
      title={savedOn ? 'Added to your plan' : 'Add to meal plan'}
      // A save is in flight; a stray tap on the backdrop should not throw it away.
      dismissOnBackdrop={!saving}
      footer={
        savedOn ? (
          <ModalActions
            confirmLabel="View plan"
            onConfirm={() => {
              onClose();
              onViewPlan?.();
            }}
            cancelLabel="Done"
            onCancel={onClose}
          />
        ) : (
          <ModalActions
            confirmLabel="Add to plan"
            onConfirm={handleSave}
            onCancel={onClose}
            busy={saving}
          />
        )
      }
    >
      {savedOn ? (
        <Text style={styles.confirmation}>
          <Text style={styles.recipeName}>{recipe.name}</Text> is planned for{' '}
          {MEAL_TYPE_SHORT_LABELS[mealType].toLowerCase()} on {format(savedOn, 'EEEE d MMMM')}.
        </Text>
      ) : (
        <>
          <Text style={styles.recipeName} numberOfLines={2}>
            {recipe.name}
          </Text>

          <View style={styles.weekHeader}>
            <TouchableOpacity
              onPress={() => setWeekStart((d) => addDays(d, -7))}
              accessibilityLabel="Previous week"
              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
            >
              <Ionicons name="chevron-back" size={20} color={C.text} />
            </TouchableOpacity>
            <Text style={styles.weekLabel}>
              {format(weekDays[0], 'MMM d')} – {format(weekDays[6], 'MMM d')}
            </Text>
            <TouchableOpacity
              onPress={() => setWeekStart((d) => addDays(d, 7))}
              accessibilityLabel="Next week"
              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
            >
              <Ionicons name="chevron-forward" size={20} color={C.text} />
            </TouchableOpacity>
          </View>

          <ScrollView
            horizontal
            showsHorizontalScrollIndicator={false}
            contentContainerStyle={styles.daysRow}
            style={styles.daysScroll}
          >
            {weekDays.map((day) => {
              const selected = isSameDay(day, selectedDay);
              return (
                <TouchableOpacity
                  key={day.toISOString()}
                  style={[styles.dayPill, selected && styles.dayPillSelected]}
                  onPress={() => setSelectedDay(day)}
                  accessibilityRole="button"
                  accessibilityState={{ selected }}
                  accessibilityLabel={format(day, 'EEEE d MMMM')}
                >
                  <Text style={[styles.dayName, selected && styles.dayTextSelected]}>
                    {format(day, 'EEE')}
                  </Text>
                  <Text style={[styles.dayNumber, selected && styles.dayTextSelected]}>
                    {format(day, 'd')}
                  </Text>
                  {isToday(day) ? (
                    <View style={[styles.todayDot, selected && styles.todayDotSelected]} />
                  ) : null}
                </TouchableOpacity>
              );
            })}
          </ScrollView>

          <MealTypeChips value={mealType} onChange={setMealType} />

          <Text style={styles.label}>Servings</Text>
          <TextInput
            style={styles.servingsInput}
            value={servings}
            onChangeText={setServings}
            keyboardType="decimal-pad"
            accessibilityLabel="Servings"
          />

          {alreadyPlanned ? (
            <Text style={styles.warning}>
              {alreadyPlanned} is already planned for this slot and will be replaced.
            </Text>
          ) : null}

          {error ? <Text style={styles.error}>{error}</Text> : null}
        </>
      )}
    </ModalSheet>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    recipeName: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
    confirmation: { fontSize: FontSize.md, color: C.text, lineHeight: 22 },
    weekHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginTop: Spacing.sm,
    },
    weekLabel: { fontSize: FontSize.sm, fontWeight: FontWeight.semibold, color: C.textSecondary },
    daysScroll: { flexGrow: 0 },
    daysRow: { gap: Spacing.sm, paddingVertical: Spacing.xs },
    dayPill: {
      alignItems: 'center',
      minWidth: 52,
      paddingVertical: Spacing.sm,
      paddingHorizontal: Spacing.sm,
      borderRadius: BorderRadius.lg,
      borderWidth: 1.5,
      borderColor: C.divider,
      backgroundColor: C.background,
    },
    dayPillSelected: { backgroundColor: C.primary, borderColor: C.primary },
    dayName: { fontSize: FontSize.xs, color: C.textSecondary, fontWeight: FontWeight.medium },
    dayNumber: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text },
    dayTextSelected: { color: C.onPrimary },
    todayDot: { width: 4, height: 4, borderRadius: 2, backgroundColor: C.primary, marginTop: 2 },
    todayDotSelected: { backgroundColor: C.onPrimary },
    label: {
      fontSize: FontSize.sm,
      fontWeight: FontWeight.semibold,
      color: C.text,
      marginTop: Spacing.sm,
    },
    servingsInput: {
      width: 96,
      borderWidth: 1,
      borderColor: C.divider,
      borderRadius: BorderRadius.md,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.sm,
      fontSize: FontSize.md,
      color: C.text,
      backgroundColor: C.background,
    },
    warning: { fontSize: FontSize.sm, color: C.warning },
    error: { fontSize: FontSize.sm, color: C.error },
  });
