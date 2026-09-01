import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import type { MealLogItemDto } from '@/types';

interface MealLogItemRowProps {
  item: MealLogItemDto;
}

export function MealLogItemRow({ item }: MealLogItemRowProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const calories = Math.round(item.nutritionalInfo.calories);
  const protein = Math.round(item.nutritionalInfo.protein);
  const carbs = Math.round(item.nutritionalInfo.carbohydrates);
  const fat = Math.round(item.nutritionalInfo.fat);

  return (
    <TouchableOpacity style={styles.container} activeOpacity={0.7}>
      <View style={styles.left}>
        <Text style={styles.foodName} numberOfLines={1}>
          {item.foodItem.name}
        </Text>
        <Text style={styles.serving}>
          {item.quantity} {item.unit}
          {item.foodItem.brand ? ` · ${item.foodItem.brand}` : ''}
        </Text>
        <View style={styles.macroRow}>
          <Text style={styles.macroText}>P: {protein}g</Text>
          <Text style={styles.macroDivider}>·</Text>
          <Text style={styles.macroText}>C: {carbs}g</Text>
          <Text style={styles.macroDivider}>·</Text>
          <Text style={styles.macroText}>F: {fat}g</Text>
        </View>
      </View>
      <View style={styles.right}>
        <Text style={styles.calories}>{calories}</Text>
        <Text style={styles.kcalLabel}>kcal</Text>
      </View>
    </TouchableOpacity>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: C.divider,
  },
  left: {
    flex: 1,
    marginRight: Spacing.sm,
  },
  foodName: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.medium,
    color: C.text,
    marginBottom: 2,
  },
  serving: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
    marginBottom: 4,
  },
  macroRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  macroText: {
    fontSize: FontSize.xs,
    color: C.textHint,
  },
  macroDivider: {
    fontSize: FontSize.xs,
    color: C.textHint,
  },
  right: {
    alignItems: 'flex-end',
    minWidth: 52,
  },
  calories: {
    fontSize: FontSize.lg,
    fontWeight: FontWeight.semibold,
    color: C.text,
  },
  kcalLabel: {
    fontSize: FontSize.xs,
    color: C.textSecondary,
  },
});
