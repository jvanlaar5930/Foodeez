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
import { MealPlanEntry, MealType } from '@/types';
import { Colors, Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';

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

export default function CalendarDayScreen({ route, navigation }: Props) {
  const { date } = route.params;
  const parsedDate = parseISO(date);
  const { activePlan } = useMealPlanStore();

  const dayEntries = activePlan?.entries.filter(e => e.entryDate === date) ?? [];

  const getEntryForMeal = (mealType: MealType): MealPlanEntry | undefined =>
    dayEntries.find(e => e.mealType === mealType);

  const handleDeleteEntry = (entry: MealPlanEntry) => {
    Alert.alert('Remove Meal', `Remove ${entry.recipeName ?? entry.foodItemName} from this slot?`, [
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
          <Ionicons name="arrow-back" size={24} color={Colors.text} />
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
                  <Ionicons name={MEAL_TYPE_ICONS[mealType] as any} size={18} color={Colors.primary} />
                  <Text style={styles.slotTypeName}>{MEAL_TYPE_LABELS[mealType]}</Text>
                </View>
              </View>

              {entry ? (
                <View style={styles.entryContent}>
                  <View style={styles.entryInfo}>
                    <Text style={styles.entryName}>{entry.recipeName ?? entry.foodItemName ?? 'Custom meal'}</Text>
                    {entry.servings > 1 && (
                      <Text style={styles.entryServings}>{entry.servings} servings</Text>
                    )}
                    {entry.notes && <Text style={styles.entryNotes}>{entry.notes}</Text>}
                  </View>
                  <TouchableOpacity style={styles.deleteButton} onPress={() => handleDeleteEntry(entry)}>
                    <Ionicons name="trash-outline" size={18} color={Colors.error} />
                  </TouchableOpacity>
                </View>
              ) : (
                <View style={styles.emptySlot}>
                  <TouchableOpacity style={styles.addButton}>
                    <Ionicons name="add" size={16} color={Colors.primary} />
                    <Text style={styles.addButtonText}>Add Meal</Text>
                  </TouchableOpacity>
                  <TouchableOpacity style={styles.aiButton} onPress={() => handleAISuggest(mealType)}>
                    <Ionicons name="sparkles" size={16} color={Colors.secondary} />
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

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: Colors.background },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: Spacing.lg,
    paddingVertical: Spacing.md,
    backgroundColor: Colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: Colors.divider,
  },
  headerTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: Colors.text },
  list: { padding: Spacing.md, gap: Spacing.md },
  slotCard: {
    backgroundColor: Colors.surface,
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
    borderBottomColor: Colors.divider,
  },
  slotTypeRow: { flexDirection: 'row', alignItems: 'center', gap: Spacing.sm },
  slotTypeName: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: Colors.text },
  entryContent: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: Spacing.md,
  },
  entryInfo: { flex: 1 },
  entryName: { fontSize: FontSize.md, fontWeight: FontWeight.medium, color: Colors.text },
  entryServings: { fontSize: FontSize.sm, color: Colors.textSecondary, marginTop: 2 },
  entryNotes: { fontSize: FontSize.sm, color: Colors.textSecondary, marginTop: 2, fontStyle: 'italic' },
  deleteButton: { padding: Spacing.sm },
  emptySlot: { flexDirection: 'row', gap: Spacing.sm, padding: Spacing.md },
  addButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    borderWidth: 1,
    borderColor: Colors.primary,
    borderRadius: BorderRadius.md,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
  },
  addButtonText: { color: Colors.primary, fontSize: FontSize.sm, fontWeight: FontWeight.medium },
  aiButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    borderWidth: 1,
    borderColor: Colors.secondary,
    borderRadius: BorderRadius.md,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
  },
  aiButtonText: { color: Colors.secondary, fontSize: FontSize.sm, fontWeight: FontWeight.medium },
});
