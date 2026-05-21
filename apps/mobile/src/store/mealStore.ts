import { create } from 'zustand';
import * as mealService from '@/services/mealService';
import { formatApiDate } from '@/utils/dateUtils';
import type { LogMealRequest, MealLogDto, NutritionSummaryDto } from '@/types';

interface MealState {
  dailyLogs: MealLogDto[];
  nutritionSummary: NutritionSummaryDto | null;
  selectedDate: string;
  isLoading: boolean;
  error: string | null;
  setSelectedDate: (date: string) => void;
  fetchDailyLogs: (userId: string, date: string) => Promise<void>;
  fetchNutritionSummary: (userId: string, date: string) => Promise<void>;
  logMeal: (data: LogMealRequest) => Promise<void>;
  deleteMealLog: (mealLogId: string) => Promise<void>;
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

  fetchDailyLogs: async (userId: string, date: string) => {
    set({ isLoading: true, error: null });
    try {
      const logs = await mealService.getDailyLogs(userId, date);
      set({ dailyLogs: logs, isLoading: false });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to fetch meal logs.';
      set({ isLoading: false, error: message });
    }
  },

  fetchNutritionSummary: async (userId: string, date: string) => {
    set({ isLoading: true, error: null });
    try {
      const summary = await mealService.getNutritionSummary(userId, date);
      set({ nutritionSummary: summary, isLoading: false });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to fetch nutrition summary.';
      set({ isLoading: false, error: message });
    }
  },

  logMeal: async (data: LogMealRequest) => {
    set({ isLoading: true, error: null });
    try {
      const newLog = await mealService.logMeal(data);
      const currentLogs = get().dailyLogs;
      set({ dailyLogs: [...currentLogs, newLog], isLoading: false });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to log meal.';
      set({ isLoading: false, error: message });
      throw err;
    }
  },

  deleteMealLog: async (mealLogId: string) => {
    set({ isLoading: true, error: null });
    try {
      await mealService.deleteMealLog(mealLogId);
      const currentLogs = get().dailyLogs.filter((log) => log.id !== mealLogId);
      set({ dailyLogs: currentLogs, isLoading: false });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to delete meal log.';
      set({ isLoading: false, error: message });
      throw err;
    }
  },

  clearError: () => {
    set({ error: null });
  },
}));
