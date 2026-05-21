import { create } from 'zustand';
import * as mealPlanService from '@/services/mealPlanService';
import type { GenerateMealPlanRequest, MealPlanDto } from '@/types';

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
}

export const useMealPlanStore = create<MealPlanState>()((set, get) => ({
  plans: [],
  activePlan: null,
  isLoading: false,
  isGenerating: false,
  error: null,

  fetchPlans: async (userId: string) => {
    set({ isLoading: true, error: null });
    try {
      const plans = await mealPlanService.getMealPlans(userId);
      const activePlan = plans.length > 0 ? plans[0] : null;
      set({ plans, activePlan, isLoading: false });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to fetch meal plans.';
      set({ isLoading: false, error: message });
    }
  },

  generatePlan: async (data: GenerateMealPlanRequest) => {
    set({ isGenerating: true, error: null });
    try {
      const newPlan = await mealPlanService.generateAIMealPlan(data);
      const currentPlans = get().plans;
      set({ plans: [newPlan, ...currentPlans], activePlan: newPlan, isGenerating: false });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to generate meal plan.';
      set({ isGenerating: false, error: message });
      throw err;
    }
  },

  setActivePlan: (plan: MealPlanDto | null) => {
    set({ activePlan: plan });
  },

  clearError: () => {
    set({ error: null });
  },
}));
