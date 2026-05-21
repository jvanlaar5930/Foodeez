import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { mealPlanService } from '@/services/mealPlanService';
import type { CreateMealPlanRequest, GenerateMealPlanRequest, MealPlan } from '@foodeez/shared';

export const useMealPlanStore = defineStore('mealPlan', () => {
  const plans = ref<MealPlan[]>([]);
  const activePlan = ref<MealPlan | null>(null);
  const isLoading = ref(false);
  const isGenerating = ref(false);
  const error = ref<string | null>(null);

  const sortedPlans = computed(() =>
    [...plans.value].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    ),
  );

  async function fetchPlans(userId: string): Promise<void> {
    isLoading.value = true;
    error.value = null;
    try {
      plans.value = await mealPlanService.getMealPlans(userId);
      if (plans.value.length > 0) {
        activePlan.value = plans.value[0];
      }
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
    } finally {
      isLoading.value = false;
    }
  }

  async function createPlan(data: CreateMealPlanRequest): Promise<MealPlan> {
    isLoading.value = true;
    error.value = null;
    try {
      const plan = await mealPlanService.createMealPlan(data);
      plans.value.unshift(plan);
      activePlan.value = plan;
      return plan;
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  async function generatePlan(data: GenerateMealPlanRequest): Promise<MealPlan> {
    isGenerating.value = true;
    error.value = null;
    try {
      const plan = await mealPlanService.generateAIMealPlan(data);
      plans.value.unshift(plan);
      activePlan.value = plan;
      return plan;
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
      throw err;
    } finally {
      isGenerating.value = false;
    }
  }

  function setActivePlan(plan: MealPlan): void {
    activePlan.value = plan;
  }

  function extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'response' in err) {
      const e = err as { response?: { data?: { detail?: string; title?: string } } };
      return e.response?.data?.detail ?? e.response?.data?.title ?? 'An error occurred';
    }
    return 'An error occurred';
  }

  return {
    plans,
    activePlan,
    sortedPlans,
    isLoading,
    isGenerating,
    error,
    fetchPlans,
    createPlan,
    generatePlan,
    setActivePlan,
  };
});
