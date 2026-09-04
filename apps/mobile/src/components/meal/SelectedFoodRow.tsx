import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import { nutritionOf, stepFor, type MealItem } from '@/hooks/useMealItems';
import type { QuickAddSource } from '@/types';

interface SelectedFoodRowProps {
  entry: MealItem;
  onAdjust: (foodItemId: string, delta: number) => void;
  onRemove: (foodItemId: string) => void;
}

/**
 * Which numbers were looked up and which were guessed at, per line, so a quick-added meal can
 * be checked at a glance instead of taken on faith.
 *
 * Nothing for a line from a saved meal: that is one the reader built and approved themselves,
 * and labelling it would only add noise.
 */
const SOURCE_LABELS: Partial<Record<QuickAddSource, string>> = {
  Estimated: 'AI estimate',
  Matched: 'from database',
};

/**
 * One food in the meal being built.
 *
 * Memoised because it sits in a list that re-renders on every keystroke in the search box
 * above it: without this, typing re-renders every selected row for nothing.
 */
export const SelectedFoodRow = React.memo(function SelectedFoodRow({
  entry,
  onAdjust,
  onRemove,
}: SelectedFoodRowProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  const step = stepFor(entry.foodItem);
  const calories = Math.round(nutritionOf(entry).calories);
  const sourceLabel = entry.source ? SOURCE_LABELS[entry.source] : undefined;

  return (
    <View style={styles.row}>
      <View style={styles.left}>
        <Text style={styles.name} numberOfLines={1}>
          {entry.foodItem.name}
        </Text>
        <View style={styles.meta}>
          <Text style={styles.calories}>{calories} kcal</Text>
          {sourceLabel ? (
            <Text style={[styles.tag, entry.source === 'Estimated' && styles.tagEstimate]}>
              {sourceLabel}
            </Text>
          ) : null}
        </View>
      </View>

      <View style={styles.quantity}>
        <TouchableOpacity
          onPress={() => onAdjust(entry.foodItem.id, -step)}
          style={styles.stepButton}
          accessibilityRole="button"
          accessibilityLabel={`Less ${entry.foodItem.name}`}
        >
          <Text style={styles.stepText}>-</Text>
        </TouchableOpacity>
        <Text style={styles.value}>{entry.quantity}</Text>
        <TouchableOpacity
          onPress={() => onAdjust(entry.foodItem.id, step)}
          style={styles.stepButton}
          accessibilityRole="button"
          accessibilityLabel={`More ${entry.foodItem.name}`}
        >
          <Text style={styles.stepText}>+</Text>
        </TouchableOpacity>
      </View>

      <TouchableOpacity
        onPress={() => onRemove(entry.foodItem.id)}
        style={styles.remove}
        accessibilityRole="button"
        accessibilityLabel={`Remove ${entry.foodItem.name}`}
        hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
      >
        <Ionicons name="close-circle" size={20} color={C.textHint} />
      </TouchableOpacity>
    </View>
  );
});

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    row: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: Spacing.sm,
      backgroundColor: C.surface,
      borderRadius: BorderRadius.md,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.sm,
      marginBottom: Spacing.xs,
    },
    left: { flex: 1 },
    name: { fontSize: FontSize.md, fontWeight: FontWeight.medium, color: C.text },
    meta: { flexDirection: 'row', alignItems: 'center', gap: Spacing.xs, marginTop: 2 },
    calories: { fontSize: FontSize.xs, color: C.textSecondary },
    tag: {
      fontSize: FontSize.xs,
      color: C.textSecondary,
      backgroundColor: C.surfaceAlt,
      paddingHorizontal: 6,
      paddingVertical: 1,
      borderRadius: BorderRadius.sm,
      overflow: 'hidden',
    },
    tagEstimate: { color: C.secondaryDark },
    quantity: { flexDirection: 'row', alignItems: 'center', gap: Spacing.xs },
    stepButton: {
      width: 28,
      height: 28,
      borderRadius: 14,
      backgroundColor: C.surfaceAlt,
      alignItems: 'center',
      justifyContent: 'center',
    },
    stepText: { fontSize: FontSize.md, fontWeight: FontWeight.bold, color: C.text },
    value: { minWidth: 44, textAlign: 'center', fontSize: FontSize.sm, color: C.text },
    remove: { paddingLeft: Spacing.xs },
  });
