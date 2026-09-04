import { create } from 'zustand';
import { describeApiError } from '@/utils/apiError';
import { runAsync } from '@/store/asyncState';
import * as mealPlanService from '@/services/mealPlanService';
import type { GenerateMealPlanRequest, MealPlanDto, MealPlanEntryRequest } from '@/types';

interface MealPlanState {
  plans: MealPlanDto[];
  activePlan: MealPlanDto | null;
  isLoading: boolean;
  isGenerating: boolean;
  error: string | null;
  fetchPlans: (userId: string) => Promise<void>;
  generatePlan: (data: GenerateMealPlanRequest) => Promise<void>;
  setActivePlan: (plan: MealPlanDto | null) => void;
  clearError: () => void;
  /** The plan covering `startDate`, creating one for the range if there is none. */
  ensurePlanFor: (userId: string, startDate: string, endDate: string, name: string) => Promise<MealPlanDto>;
  /** Fills one slot - `entryId` null adds, otherwise replaces that entry. */
  saveEntry: (
    userId: string,
    planId: string,
    entryId: string | null,
    payload: MealPlanEntryRequest,
  ) => Promise<void>;
  removeEntry: (userId: string, planId: string, entryId: string) => Promise<void>;
}

export const useMealPlanStore = create<MealPlanState>()((set, get) => ({
  plans: [],
  activePlan: null,
  isLoading: false,
  isGenerating: false,
  error: null,

  fetchPlans: async (userId: string) => {
    await runAsync(set, { fallback: 'Your meal plans could not be loaded.' }, async () => {
      const plans = await mealPlanService.getMealPlans(userId);
      set({ plans, activePlan: plans.length > 0 ? plans[0] : null });
    });
  },

  generatePlan: async (data: GenerateMealPlanRequest) => {
    set({ isGenerating: true, error: null });
    try {
      const newPlan = await mealPlanService.generateAIMealPlan(data);
      const currentPlans = get().plans;
      set({ plans: [newPlan, ...currentPlans], activePlan: newPlan, isGenerating: false });
    } catch (err: unknown) {
      set({ isGenerating: false, error: describeApiError(err, 'That plan could not be generated.') });
      throw err;
    }
  },

  setActivePlan: (plan: MealPlanDto | null) => {
    set({ activePlan: plan });
  },

  clearError: () => {
    set({ error: null });
  },

  ensurePlanFor: async (userId: string, startDate: string, endDate: string, name: string) => {
    const existing = get().plans.find((p) => p.startDate <= startDate && p.endDate >= endDate);
    if (existing) return existing;

    const plan = await mealPlanService.createMealPlan({ userId, name, startDate, endDate });
    set({ plans: [plan, ...get().plans], activePlan: plan });
    return plan;
  },

  saveEntry: async (userId: string, planId: string, entryId: string | null, payload: MealPlanEntryRequest) => {
    set({ error: null });
    try {
      if (entryId) {
        await mealPlanService.updateEntry(planId, entryId, payload);
      } else {
        await mealPlanService.addEntry(planId, payload);
      }
      // Entries come back grouped by date from the plans list itself, so refetching is the
      // simplest way to keep that grouping correct rather than re-deriving it here.
      const plans = await mealPlanService.getMealPlans(userId);
      const active = plans.find((p) => p.id === planId) ?? plans[0] ?? null;
      set({ plans, activePlan: active });
    } catch (err: unknown) {
      set({ error: describeApiError(err, 'That meal could not be saved.') });
      throw err;
    }
  },

  removeEntry: async (userId: string, planId: string, entryId: string) => {
    set({ error: null });
    try {
      await mealPlanService.deleteEntry(planId, entryId);
      const plans = await mealPlanService.getMealPlans(userId);
      const active = plans.find((p) => p.id === planId) ?? plans[0] ?? null;
      set({ plans, activePlan: active });
    } catch (err: unknown) {
      set({ error: describeApiError(err, 'That meal could not be removed.') });
      throw err;
    }
  },
}));
