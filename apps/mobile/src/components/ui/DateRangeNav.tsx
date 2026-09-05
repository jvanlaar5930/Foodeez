import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

interface DateRangeNavProps {
  /** What the current position is called - "Today", "Mon 3 Mar", "3-9 March". */
  label: string;
  onPrevious: () => void;
  onNext: () => void;
  /**
   * Stops the reader paging into days that do not exist yet. A meal log has nothing to show
   * for tomorrow; a meal plan legitimately does.
   */
  canGoNext?: boolean;
  /** A "Today" shortcut, shown only when there is somewhere to go back to. */
  onReset?: () => void;
  resetLabel?: string;
}

/**
 * Back / here / forward, over days or weeks.
 *
 * Three screens built this themselves and two of them let you page indefinitely into the
 * future, where every day is empty by definition.
 */
export function DateRangeNav({
  label,
  onPrevious,
  onNext,
  canGoNext = true,
  onReset,
  resetLabel = 'Today',
}: DateRangeNavProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  return (
    <View style={styles.row}>
      <TouchableOpacity
        onPress={onPrevious}
        accessibilityRole="button"
        accessibilityLabel="Previous"
        hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
      >
        <Ionicons name="chevron-back" size={22} color={C.text} />
      </TouchableOpacity>

      <View style={styles.middle}>
        <Text style={styles.label} numberOfLines={1}>
          {label}
        </Text>
        {onReset ? (
          <TouchableOpacity onPress={onReset} accessibilityRole="button">
            <Text style={styles.reset}>{resetLabel}</Text>
          </TouchableOpacity>
        ) : null}
      </View>

      <TouchableOpacity
        onPress={onNext}
        disabled={!canGoNext}
        accessibilityRole="button"
        accessibilityLabel="Next"
        accessibilityState={{ disabled: !canGoNext }}
        hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
      >
        <Ionicons name="chevron-forward" size={22} color={canGoNext ? C.text : C.textHint} />
      </TouchableOpacity>
    </View>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    row: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.sm,
      backgroundColor: C.surface,
    },
    middle: { flex: 1, alignItems: 'center' },
    label: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
    reset: { fontSize: FontSize.xs, color: C.primary, marginTop: 2 },
  });
