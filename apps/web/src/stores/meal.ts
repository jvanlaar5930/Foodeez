import { ref } from 'vue';
import { defineStore } from 'pinia';
import { format } from 'date-fns';
import { mealService } from '@/services/mealService';
import { useAsyncState } from '@/stores/asyncState';
import type { LogMealRequest, MealLog, NutritionSummary } from '@foodeez/shared';

export const useMealStore = defineStore('meal', () => {
  const dailyLogs = ref<MealLog[]>([]);
  const nutritionSummary = ref<NutritionSummary | null>(null);
  const selectedDate = ref<string>(format(new Date(), 'yyyy-MM-dd'));
  const { isLoading, error, run, runOrThrow } = useAsyncState();

  async function refreshDay(userId: string, date: string): Promise<void> {
    await Promise.all([fetchDailyLogs(userId, date), fetchNutritionSummary(userId, date)]);
  }

  async function fetchDailyLogs(userId: string, date: string): Promise<void> {
    const logs = await run("That day's meals could not be loaded.", () =>
      mealService.getDailyLogs(userId, date),
    );

    if (logs) {
      dailyLogs.value = logs;
    }
  }

  async function fetchNutritionSummary(userId: string, date: string): Promise<void> {
    const summary = await run("That day's totals could not be loaded.", () =>
      mealService.getNutritionSummary(userId, date),
    );

    if (summary) {
      nutritionSummary.value = summary;
    }
  }

  // The three writes rethrow: each is triggered by a dialog or a row that has to know whether
  // it may close or disappear.

  async function logMeal(data: LogMealRequest): Promise<void> {
    await runOrThrow('That meal could not be saved.', async () => {
      await mealService.logMeal(data);
      await refreshDay(data.userId, data.logDate);
    });
  }

  async function updateMealLog(mealLogId: string, data: LogMealRequest): Promise<void> {
    await runOrThrow('That meal could not be updated.', async () => {
      await mealService.updateMealLog(mealLogId, data);
      await refreshDay(data.userId, data.logDate);
    });
  }

  async function deleteMealLog(mealLogId: string, userId: string): Promise<void> {
    await runOrThrow('That meal could not be deleted.', async () => {
      await mealService.deleteMealLog(mealLogId);
      await refreshDay(userId, selectedDate.value);
    });
  }

  function setSelectedDate(date: string): void {
    selectedDate.value = date;
  }

  return {
    dailyLogs,
    nutritionSummary,
    selectedDate,
    isLoading,
    error,
    fetchDailyLogs,
    fetchNutritionSummary,
    refreshDay,
    logMeal,
    updateMealLog,
    deleteMealLog,
    setSelectedDate,
  };
});
