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
    set({ isLoading: true, error: null });
    try {
      const [logs, summary] = await Promise.all([
        mealService.getDailyLogs(userId, date),
        mealService.getNutritionSummary(userId, date),
      ]);
      set({ dailyLogs: logs, nutritionSummary: summary, isLoading: false });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to refresh nutrition data.';
      set({ isLoading: false, error: message });
    }
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
    try {
      const summary = await mealService.getNutritionSummary(userId, date);
      set({ nutritionSummary: summary });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to fetch nutrition summary.';
      set({ error: message });
    }
  },

  logMeal: async (data: LogMealRequest) => {
    set({ isLoading: true, error: null });
    try {
      await mealService.logMeal(data);
      await get().refreshDay(data.userId, data.logDate);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to log meal.';
      set({ isLoading: false, error: message });
      throw err;
    }
  },

  updateMealLog: async (mealLogId: string, data: LogMealRequest) => {
    set({ isLoading: true, error: null });
    try {
      await mealService.updateMealLog(mealLogId, data);
      await get().refreshDay(data.userId, data.logDate);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to update meal log.';
      set({ isLoading: false, error: message });
      throw err;
    }
  },

  deleteMealLog: async (mealLogId: string, userId: string) => {
    set({ isLoading: true, error: null });
    try {
      await mealService.deleteMealLog(mealLogId);
      await get().refreshDay(userId, get().selectedDate);
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
