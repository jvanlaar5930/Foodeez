import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  FlatList,
  Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { format, parseISO } from 'date-fns';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { MealPlanStackParamList } from '@/navigation/types';
import { useMealPlanStore } from '@/store/mealPlanStore';
import { MealPlanEntryDto, MealType } from '@/types';
import { Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

type Props = NativeStackScreenProps<MealPlanStackParamList, 'CalendarDay'>;

const MEAL_TYPE_LABELS: Record<MealType, string> = {
  [MealType.Breakfast]: 'Breakfast',
  [MealType.MorningSnack]: 'Morning Snack',
  [MealType.Lunch]: 'Lunch',
  [MealType.AfternoonSnack]: 'Afternoon Snack',
  [MealType.Dinner]: 'Dinner',
  [MealType.EveningSnack]: 'Evening Snack',
};

const MEAL_TYPE_ICONS: Record<MealType, string> = {
  [MealType.Breakfast]: 'sunny-outline',
  [MealType.MorningSnack]: 'cafe-outline',
  [MealType.Lunch]: 'restaurant-outline',
  [MealType.AfternoonSnack]: 'nutrition-outline',
  [MealType.Dinner]: 'moon-outline',
  [MealType.EveningSnack]: 'ice-cream-outline',
};

const ALL_MEAL_TYPES = [
  MealType.Breakfast,
  MealType.MorningSnack,
  MealType.Lunch,
  MealType.AfternoonSnack,
  MealType.Dinner,
  MealType.EveningSnack,
];

/**
 * What to call a planned meal. AI-generated entries have no Recipe or FoodItem row behind
 * them, so both names are null and the meal's own name is in `notes` - without this every
 * generated slot read "Custom meal".
 */
function entryLabel(entry: MealPlanEntryDto): string {
  return entry.recipeName ?? entry.foodItemName ?? entry.notes ?? 'Custom meal';
}

export function CalendarDayScreen({ route, navigation }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const { date } = route.params;
  const parsedDate = parseISO(date);
  const { activePlan } = useMealPlanStore();

  // `?.` on activePlan alone was not enough - `entries` does not exist on the payload, so
  // the optional chain resolved to undefined and .filter threw.
  const dayEntries = activePlan?.entriesByDate?.[date] ?? [];

  const getEntryForMeal = (mealType: MealType): MealPlanEntryDto | undefined =>
    dayEntries.find(e => e.mealType === mealType);

  const handleDeleteEntry = (entry: MealPlanEntryDto) => {
    Alert.alert('Remove Meal', `Remove ${entryLabel(entry)} from this slot?`, [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Remove',
        style: 'destructive',
        onPress: () => {
          // TODO: call mealPlanStore.deleteEntry(entry.id)
        },
      },
    ]);
  };

  const handleAISuggest = (mealType: MealType) => {
    Alert.alert('AI Suggestion', `AI will suggest a ${MEAL_TYPE_LABELS[mealType]} based on your goals.`, [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Get Suggestion', onPress: () => { /* TODO: call AI suggest */ } },
    ]);
  };

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()}>
          <Ionicons name="arrow-back" size={24} color={C.text} />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>{format(parsedDate, 'EEEE, MMMM d')}</Text>
        <View style={{ width: 24 }} />
      </View>

      <FlatList
        data={ALL_MEAL_TYPES}
        keyExtractor={item => item.toString()}
        contentContainerStyle={styles.list}
        renderItem={({ item: mealType }) => {
          const entry = getEntryForMeal(mealType);
          return (
            <View style={styles.slotCard}>
              <View style={styles.slotHeader}>
                <View style={styles.slotTypeRow}>
                  <Ionicons name={MEAL_TYPE_ICONS[mealType] as any} size={18} color={C.primary} />
                  <Text style={styles.slotTypeName}>{MEAL_TYPE_LABELS[mealType]}</Text>
                </View>
              </View>

              {entry ? (
                <View style={styles.entryContent}>
                  <View style={styles.entryInfo}>
                    <Text style={styles.entryName}>{entryLabel(entry)}</Text>
                    {entry.servings > 1 && (
                      <Text style={styles.entryServings}>{entry.servings} servings</Text>
                    )}
                    {entry.notes && entry.notes !== entryLabel(entry) && (
                      <Text style={styles.entryNotes}>{entry.notes}</Text>
                    )}
                  </View>
                  <TouchableOpacity style={styles.deleteButton} onPress={() => handleDeleteEntry(entry)}>
                    <Ionicons name="trash-outline" size={18} color={C.error} />
                  </TouchableOpacity>
                </View>
              ) : (
                <View style={styles.emptySlot}>
                  <TouchableOpacity style={styles.addButton}>
                    <Ionicons name="add" size={16} color={C.primary} />
                    <Text style={styles.addButtonText}>Add Meal</Text>
                  </TouchableOpacity>
                  <TouchableOpacity style={styles.aiButton} onPress={() => handleAISuggest(mealType)}>
                    <Ionicons name="sparkles" size={16} color={C.secondary} />
                    <Text style={styles.aiButtonText}>Ask AI</Text>
                  </TouchableOpacity>
                </View>
              )}
            </View>
          );
        }}
      />
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: { flex: 1, backgroundColor: C.background },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: Spacing.lg,
    paddingVertical: Spacing.md,
    backgroundColor: C.surface,
    borderBottomWidth: 1,
    borderBottomColor: C.divider,
  },
  headerTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text },
  list: { padding: Spacing.md, gap: Spacing.md },
  slotCard: {
    backgroundColor: C.surface,
    borderRadius: BorderRadius.lg,
    overflow: 'hidden',
    ...Shadows.sm,
  },
  slotHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: Spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: C.divider,
  },
  slotTypeRow: { flexDirection: 'row', alignItems: 'center', gap: Spacing.sm },
  slotTypeName: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
  entryContent: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: Spacing.md,
  },
  entryInfo: { flex: 1 },
  entryName: { fontSize: FontSize.md, fontWeight: FontWeight.medium, color: C.text },
  entryServings: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2 },
  entryNotes: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2, fontStyle: 'italic' },
  deleteButton: { padding: Spacing.sm },
  emptySlot: { flexDirection: 'row', gap: Spacing.sm, padding: Spacing.md },
  addButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    borderWidth: 1,
    borderColor: C.primary,
    borderRadius: BorderRadius.md,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
  },
  addButtonText: { color: C.primary, fontSize: FontSize.sm, fontWeight: FontWeight.medium },
  aiButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    borderWidth: 1,
    borderColor: C.secondary,
    borderRadius: BorderRadius.md,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
  },
  aiButtonText: { color: C.secondary, fontSize: FontSize.sm, fontWeight: FontWeight.medium },
});
