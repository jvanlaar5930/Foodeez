import React, { useState } from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { MealLogItemRow } from './MealLogItemRow';
import { Colors, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { MealType, type MealLogDto, type MealLogItemDto, type NutritionalInfo } from '@/types';

interface MealSectionProps {
  mealLog: MealLogDto;
  onDeleteItem?: (mealLogId: string, itemId: string) => void;
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

export function MealSection({ mealLog, onDeleteItem }: MealSectionProps) {
  const [isExpanded, setIsExpanded] = useState(true);

  const calories = Math.round(mealLog.totalNutrition.calories);
  const label = MEAL_TYPE_LABELS[mealLog.mealType] ?? mealLog.mealType;
  const icon = MEAL_TYPE_ICONS[mealLog.mealType] ?? 'restaurant-outline';

  return (
    <View style={styles.container}>
      <TouchableOpacity
        style={styles.header}
        onPress={() => setIsExpanded((prev) => !prev)}
        activeOpacity={0.7}
      >
        <View style={styles.headerLeft}>
          <View style={styles.iconBadge}>
            <Ionicons name={icon} size={16} color={Colors.primary} />
          </View>
          <View>
            <Text style={styles.mealLabel}>{label}</Text>
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
            color={Colors.textSecondary}
          />
        </View>
      </TouchableOpacity>

      {isExpanded && (
        <View style={styles.itemsList}>
          {mealLog.items.map((item) => (
            <MealLogItemRow
              key={item.id}
              item={item}
              onDelete={
                onDeleteItem ? () => onDeleteItem(mealLog.id, item.id) : undefined
              }
            />
          ))}
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: Colors.surface,
    borderRadius: 12,
    marginBottom: Spacing.sm,
    overflow: 'hidden',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: Spacing.md,
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
    backgroundColor: Colors.primaryLight,
    justifyContent: 'center',
    alignItems: 'center',
  },
  mealLabel: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.semibold,
    color: Colors.text,
  },
  itemCount: {
    fontSize: FontSize.sm,
    color: Colors.textSecondary,
  },
  headerRight: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
  },
  calories: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.semibold,
    color: Colors.primary,
  },
  itemsList: {
    borderTopWidth: 1,
    borderTopColor: Colors.divider,
  },
});
