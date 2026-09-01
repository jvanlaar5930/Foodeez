import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { NutritionBar } from '@/components/ui/NutritionBar';
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

interface MacroProgressProps {
  protein: { current: number; target: number };
  carbs: { current: number; target: number };
  fat: { current: number; target: number };
}

const macroColors = (C: Palette) => ({
  protein: C.info,
  carbs: C.secondary,
  fat: '#FFC107',
});

export function MacroProgress({ protein, carbs, fat }: MacroProgressProps) {
  const C = useTheme();
  const MACRO_COLORS = macroColors(C);
  const styles = useThemedStyles(makeStyles);
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Macros</Text>
      <NutritionBar
        label="Protein"
        current={protein.current}
        target={protein.target}
        color={MACRO_COLORS.protein}
        unit="g"
      />
      <NutritionBar
        label="Carbs"
        current={carbs.current}
        target={carbs.target}
        color={MACRO_COLORS.carbs}
        unit="g"
      />
      <NutritionBar
        label="Fat"
        current={fat.current}
        target={fat.target}
        color={MACRO_COLORS.fat}
        unit="g"
      />
    </View>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: {
    padding: Spacing.md,
  },
  title: {
    fontSize: FontSize.lg,
    fontWeight: FontWeight.semibold,
    color: C.text,
    marginBottom: Spacing.sm,
  },
});
