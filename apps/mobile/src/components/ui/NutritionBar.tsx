import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

interface NutritionBarProps {
  label: string;
  current: number;
  target: number;
  color: string;
  unit: string;
}

export function NutritionBar({ label, current, target, color, unit }: NutritionBarProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const percentage = target > 0 ? Math.min(100, Math.round((current / target) * 100)) : 0;

  return (
    <View style={styles.container}>
      <View style={styles.labelRow}>
        <Text style={styles.label}>{label}</Text>
        <Text style={styles.values}>
          <Text style={[styles.current, { color }]}>{Math.round(current)}</Text>
          <Text style={styles.separator}> / </Text>
          <Text style={styles.target}>
            {Math.round(target)}
            {unit}
          </Text>
        </Text>
      </View>
      <View style={styles.track}>
        <View
          style={[
            styles.fill,
            { width: `${percentage}%`, backgroundColor: color },
          ]}
        />
      </View>
    </View>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: {
    marginBottom: Spacing.sm,
  },
  labelRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: Spacing.xs,
  },
  label: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.medium,
    color: C.text,
  },
  values: {
    fontSize: FontSize.sm,
  },
  current: {
    fontWeight: FontWeight.semibold,
  },
  separator: {
    color: C.textHint,
  },
  target: {
    color: C.textSecondary,
  },
  track: {
    height: 8,
    backgroundColor: C.divider,
    borderRadius: BorderRadius.full,
    overflow: 'hidden',
  },
  fill: {
    height: '100%',
    borderRadius: BorderRadius.full,
  },
});
