import React from 'react';
import { ScrollView, StyleSheet, Text, TouchableOpacity } from 'react-native';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useThemedStyles, type Palette } from '@/theme';
import { MEAL_TYPE_SHORT_LABELS, MealType, ORDERED_MEAL_TYPES } from '@/types';

interface MealTypeChipsProps {
  value: MealType;
  onChange: (mealType: MealType) => void;
}

/**
 * The row of meal slots across the top of the add-meal screen. In the order a day happens,
 * from the shared package, so web and mobile name and order them the same way.
 */
export function MealTypeChips({ value, onChange }: MealTypeChipsProps) {
  const styles = useThemedStyles(makeStyles);

  return (
    <ScrollView
      horizontal
      showsHorizontalScrollIndicator={false}
      contentContainerStyle={styles.row}
      style={styles.scroll}
    >
      {ORDERED_MEAL_TYPES.map((mealType) => {
        const selected = value === mealType;

        return (
          <TouchableOpacity
            key={mealType}
            style={[styles.chip, selected && styles.chipActive]}
            onPress={() => onChange(mealType)}
            accessibilityRole="button"
            accessibilityState={{ selected }}
          >
            <Text style={[styles.label, selected && styles.labelActive]}>
              {MEAL_TYPE_SHORT_LABELS[mealType]}
            </Text>
          </TouchableOpacity>
        );
      })}
    </ScrollView>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    scroll: {
      flexGrow: 0,
      backgroundColor: C.surface,
      borderBottomWidth: 1,
      borderBottomColor: C.divider,
    },
    row: { paddingHorizontal: Spacing.md, paddingVertical: Spacing.sm, gap: Spacing.sm },
    chip: {
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.xs + 2,
      borderRadius: BorderRadius.full,
      backgroundColor: C.background,
      borderWidth: 1.5,
      borderColor: C.divider,
    },
    chipActive: { backgroundColor: C.primary, borderColor: C.primary },
    label: { fontSize: FontSize.sm, fontWeight: FontWeight.medium, color: C.textSecondary },
    labelActive: { color: C.onPrimary },
  });
