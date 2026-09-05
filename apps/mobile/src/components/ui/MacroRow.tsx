import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

export interface MacroFigures {
  calories: number;
  protein: number;
  carbs: number;
  fat: number;
}

interface MacroRowProps {
  totals: MacroFigures;
  /** `sm` for a row inside a list item, `md` for a summary panel. */
  size?: 'sm' | 'md';
}

/**
 * Calories and the three macros, side by side.
 *
 * Four screens laid this out themselves, and they rounded differently - one showed a decimal
 * place on protein and the others did not, so the same meal read as 31g in one place and
 * 30.6g in another.
 */
export function MacroRow({ totals, size = 'md' }: MacroRowProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  const macros = [
    { label: 'kcal', value: Math.round(totals.calories), color: C.primary },
    { label: 'protein', value: Math.round(totals.protein), color: C.info, unit: 'g' },
    { label: 'carbs', value: Math.round(totals.carbs), color: C.warning, unit: 'g' },
    { label: 'fat', value: Math.round(totals.fat), color: C.secondary, unit: 'g' },
  ];

  return (
    <View style={styles.row}>
      {macros.map((macro) => (
        <View key={macro.label} style={styles.cell}>
          <Text
            style={[styles.value, size === 'sm' && styles.valueSmall, { color: macro.color }]}
          >
            {macro.value}
            {macro.unit ?? ''}
          </Text>
          <Text style={styles.label}>{macro.label}</Text>
        </View>
      ))}
    </View>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    row: { flexDirection: 'row', gap: Spacing.sm },
    cell: { flex: 1, alignItems: 'center' },
    value: { fontSize: FontSize.xl, fontWeight: FontWeight.bold },
    valueSmall: { fontSize: FontSize.md },
    label: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 2 },
  });
