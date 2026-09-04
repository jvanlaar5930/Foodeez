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
  TextInput,
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
import { mealService } from '@/services/mealService';
import {
  MEAL_TYPE_LABELS,
  MealLogDto,
  MealPlanEntryDto,
  MealType,
  ORDERED_MEAL_TYPES,
} from '@/types';
import { Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

type Props = NativeStackScreenProps<MealPlanStackParamList, 'MealPlanHome'>;

/**
 * What to call a planned meal. AI-generated entries have no Recipe or FoodItem row behind
 * them, so both names are null and the meal's own name is in `notes` - without this every
 * generated slot read "Custom meal".
 */
function entryLabel(entry: MealPlanEntryDto): string {
  return entry.recipeName ?? entry.foodItemName ?? entry.notes ?? 'Custom meal';
}

export function MealPlanScreen({ navigation }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const [weekStart, setWeekStart] = useState(() => startOfWeek(new Date(), { weekStartsOn: 1 }));
  const [selectedDay, setSelectedDay] = useState(new Date());
  const [showGenerateModal, setShowGenerateModal] = useState(false);
  /** Free text for the next generation. Held here so a failed attempt can be retried as asked. */
  const [guidance, setGuidance] = useState('');

  const { user } = useAuthStore();
  const { plans, activePlan, isLoading, isGenerating, fetchPlans, generatePlan } = useMealPlanStore();
  // What was actually logged for the visible week, shown read-only alongside what was
  // planned - a display-only overlay, so a failed fetch just leaves the grid unannotated.
  const [loggedLogs, setLoggedLogs] = useState<MealLogDto[]>([]);

  const weekDays = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i));

  useEffect(() => {
    if (user?.id) fetchPlans(user.id);
  }, [user?.id]);

  useEffect(() => {
    if (!user?.id) return;
    let cancelled = false;
    mealService
      .getLogsRange(user.id, format(weekStart, 'yyyy-MM-dd'), format(addDays(weekStart, 6), 'yyyy-MM-dd'))
      .then((logs) => { if (!cancelled) setLoggedLogs(logs); })
      .catch(() => { if (!cancelled) setLoggedLogs([]); });
    return () => { cancelled = true; };
  }, [user?.id, weekStart]);

  const getLoggedLabel = useCallback(
    (date: Date, mealType: MealType): string | undefined => {
      const dateStr = format(date, 'yyyy-MM-dd');
      const log = loggedLogs.find((l) => l.logDate === dateStr && l.mealType === mealType);
      if (!log) return undefined;
      const label = log.items.map((item) => item.foodItem.name).filter(Boolean).join(', ');
      return label.length > 0 ? label : undefined;
    },
    [loggedLogs]
  );

  const getEntriesForDay = useCallback(
    (date: Date): MealPlanEntryDto[] => {
      // Already grouped by date on the server, so this is a lookup rather than a scan.
      // The ?? [] matters: a day with no meals simply has no key.
      const dateStr = format(date, 'yyyy-MM-dd');
      return activePlan?.entriesByDate?.[dateStr] ?? [];
    },
    [activePlan]
  );

  const getEntryForDayAndMeal = (date: Date, mealType: MealType): MealPlanEntryDto | undefined => {
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
        guidance: guidance.trim() || undefined,
      });
    } catch {
      Alert.alert('Error', 'Failed to generate meal plan. Please try again.');
    }
  };

  const selectedDayEntries = getEntriesForDay(selectedDay);
  const dayHasLoggedMeal = (date: Date) => loggedLogs.some((l) => l.logDate === format(date, 'yyyy-MM-dd'));
  const hasEntriesThisWeek = weekDays.some(d => getEntriesForDay(d).length > 0 || dayHasLoggedMeal(d));

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      {/* Week navigation */}
      <View style={styles.weekHeader}>
        <TouchableOpacity onPress={() => setWeekStart(d => addDays(d, -7))}>
          <Ionicons name="chevron-back" size={24} color={C.text} />
        </TouchableOpacity>
        <Text style={styles.weekLabel}>
          {format(weekStart, 'MMM d')} – {format(addDays(weekStart, 6), 'MMM d, yyyy')}
        </Text>
        {/* Grouped so the header keeps three children and the week label stays centred. */}
        <View style={styles.weekHeaderRight}>
          <TouchableOpacity onPress={() => setWeekStart(d => addDays(d, 7))}>
            <Ionicons name="chevron-forward" size={24} color={C.text} />
          </TouchableOpacity>
          {/* Always reachable, not only on an empty week: replanning a week you are part
              way through is the common case, and the banner below disappears the moment a
              single meal is planned. */}
          <TouchableOpacity
            disabled={isGenerating}
            onPress={() => setShowGenerateModal(true)}
            accessibilityLabel="Generate a plan for this week"
          >
            <Ionicons name="sparkles" size={22} color={isGenerating ? C.textSecondary : C.primary} />
          </TouchableOpacity>
        </View>
      </View>

      {/* Day pills */}
      <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.daysRow} contentContainerStyle={styles.daysRowContent}>
        {weekDays.map(day => {
          const isSelected = isSameDay(day, selectedDay);
          const hasMeals = getEntriesForDay(day).length > 0 || dayHasLoggedMeal(day);
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
            <Ionicons name="create-outline" size={18} color={C.primary} />
            <Text style={styles.addDayButtonText}>Edit Day</Text>
          </TouchableOpacity>
        </View>

        {/* Meal slots for selected day */}
        {isLoading ? (
          <ActivityIndicator size="large" color={C.primary} style={{ marginTop: Spacing.xl }} />
        ) : (
          ORDERED_MEAL_TYPES.map(mealType => {
            const entry = getEntryForDayAndMeal(selectedDay, mealType);
            const loggedLabel = entry ? undefined : getLoggedLabel(selectedDay, mealType);
            return (
              <TouchableOpacity
                key={mealType}
                style={styles.mealSlot}
                onPress={() => navigation.navigate('CalendarDay', { date: format(selectedDay, 'yyyy-MM-dd') })}
              >
                <View style={styles.mealSlotLeft}>
                  <Text style={styles.mealSlotType}>{MEAL_TYPE_LABELS[mealType]}</Text>
                  {entry ? (
                    <Text style={styles.mealSlotName} numberOfLines={2}>{entryLabel(entry)}</Text>
                  ) : loggedLabel ? (
                    <Text style={styles.mealSlotLogged} numberOfLines={2}>{loggedLabel} (logged)</Text>
                  ) : (
                    <Text style={styles.mealSlotEmpty}>Not planned</Text>
                  )}
                </View>
                {entry ? (
                  <Ionicons name="checkmark-circle" size={22} color={C.primary} />
                ) : loggedLabel ? (
                  <Ionicons name="restaurant" size={20} color={C.textSecondary} />
                ) : (
                  <Ionicons name="add-circle-outline" size={22} color={C.textSecondary} />
                )}
              </TouchableOpacity>
            );
          })
        )}

        {/* AI generate section */}
        {!isGenerating && !hasEntriesThisWeek && (
          <View style={styles.generateBanner}>
            <Ionicons name="sparkles" size={32} color={C.secondary} />
            <Text style={styles.generateTitle}>Let AI plan your week</Text>
            <Text style={styles.generateSubtitle}>
              Get a personalized meal plan based on your dietary goals and preferences.
            </Text>
            <TouchableOpacity style={styles.generateButton} onPress={() => setShowGenerateModal(true)}>
              <Ionicons name="sparkles" size={18} color={C.onPrimary} />
              <Text style={styles.generateButtonText}>Generate Plan</Text>
            </TouchableOpacity>
          </View>
        )}

        {isGenerating && (
          <View style={styles.generatingState}>
            <ActivityIndicator size="large" color={C.primary} />
            <Text style={styles.generatingText}>AI is crafting your meal plan...</Text>
            <Text style={styles.generatingSubText}>This may take a moment</Text>
          </View>
        )}
      </ScrollView>

      {/* Generate confirm modal */}
      <Modal visible={showGenerateModal} transparent animationType="fade">
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Ionicons name="sparkles" size={40} color={C.secondary} />
            <Text style={styles.modalTitle}>Generate Plan</Text>
            <Text style={styles.modalText}>
              A full week for {format(weekStart, 'MMM d')} –{' '}
              {format(addDays(weekStart, 6), 'MMM d')}, built around your targets and the
              foods you avoid.
            </Text>

            <Text style={styles.modalLabel}>Anything specific? (optional)</Text>
            <TextInput
              style={styles.modalInput}
              value={guidance}
              onChangeText={setGuidance}
              placeholder="e.g. more variety in the dinners, and reuse last week's breakfasts"
              placeholderTextColor={C.textSecondary}
              multiline
              numberOfLines={3}
              maxLength={1000}
              textAlignVertical="top"
            />
            <Text style={styles.modalHint}>
              Mention last week and the plan you already have is used as the reference.
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

const makeStyles = (C: Palette) => StyleSheet.create({
  container: { flex: 1, backgroundColor: C.background },
  weekHeaderRight: { flexDirection: 'row', alignItems: 'center', gap: Spacing.md },
  weekHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: Spacing.lg,
    paddingVertical: Spacing.md,
    backgroundColor: C.surface,
    borderBottomWidth: 1,
    borderBottomColor: C.divider,
  },
  weekLabel: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
  daysRow: { backgroundColor: C.surface, maxHeight: 88 },
  daysRowContent: { paddingHorizontal: Spacing.md, paddingVertical: Spacing.sm, gap: Spacing.sm },
  dayPill: {
    alignItems: 'center',
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
    borderRadius: BorderRadius.xl,
    minWidth: 52,
    borderWidth: 1,
    borderColor: C.divider,
  },
  dayPillSelected: { backgroundColor: C.primary, borderColor: C.primary },
  dayPillName: { fontSize: FontSize.xs, color: C.textSecondary, fontWeight: FontWeight.medium },
  dayPillNumber: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text, marginTop: 2 },
  dayPillTextSelected: { color: C.onPrimary },
  dayDot: { width: 6, height: 6, borderRadius: 3, backgroundColor: C.primary, marginTop: 2 },
  dayDotSelected: { backgroundColor: C.surface },
  todayIndicator: {
    position: 'absolute',
    top: 4,
    right: 4,
    width: 6,
    height: 6,
    borderRadius: 3,
    backgroundColor: C.secondary,
  },
  content: { flex: 1 },
  contentPadding: { padding: Spacing.lg, gap: Spacing.md, paddingBottom: Spacing.xl },
  dayHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: Spacing.sm },
  dayTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text },
  addDayButton: { flexDirection: 'row', alignItems: 'center', gap: Spacing.xs },
  addDayButtonText: { color: C.primary, fontWeight: FontWeight.medium },
  mealSlot: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: C.surface,
    borderRadius: BorderRadius.lg,
    padding: Spacing.md,
    ...Shadows.sm,
  },
  mealSlotLeft: { flex: 1 },
  mealSlotType: { fontSize: FontSize.sm, color: C.textSecondary, fontWeight: FontWeight.medium, textTransform: 'uppercase', letterSpacing: 0.5 },
  mealSlotName: { fontSize: FontSize.md, color: C.text, fontWeight: FontWeight.semibold, marginTop: 2 },
  mealSlotEmpty: { fontSize: FontSize.md, color: C.textHint, fontStyle: 'italic', marginTop: 2 },
  mealSlotLogged: { fontSize: FontSize.md, color: C.textSecondary, fontStyle: 'italic', marginTop: 2 },
  generateBanner: {
    backgroundColor: C.surface,
    borderRadius: BorderRadius.xl,
    padding: Spacing.xl,
    alignItems: 'center',
    gap: Spacing.sm,
    marginTop: Spacing.md,
    ...Shadows.md,
    borderWidth: 1,
    borderColor: C.primaryLight,
  },
  generateTitle: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: C.text },
  generateSubtitle: { fontSize: FontSize.md, color: C.textSecondary, textAlign: 'center' },
  generateButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.sm,
    backgroundColor: C.secondary,
    paddingHorizontal: Spacing.xl,
    paddingVertical: Spacing.md,
    borderRadius: BorderRadius.lg,
    marginTop: Spacing.sm,
  },
  generateButtonText: { color: C.onPrimary, fontWeight: FontWeight.semibold, fontSize: FontSize.md },
  generatingState: { alignItems: 'center', gap: Spacing.md, paddingVertical: Spacing.xl },
  generatingText: { fontSize: FontSize.lg, fontWeight: FontWeight.semibold, color: C.text },
  generatingSubText: { fontSize: FontSize.md, color: C.textSecondary },
  modalOverlay: { flex: 1, backgroundColor: C.overlay, justifyContent: 'center', alignItems: 'center', padding: Spacing.xl },
  modalContent: { backgroundColor: C.surface, borderRadius: BorderRadius.xl, padding: Spacing.xl, alignItems: 'center', gap: Spacing.md, width: '100%' },
  modalTitle: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: C.text },
  modalText: { fontSize: FontSize.md, color: C.textSecondary, textAlign: 'center', lineHeight: 22 },
  modalLabel: {
    alignSelf: 'flex-start',
    marginTop: Spacing.md,
    fontSize: FontSize.sm,
    fontWeight: FontWeight.semibold,
    color: C.text,
  },
  modalInput: {
    width: '100%',
    marginTop: Spacing.xs,
    minHeight: 72,
    borderWidth: 1,
    borderColor: C.divider,
    borderRadius: BorderRadius.md,
    paddingHorizontal: Spacing.sm,
    paddingVertical: Spacing.sm,
    fontSize: FontSize.sm,
    color: C.text,
    backgroundColor: C.background,
  },
  modalHint: {
    alignSelf: 'flex-start',
    marginTop: 4,
    fontSize: FontSize.xs,
    color: C.textSecondary,
  },
  modalActions: { flexDirection: 'row', gap: Spacing.md, marginTop: Spacing.sm, width: '100%' },
  modalCancel: { flex: 1, borderWidth: 2, borderColor: C.divider, borderRadius: BorderRadius.lg, paddingVertical: Spacing.md, alignItems: 'center' },
  modalCancelText: { color: C.textSecondary, fontWeight: FontWeight.semibold },
  modalConfirm: { flex: 1, backgroundColor: C.secondary, borderRadius: BorderRadius.lg, paddingVertical: Spacing.md, alignItems: 'center' },
  modalConfirmText: { color: C.onPrimary, fontWeight: FontWeight.semibold },
});
