import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { NutritionBar } from '@/components/ui/NutritionBar';
import { Colors, FontSize, FontWeight, Spacing } from '@/constants/theme';

interface MacroProgressProps {
  protein: { current: number; target: number };
  carbs: { current: number; target: number };
  fat: { current: number; target: number };
}

const MACRO_COLORS = {
  protein: Colors.info,
  carbs: Colors.secondary,
  fat: '#FFC107',
};

export function MacroProgress({ protein, carbs, fat }: MacroProgressProps) {
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

const styles = StyleSheet.create({
  container: {
    padding: Spacing.md,
  },
  title: {
    fontSize: FontSize.lg,
    fontWeight: FontWeight.semibold,
    color: Colors.text,
    marginBottom: Spacing.sm,
  },
});
