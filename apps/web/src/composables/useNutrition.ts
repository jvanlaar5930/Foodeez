import { computed } from 'vue';
import { useMealStore } from '@/stores/meal';

export function useNutrition() {
  const mealStore = useMealStore();
  const summary = computed(() => mealStore.nutritionSummary);

  const calorieProgress = computed(() => Math.min(summary.value?.caloriesPercentage ?? 0, 150));

  const macroChartData = computed(() => {
    const s = summary.value;
    if (!s) {
      return {
        labels: ['Protein', 'Carbs', 'Fat'],
        datasets: [
          {
            data: [0, 0, 0],
            backgroundColor: ['#3b82f6', '#f59e0b', '#ef4444'],
            borderWidth: 0,
          },
        ],
      };
    }

    return {
      labels: ['Protein', 'Carbs', 'Fat'],
      datasets: [
        {
          data: [
            Math.round(s.totalProtein),
            Math.round(s.totalCarbs),
            Math.round(s.totalFat),
          ],
          backgroundColor: ['#3b82f6', '#f59e0b', '#ef4444'],
          borderWidth: 0,
        },
      ],
    };
  });

  const calorieStatus = computed<'under' | 'on-track' | 'over'>(() => {
    const pct = summary.value?.caloriesPercentage ?? 0;
    if (pct < 80) return 'under';
    if (pct <= 100) return 'on-track';
    return 'over';
  });

  const calorieStatusColor = computed(() => {
    switch (calorieStatus.value) {
      case 'under':
        return 'text-blue-500';
      case 'on-track':
        return 'text-green-500';
      case 'over':
        return 'text-red-500';
    }
  });

  const remainingCalories = computed(() => {
    const s = summary.value;
    if (!s) return 0;
    return Math.max(0, s.targetCalories - s.totalCalories);
  });

  return {
    summary,
    calorieProgress,
    macroChartData,
    calorieStatus,
    calorieStatusColor,
    remainingCalories,
  };
}
