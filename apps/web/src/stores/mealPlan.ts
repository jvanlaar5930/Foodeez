import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { mealPlanService } from '@/services/mealPlanService';
import type { CreateMealPlanRequest, GenerateMealPlanRequest, MealPlan } from '@foodeez/shared';

export const useMealPlanStore = defineStore('mealPlan', () => {
  const plans = ref<MealPlan[]>([]);
  const activePlan = ref<MealPlan | null>(null);
  const isLoading = ref(false);
  const isGenerating = ref(false);
  /** What the model has written so far this generation, for the view to show as it arrives. */
  const generationText = ref('');
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

  // Held outside the action so cancelGeneration can reach the in-flight request.
  let generateController: AbortController | null = null;

  async function generatePlan(data: GenerateMealPlanRequest): Promise<MealPlan> {
    // A second click should replace the first request, not race it.
    generateController?.abort();
    generateController = new AbortController();
    const controller = generateController;

    isGenerating.value = true;
    generationText.value = '';
    error.value = null;
    try {
      const plan = await mealPlanService.generateAIMealPlanStream(
        data,
        (text) => {
          // Only the current request may write to the shared buffer; an aborted older one
          // would otherwise keep typing over its replacement.
          if (generateController === controller) {
            generationText.value += text;
          }
        },
        controller.signal,
      );
      plans.value.unshift(plan);
      activePlan.value = plan;
      return plan;
    } catch (err: unknown) {
      // A cancel is not a failure and must not leave an error banner behind.
      if (controller.signal.aborted) {
        error.value = null;
      } else {
        error.value = extractErrorMessage(err);
      }
      throw err;
    } finally {
      // Only the request that is still current owns the flag; an aborted older one does not.
      if (generateController === controller) {
        generateController = null;
        isGenerating.value = false;
      }
    }
  }

  /** Stops an in-flight generation. Closing the connection is what stops the model call. */
  function cancelGeneration(): void {
    generateController?.abort();
    generateController = null;
    isGenerating.value = false;
    generationText.value = '';
  }

  function setActivePlan(plan: MealPlan): void {
    activePlan.value = plan;
  }

  function extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'response' in err) {
      // `message` covers the plain-object errors this API returns (the 503 when the AI
      // provider is unavailable); detail/title cover the ProblemDetails responses.
      const e = err as {
        response?: { data?: { message?: string; detail?: string; title?: string } };
      };
      return (
        e.response?.data?.message ??
        e.response?.data?.detail ??
        e.response?.data?.title ??
        'An error occurred'
      );
    }
    return 'An error occurred';
  }

  return {
    plans,
    activePlan,
    cancelGeneration,
    sortedPlans,
    isLoading,
    isGenerating,
    generationText,
    error,
    fetchPlans,
    createPlan,
    generatePlan,
    setActivePlan,
  };
});
