import { ref } from 'vue';
import { defineStore } from 'pinia';
import { format } from 'date-fns';
import { mealService } from '@/services/mealService';
import type { LogMealRequest, MealLog, NutritionSummary } from '@foodeez/shared';

export const useMealStore = defineStore('meal', () => {
  const dailyLogs = ref<MealLog[]>([]);
  const nutritionSummary = ref<NutritionSummary | null>(null);
  const selectedDate = ref<string>(format(new Date(), 'yyyy-MM-dd'));
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  async function fetchDailyLogs(userId: string, date: string): Promise<void> {
    isLoading.value = true;
    error.value = null;
    try {
      dailyLogs.value = await mealService.getDailyLogs(userId, date);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
    } finally {
      isLoading.value = false;
    }
  }

  async function fetchNutritionSummary(userId: string, date: string): Promise<void> {
    try {
      nutritionSummary.value = await mealService.getNutritionSummary(userId, date);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
    }
  }

  async function logMeal(data: LogMealRequest): Promise<void> {
    isLoading.value = true;
    error.value = null;
    try {
      const newLog = await mealService.logMeal(data);
      dailyLogs.value.push(newLog);
      // Refresh summary after logging
      await fetchNutritionSummary(data.userId, data.logDate);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  async function deleteMealLog(mealLogId: string, userId: string): Promise<void> {
    try {
      await mealService.deleteMealLog(mealLogId);
      dailyLogs.value = dailyLogs.value.filter((log) => log.id !== mealLogId);
      await fetchNutritionSummary(userId, selectedDate.value);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
      throw err;
    }
  }

  function setSelectedDate(date: string): void {
    selectedDate.value = date;
  }

  function extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'response' in err) {
      const e = err as { response?: { data?: { detail?: string; title?: string } } };
      return e.response?.data?.detail ?? e.response?.data?.title ?? 'An error occurred';
    }
    return 'An error occurred';
  }

  return {
    dailyLogs,
    nutritionSummary,
    selectedDate,
    isLoading,
    error,
    fetchDailyLogs,
    fetchNutritionSummary,
    logMeal,
    deleteMealLog,
    setSelectedDate,
  };
});
