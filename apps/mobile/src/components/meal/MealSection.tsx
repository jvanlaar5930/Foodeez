import React, { useState } from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { MealLogItemRow } from './MealLogItemRow';
import { FontSize, FontWeight, Spacing, BorderRadius } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import { scoreColor } from '@/utils/analysisScore';
import { MealType, type MealLogDto } from '@/types';

interface MealSectionProps {
  mealLog: MealLogDto;
  onEditMeal?: (mealLog: MealLogDto) => void;
  onDeleteMeal?: (mealLog: MealLogDto) => void;
}

const MEAL_TYPE_LABELS: Record<MealType, string> = {
  [MealType.Breakfast]: 'Breakfast',
  [MealType.MorningSnack]: 'Morning Snack',
  [MealType.Lunch]: 'Lunch',
  [MealType.AfternoonSnack]: 'Afternoon Snack',
  [MealType.Dinner]: 'Dinner',
  [MealType.EveningSnack]: 'Evening Snack',
};

const MEAL_TYPE_ICONS: Record<MealType, keyof typeof Ionicons.glyphMap> = {
  [MealType.Breakfast]: 'sunny-outline',
  [MealType.MorningSnack]: 'cafe-outline',
  [MealType.Lunch]: 'restaurant-outline',
  [MealType.AfternoonSnack]: 'nutrition-outline',
  [MealType.Dinner]: 'moon-outline',
  [MealType.EveningSnack]: 'ice-cream-outline',
};

export function MealSection({ mealLog, onEditMeal, onDeleteMeal }: MealSectionProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const [isExpanded, setIsExpanded] = useState(true);

  const calories = Math.round(mealLog.totalNutrition.calories);
  const label = MEAL_TYPE_LABELS[mealLog.mealType] ?? mealLog.mealType;
  const icon = MEAL_TYPE_ICONS[mealLog.mealType] ?? 'restaurant-outline';

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.headerMain}
          onPress={() => setIsExpanded((prev) => !prev)}
          activeOpacity={0.7}
        >
          <View style={styles.headerLeft}>
            <View style={styles.iconBadge}>
              <Ionicons name={icon} size={16} color={C.primary} />
            </View>
            <View>
              <View style={styles.mealLabelRow}>
                <Text style={styles.mealLabel}>{label}</Text>
                {/* Only for a meal that already has an analysis stored: this reads what is
                    on file and never spends an AI call, so it is safe on every row. */}
                {mealLog.analysis && (
                  <View
                    style={[
                      styles.scoreBadge,
                      { borderColor: scoreColor(mealLog.analysis.score) },
                    ]}
                  >
                    <Text style={[styles.scoreBadgeText, { color: scoreColor(mealLog.analysis.score) }]}>
                      {mealLog.analysis.score}
                    </Text>
                  </View>
                )}
              </View>
              <Text style={styles.itemCount}>
                {mealLog.items.length} item{mealLog.items.length !== 1 ? 's' : ''}
              </Text>
            </View>
          </View>
          <View style={styles.headerRight}>
            <Text style={styles.calories}>{calories} kcal</Text>
            <Ionicons
              name={isExpanded ? 'chevron-up' : 'chevron-down'}
              size={20}
              color={C.textSecondary}
            />
          </View>
        </TouchableOpacity>
        <View style={styles.actions}>
          <TouchableOpacity onPress={() => onEditMeal?.(mealLog)} hitSlop={8}>
            <Text style={styles.editAction}>Edit</Text>
          </TouchableOpacity>
          <TouchableOpacity onPress={() => onDeleteMeal?.(mealLog)} hitSlop={8}>
            <Text style={styles.deleteAction}>Delete</Text>
          </TouchableOpacity>
        </View>
      </View>

      {isExpanded && (
        <View style={styles.itemsList}>
          {mealLog.items.map((item) => (
            <MealLogItemRow key={item.id} item={item} />
          ))}
        </View>
      )}
    </View>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: {
    backgroundColor: C.surface,
    borderRadius: 12,
    marginBottom: Spacing.sm,
    overflow: 'hidden',
  },
  header: {
    paddingHorizontal: Spacing.md,
    paddingTop: Spacing.md,
    paddingBottom: Spacing.sm,
  },
  headerMain: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  headerLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.sm,
  },
  iconBadge: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: C.primaryLight,
    justifyContent: 'center',
    alignItems: 'center',
  },
  mealLabelRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  mealLabel: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.semibold,
    color: C.text,
  },
  scoreBadge: {
    borderWidth: 1.5,
    borderRadius: BorderRadius.full,
    paddingHorizontal: 6,
    paddingVertical: 1,
  },
  scoreBadgeText: {
    fontSize: FontSize.xs,
    fontWeight: FontWeight.bold,
  },
  itemCount: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
  },
  headerRight: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
  },
  calories: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.semibold,
    color: C.primary,
  },
  actions: {
    flexDirection: 'row',
    gap: Spacing.md,
    justifyContent: 'flex-end',
    marginTop: Spacing.sm,
  },
  editAction: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.medium,
    color: C.primary,
  },
  deleteAction: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.medium,
    color: C.error,
  },
  itemsList: {
    borderTopWidth: 1,
    borderTopColor: C.divider,
  },
});
