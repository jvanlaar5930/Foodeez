import { create } from 'zustand';
import * as mealService from '@/services/mealService';
import { runAsync } from '@/store/asyncState';
import { formatApiDate } from '@/utils/dateUtils';
import type { LogMealRequest, MealLogDto, NutritionSummaryDto } from '@/types';

interface MealState {
  dailyLogs: MealLogDto[];
  nutritionSummary: NutritionSummaryDto | null;
  selectedDate: string;
  isLoading: boolean;
  error: string | null;
  setSelectedDate: (date: string) => void;
  refreshDay: (userId: string, date: string) => Promise<void>;
  fetchDailyLogs: (userId: string, date: string) => Promise<void>;
  fetchNutritionSummary: (userId: string, date: string) => Promise<void>;
  logMeal: (data: LogMealRequest) => Promise<void>;
  updateMealLog: (mealLogId: string, data: LogMealRequest) => Promise<void>;
  deleteMealLog: (mealLogId: string, userId: string) => Promise<void>;
  clearError: () => void;
}

export const useMealStore = create<MealState>()((set, get) => ({
  dailyLogs: [],
  nutritionSummary: null,
  selectedDate: formatApiDate(new Date()),
  isLoading: false,
  error: null,

  setSelectedDate: (date: string) => {
    set({ selectedDate: date });
  },

  refreshDay: async (userId: string, date: string) => {
    await runAsync(set, { fallback: 'Could not refresh this day.' }, async () => {
      const [dailyLogs, nutritionSummary] = await Promise.all([
        mealService.getDailyLogs(userId, date),
        mealService.getNutritionSummary(userId, date),
      ]);
      set({ dailyLogs, nutritionSummary });
    });
  },

  fetchDailyLogs: async (userId: string, date: string) => {
    await runAsync(set, { fallback: "That day's meals could not be loaded." }, async () => {
      set({ dailyLogs: await mealService.getDailyLogs(userId, date) });
    });
  },

  fetchNutritionSummary: async (userId: string, date: string) => {
    // No spinner: this runs alongside the meal list, which is already showing one.
    await runAsync(
      set,
      { fallback: "That day's totals could not be loaded.", loading: false },
      async () => {
        set({ nutritionSummary: await mealService.getNutritionSummary(userId, date) });
      },
    );
  },

  // The three writes rethrow: each is triggered by a screen that only closes or navigates
  // away once the save has actually gone through.

  logMeal: async (data: LogMealRequest) => {
    await runAsync(set, { fallback: 'That meal could not be saved.', rethrow: true }, async () => {
      await mealService.logMeal(data);
      await get().refreshDay(data.userId, data.logDate);
    });
  },

  updateMealLog: async (mealLogId: string, data: LogMealRequest) => {
    await runAsync(set, { fallback: 'That meal could not be updated.', rethrow: true }, async () => {
      await mealService.updateMealLog(mealLogId, data);
      await get().refreshDay(data.userId, data.logDate);
    });
  },

  deleteMealLog: async (mealLogId: string, userId: string) => {
    await runAsync(set, { fallback: 'That meal could not be deleted.', rethrow: true }, async () => {
      await mealService.deleteMealLog(mealLogId);
      await get().refreshDay(userId, get().selectedDate);
    });
  },

  clearError: () => {
    set({ error: null });
  },
}));
