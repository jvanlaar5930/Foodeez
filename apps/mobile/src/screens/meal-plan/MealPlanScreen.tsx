import React, { useEffect, useState, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ScrollView,
  ActivityIndicator,
  Alert,
  Modal,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import {
  addDays,
  startOfWeek,
  format,
  isSameDay,
  isToday,
  parseISO,
} from 'date-fns';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { MealPlanStackParamList } from '@/navigation/types';
import { useMealPlanStore } from '@/store/mealPlanStore';
import { useAuthStore } from '@/store/authStore';
import { MealPlanEntry, MealType } from '@/types';
import { Colors, Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';

type Props = NativeStackScreenProps<MealPlanStackParamList, 'MealPlanHome'>;

const MEAL_TYPE_LABELS: Record<MealType, string> = {
  [MealType.Breakfast]: 'Breakfast',
  [MealType.MorningSnack]: 'Morning Snack',
  [MealType.Lunch]: 'Lunch',
  [MealType.AfternoonSnack]: 'Afternoon Snack',
  [MealType.Dinner]: 'Dinner',
  [MealType.EveningSnack]: 'Evening Snack',
};

const MEAL_TYPES = [
  MealType.Breakfast,
  MealType.MorningSnack,
  MealType.Lunch,
  MealType.AfternoonSnack,
  MealType.Dinner,
  MealType.EveningSnack,
];

export function MealPlanScreen({ navigation }: Props) {
  const [weekStart, setWeekStart] = useState(() => startOfWeek(new Date(), { weekStartsOn: 1 }));
  const [selectedDay, setSelectedDay] = useState(new Date());
  const [showGenerateModal, setShowGenerateModal] = useState(false);

  const { user } = useAuthStore();
  const { plans, activePlan, isLoading, isGenerating, fetchPlans, generatePlan } = useMealPlanStore();

  const weekDays = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i));

  useEffect(() => {
    if (user?.id) fetchPlans(user.id);
  }, [user?.id]);

  const getEntriesForDay = useCallback(
    (date: Date): MealPlanEntry[] => {
      if (!activePlan) return [];
      const dateStr = format(date, 'yyyy-MM-dd');
      return activePlan.entries.filter(e => e.entryDate === dateStr);
    },
    [activePlan]
  );

  const getEntryForDayAndMeal = (date: Date, mealType: MealType): MealPlanEntry | undefined => {
    return getEntriesForDay(date).find(e => e.mealType === mealType);
  };

  const handleGeneratePlan = async () => {
    if (!user?.id) return;
    setShowGenerateModal(false);
    try {
      await generatePlan({
        userId: user.id,
        startDate: format(weekStart, 'yyyy-MM-dd'),
        endDate: format(addDays(weekStart, 6), 'yyyy-MM-dd'),
      });
    } catch {
      Alert.alert('Error', 'Failed to generate meal plan. Please try again.');
    }
  };

  const selectedDayEntries = getEntriesForDay(selectedDay);
  const hasEntriesThisWeek = weekDays.some(d => getEntriesForDay(d).length > 0);

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      {/* Week navigation */}
      <View style={styles.weekHeader}>
        <TouchableOpacity onPress={() => setWeekStart(d => addDays(d, -7))}>
          <Ionicons name="chevron-back" size={24} color={Colors.text} />
        </TouchableOpacity>
        <Text style={styles.weekLabel}>
          {format(weekStart, 'MMM d')} – {format(addDays(weekStart, 6), 'MMM d, yyyy')}
        </Text>
        <TouchableOpacity onPress={() => setWeekStart(d => addDays(d, 7))}>
          <Ionicons name="chevron-forward" size={24} color={Colors.text} />
        </TouchableOpacity>
      </View>

      {/* Day pills */}
      <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.daysRow} contentContainerStyle={styles.daysRowContent}>
        {weekDays.map(day => {
          const isSelected = isSameDay(day, selectedDay);
          const hasMeals = getEntriesForDay(day).length > 0;
          return (
            <TouchableOpacity
              key={day.toISOString()}
              style={[styles.dayPill, isSelected && styles.dayPillSelected]}
              onPress={() => setSelectedDay(day)}
            >
              <Text style={[styles.dayPillName, isSelected && styles.dayPillTextSelected]}>
                {format(day, 'EEE')}
              </Text>
              <Text style={[styles.dayPillNumber, isSelected && styles.dayPillTextSelected]}>
                {format(day, 'd')}
              </Text>
              {hasMeals && <View style={[styles.dayDot, isSelected && styles.dayDotSelected]} />}
              {isToday(day) && <View style={styles.todayIndicator} />}
            </TouchableOpacity>
          );
        })}
      </ScrollView>

      <ScrollView style={styles.content} contentContainerStyle={styles.contentPadding}>
        {/* Selected day header */}
        <View style={styles.dayHeader}>
          <Text style={styles.dayTitle}>{format(selectedDay, 'EEEE, MMMM d')}</Text>
          <TouchableOpacity
            style={styles.addDayButton}
            onPress={() => navigation.navigate('CalendarDay', { date: format(selectedDay, 'yyyy-MM-dd') })}
          >
            <Ionicons name="create-outline" size={18} color={Colors.primary} />
            <Text style={styles.addDayButtonText}>Edit Day</Text>
          </TouchableOpacity>
        </View>

        {/* Meal slots for selected day */}
        {isLoading ? (
          <ActivityIndicator size="large" color={Colors.primary} style={{ marginTop: Spacing.xl }} />
        ) : (
          MEAL_TYPES.map(mealType => {
            const entry = getEntryForDayAndMeal(selectedDay, mealType);
            return (
              <TouchableOpacity
                key={mealType}
                style={styles.mealSlot}
                onPress={() => navigation.navigate('CalendarDay', { date: format(selectedDay, 'yyyy-MM-dd') })}
              >
                <View style={styles.mealSlotLeft}>
                  <Text style={styles.mealSlotType}>{MEAL_TYPE_LABELS[mealType]}</Text>
                  {entry ? (
                    <Text style={styles.mealSlotName}>{entry.recipeName ?? entry.foodItemName ?? 'Custom meal'}</Text>
                  ) : (
                    <Text style={styles.mealSlotEmpty}>Not planned</Text>
                  )}
                </View>
                {entry ? (
                  <Ionicons name="checkmark-circle" size={22} color={Colors.primary} />
                ) : (
                  <Ionicons name="add-circle-outline" size={22} color={Colors.textSecondary} />
                )}
              </TouchableOpacity>
            );
          })
        )}

        {/* AI generate section */}
        {!isGenerating && !hasEntriesThisWeek && (
          <View style={styles.generateBanner}>
            <Ionicons name="sparkles" size={32} color={Colors.secondary} />
            <Text style={styles.generateTitle}>Let AI plan your week</Text>
            <Text style={styles.generateSubtitle}>
              Get a personalized meal plan based on your dietary goals and preferences.
            </Text>
            <TouchableOpacity style={styles.generateButton} onPress={() => setShowGenerateModal(true)}>
              <Ionicons name="sparkles" size={18} color={Colors.surface} />
              <Text style={styles.generateButtonText}>Generate AI Meal Plan</Text>
            </TouchableOpacity>
          </View>
        )}

        {isGenerating && (
          <View style={styles.generatingState}>
            <ActivityIndicator size="large" color={Colors.primary} />
            <Text style={styles.generatingText}>AI is crafting your meal plan...</Text>
            <Text style={styles.generatingSubText}>This may take a moment</Text>
          </View>
        )}
      </ScrollView>

      {/* Generate confirm modal */}
      <Modal visible={showGenerateModal} transparent animationType="fade">
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Ionicons name="sparkles" size={40} color={Colors.secondary} />
            <Text style={styles.modalTitle}>Generate AI Meal Plan</Text>
            <Text style={styles.modalText}>
              AI will create a complete meal plan for the week of{' '}
              {format(weekStart, 'MMM d')} – {format(addDays(weekStart, 6), 'MMM d')}{' '}
              based on your nutritional goals.
            </Text>
            <View style={styles.modalActions}>
              <TouchableOpacity style={styles.modalCancel} onPress={() => setShowGenerateModal(false)}>
                <Text style={styles.modalCancelText}>Cancel</Text>
              </TouchableOpacity>
              <TouchableOpacity style={styles.modalConfirm} onPress={handleGeneratePlan}>
                <Text style={styles.modalConfirmText}>Generate</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: Colors.background },
  weekHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: Spacing.lg,
    paddingVertical: Spacing.md,
    backgroundColor: Colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: Colors.divider,
  },
  weekLabel: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: Colors.text },
  daysRow: { backgroundColor: Colors.surface, maxHeight: 88 },
  daysRowContent: { paddingHorizontal: Spacing.md, paddingVertical: Spacing.sm, gap: Spacing.sm },
  dayPill: {
    alignItems: 'center',
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
    borderRadius: BorderRadius.xl,
    minWidth: 52,
    borderWidth: 1,
    borderColor: Colors.divider,
  },
  dayPillSelected: { backgroundColor: Colors.primary, borderColor: Colors.primary },
  dayPillName: { fontSize: FontSize.xs, color: Colors.textSecondary, fontWeight: FontWeight.medium },
  dayPillNumber: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: Colors.text, marginTop: 2 },
  dayPillTextSelected: { color: Colors.surface },
  dayDot: { width: 6, height: 6, borderRadius: 3, backgroundColor: Colors.primary, marginTop: 2 },
  dayDotSelected: { backgroundColor: Colors.surface },
  todayIndicator: {
    position: 'absolute',
    top: 4,
    right: 4,
    width: 6,
    height: 6,
    borderRadius: 3,
    backgroundColor: Colors.secondary,
  },
  content: { flex: 1 },
  contentPadding: { padding: Spacing.lg, gap: Spacing.md, paddingBottom: Spacing.xl },
  dayHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: Spacing.sm },
  dayTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: Colors.text },
  addDayButton: { flexDirection: 'row', alignItems: 'center', gap: Spacing.xs },
  addDayButtonText: { color: Colors.primary, fontWeight: FontWeight.medium },
  mealSlot: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: Colors.surface,
    borderRadius: BorderRadius.lg,
    padding: Spacing.md,
    ...Shadows.sm,
  },
  mealSlotLeft: { flex: 1 },
  mealSlotType: { fontSize: FontSize.sm, color: Colors.textSecondary, fontWeight: FontWeight.medium, textTransform: 'uppercase', letterSpacing: 0.5 },
  mealSlotName: { fontSize: FontSize.md, color: Colors.text, fontWeight: FontWeight.semibold, marginTop: 2 },
  mealSlotEmpty: { fontSize: FontSize.md, color: Colors.textHint, fontStyle: 'italic', marginTop: 2 },
  generateBanner: {
    backgroundColor: Colors.surface,
    borderRadius: BorderRadius.xl,
    padding: Spacing.xl,
    alignItems: 'center',
    gap: Spacing.sm,
    marginTop: Spacing.md,
    ...Shadows.md,
    borderWidth: 1,
    borderColor: Colors.primaryLight,
  },
  generateTitle: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: Colors.text },
  generateSubtitle: { fontSize: FontSize.md, color: Colors.textSecondary, textAlign: 'center' },
  generateButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.sm,
    backgroundColor: Colors.secondary,
    paddingHorizontal: Spacing.xl,
    paddingVertical: Spacing.md,
    borderRadius: BorderRadius.lg,
    marginTop: Spacing.sm,
  },
  generateButtonText: { color: Colors.surface, fontWeight: FontWeight.semibold, fontSize: FontSize.md },
  generatingState: { alignItems: 'center', gap: Spacing.md, paddingVertical: Spacing.xl },
  generatingText: { fontSize: FontSize.lg, fontWeight: FontWeight.semibold, color: Colors.text },
  generatingSubText: { fontSize: FontSize.md, color: Colors.textSecondary },
  modalOverlay: { flex: 1, backgroundColor: Colors.overlay, justifyContent: 'center', alignItems: 'center', padding: Spacing.xl },
  modalContent: { backgroundColor: Colors.surface, borderRadius: BorderRadius.xl, padding: Spacing.xl, alignItems: 'center', gap: Spacing.md, width: '100%' },
  modalTitle: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: Colors.text },
  modalText: { fontSize: FontSize.md, color: Colors.textSecondary, textAlign: 'center', lineHeight: 22 },
  modalActions: { flexDirection: 'row', gap: Spacing.md, marginTop: Spacing.sm, width: '100%' },
  modalCancel: { flex: 1, borderWidth: 2, borderColor: Colors.divider, borderRadius: BorderRadius.lg, paddingVertical: Spacing.md, alignItems: 'center' },
  modalCancelText: { color: Colors.textSecondary, fontWeight: FontWeight.semibold },
  modalConfirm: { flex: 1, backgroundColor: Colors.secondary, borderRadius: BorderRadius.lg, paddingVertical: Spacing.md, alignItems: 'center' },
  modalConfirmText: { color: Colors.surface, fontWeight: FontWeight.semibold },
});
