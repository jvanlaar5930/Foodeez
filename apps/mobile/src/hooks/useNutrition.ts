import { useMemo } from 'react';
import { useMealStore } from '@/store/mealStore';
import { clampPercentage } from '@/utils/nutritionUtils';

export function useNutrition() {
  const nutritionSummary = useMealStore((state) => state.nutritionSummary);

  const computed = useMemo(() => {
    if (!nutritionSummary) {
      return {
        caloriePercentage: 0,
        proteinPercentage: 0,
        carbsPercentage: 0,
        fatPercentage: 0,
        isOverCalorieTarget: false,
        macrosSummary: {
          calories: { current: 0, target: 0 },
          protein: { current: 0, target: 0 },
          carbs: { current: 0, target: 0 },
          fat: { current: 0, target: 0 },
        },
      };
    }

    const {
      totalCalories,
      totalProtein,
      totalCarbs,
      totalFat,
      targetCalories,
      targetProtein,
      targetCarbs,
      targetFat,
    } = nutritionSummary;

    const safePercent = (current: number, target: number) =>
      target > 0 ? clampPercentage(Math.round((current / target) * 100)) : 0;

    return {
      caloriePercentage: safePercent(totalCalories, targetCalories),
      proteinPercentage: safePercent(totalProtein, targetProtein),
      carbsPercentage: safePercent(totalCarbs, targetCarbs),
      fatPercentage: safePercent(totalFat, targetFat),
      isOverCalorieTarget: totalCalories > targetCalories,
      macrosSummary: {
        calories: { current: totalCalories, target: targetCalories },
        protein: { current: totalProtein, target: targetProtein },
        carbs: { current: totalCarbs, target: targetCarbs },
        fat: { current: totalFat, target: targetFat },
      },
    };
  }, [nutritionSummary]);

  return computed;
}
