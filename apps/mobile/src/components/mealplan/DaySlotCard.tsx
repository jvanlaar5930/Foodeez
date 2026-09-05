import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { entryLabel, loggedLabel } from '@/utils/mealPlanLabels';
import { MEAL_TYPE_ICONS } from '@/constants/mealTypes';
import { BorderRadius, FontSize, FontWeight, Shadows, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import {
  MEAL_TYPE_LABELS,
  type MealLogDto,
  type MealPlanEntryDto,
  type MealType,
} from '@/types';

interface DaySlotCardProps {
  mealType: MealType;
  /** What is planned for this slot, if anything. */
  entry?: MealPlanEntryDto;
  /** What was actually eaten, shown only when nothing is planned. */
  log?: MealLogDto;
  onEditEntry: (mealType: MealType) => void;
  onDeleteEntry: (entry: MealPlanEntryDto) => void;
  onEditLog: (log: MealLogDto) => void;
  onAskAI: (mealType: MealType) => void;
}

/**
 * One slot of a planned day: what is in it, or the two ways to fill it.
 *
 * Memoised, and lifted out of the list's renderItem. It was a sixty-line closure rebuilt on
 * every render of the screen, which gave FlatList a new component type each time and left it
 * unable to recycle anything.
 */
export const DaySlotCard = React.memo(function DaySlotCard({
  mealType,
  entry,
  log,
  onEditEntry,
  onDeleteEntry,
  onEditLog,
  onAskAI,
}: DaySlotCardProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  return (
    <View style={styles.card}>
      <View style={styles.header}>
        <View style={styles.typeRow}>
          <Ionicons name={MEAL_TYPE_ICONS[mealType]} size={18} color={C.primary} />
          <Text style={styles.typeName}>{MEAL_TYPE_LABELS[mealType]}</Text>
        </View>
      </View>

      {entry ? (
        <TouchableOpacity
          style={styles.content}
          onPress={() => onEditEntry(mealType)}
          accessibilityLabel={`Edit ${entryLabel(entry)}`}
        >
          <View style={styles.info}>
            <Text style={styles.name}>{entryLabel(entry)}</Text>
            {entry.servings > 1 ? (
              <Text style={styles.servings}>{entry.servings} servings</Text>
            ) : null}
            {entry.notes && entry.notes !== entryLabel(entry) ? (
              <Text style={styles.notes}>{entry.notes}</Text>
            ) : null}
          </View>
          <TouchableOpacity
            style={styles.delete}
            accessibilityLabel={`Remove ${entryLabel(entry)}`}
            onPress={(event) => {
              // Otherwise the tap also opens the edit dialog underneath it.
              event.stopPropagation();
              onDeleteEntry(entry);
            }}
          >
            <Ionicons name="trash-outline" size={18} color={C.error} />
          </TouchableOpacity>
        </TouchableOpacity>
      ) : log ? (
        <TouchableOpacity
          style={styles.content}
          onPress={() => onEditLog(log)}
          accessibilityLabel={`Edit logged ${loggedLabel(log)}`}
        >
          <View style={styles.info}>
            <Text style={styles.name}>{loggedLabel(log)}</Text>
            <Text style={styles.loggedTag}>Logged as eaten - tap to edit</Text>
          </View>
          <Ionicons name="restaurant" size={18} color={C.textSecondary} />
        </TouchableOpacity>
      ) : (
        <View style={styles.empty}>
          <TouchableOpacity style={styles.add} onPress={() => onEditEntry(mealType)}>
            <Ionicons name="add" size={16} color={C.primary} />
            <Text style={styles.addText}>Add Meal</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.ai} onPress={() => onAskAI(mealType)}>
            <Ionicons name="sparkles" size={16} color={C.secondary} />
            <Text style={styles.aiText}>Ask AI</Text>
          </TouchableOpacity>
        </View>
      )}
    </View>
  );
});

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    card: {
      backgroundColor: C.surface,
      borderRadius: BorderRadius.lg,
      padding: Spacing.md,
      marginBottom: Spacing.sm,
      ...Shadows.sm,
    },
    header: { flexDirection: 'row', alignItems: 'center', marginBottom: Spacing.sm },
    typeRow: { flexDirection: 'row', alignItems: 'center', gap: Spacing.xs },
    typeName: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
    content: { flexDirection: 'row', alignItems: 'center', gap: Spacing.sm },
    info: { flex: 1 },
    name: { fontSize: FontSize.md, color: C.text },
    servings: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 2 },
    notes: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 2 },
    loggedTag: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 2 },
    delete: { padding: Spacing.xs },
    empty: { flexDirection: 'row', gap: Spacing.sm },
    add: {
      flex: 1,
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      gap: Spacing.xs,
      borderWidth: 1,
      borderStyle: 'dashed',
      borderColor: C.primary,
      borderRadius: BorderRadius.md,
      paddingVertical: Spacing.sm,
    },
    addText: { fontSize: FontSize.sm, color: C.primary, fontWeight: FontWeight.medium },
    ai: {
      flex: 1,
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      gap: Spacing.xs,
      borderWidth: 1,
      borderStyle: 'dashed',
      borderColor: C.secondary,
      borderRadius: BorderRadius.md,
      paddingVertical: Spacing.sm,
    },
    aiText: { fontSize: FontSize.sm, color: C.secondary, fontWeight: FontWeight.medium },
  });
