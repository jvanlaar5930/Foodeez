import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { extractErrorMessage } from '@/utils/apiError';
import { useAsyncState } from '@/stores/asyncState';
import { mealPlanService } from '@/services/mealPlanService';
import type {
  CreateMealPlanRequest,
  GenerateMealPlanRequest,
  MealPlan,
  MealPlanEntry,
  MealPlanEntryRequest,
} from '@foodeez/shared';

export const useMealPlanStore = defineStore('mealPlan', () => {
  const plans = ref<MealPlan[]>([]);
  const activePlan = ref<MealPlan | null>(null);
  const isGenerating = ref(false);
  /** What the model has written so far this generation, for the view to show as it arrives. */
  const generationText = ref('');
  const { isLoading, error, run, runOrThrow } = useAsyncState();

  const sortedPlans = computed(() =>
    [...plans.value].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    ),
  );

  async function fetchPlans(userId: string): Promise<void> {
    await run('Your meal plans could not be loaded.', async () => {
      plans.value = await mealPlanService.getMealPlans(userId);
      if (plans.value.length > 0) {
        activePlan.value = plans.value[0];
      }
    });
  }

  async function createPlan(data: CreateMealPlanRequest): Promise<MealPlan> {
    return runOrThrow('That plan could not be created.', async () => {
      const plan = await mealPlanService.createMealPlan(data);
      plans.value.unshift(plan);
      activePlan.value = plan;
      return plan;
    });
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

  /**
   * The plan a given day belongs to. The calendar can be scrolled to any week, and only
   * the plan whose range covers that day can hold a meal on it - `activePlan` is just the
   * most recent plan and is often the wrong one once you page back a week.
   */
  function planCovering(dateStr: string): MealPlan | undefined {
    return plans.value.find((p) => p.startDate <= dateStr && p.endDate >= dateStr);
  }

  /**
   * The plan covering `startDate`, creating one for the range if there is none. Adding a
   * meal to a week with no plan should just work rather than making the user go and make
   * a plan first.
   */
  async function ensurePlanFor(
    userId: string,
    startDate: string,
    endDate: string,
    name: string,
  ): Promise<MealPlan> {
    const existing = planCovering(startDate);
    if (existing) return existing;
    return createPlan({ userId, name, startDate, endDate });
  }

  /**
   * Writes a saved entry into the cached plan so the grid updates without a refetch.
   * The server replaces whatever occupied the slot, so this mirrors that: the entry is
   * pulled out of wherever it used to be (an edit can move it to another day) and any
   * other occupant of its new slot is dropped.
   */
  function placeEntry(plan: MealPlan, entry: MealPlanEntry): void {
    for (const [date, entries] of Object.entries(plan.entriesByDate)) {
      const kept = entries.filter(
        (e) => e.id !== entry.id && !(date === entry.entryDate && e.mealType === entry.mealType),
      );
      if (kept.length !== entries.length) plan.entriesByDate[date] = kept;
    }
    plan.entriesByDate[entry.entryDate] = [
      ...(plan.entriesByDate[entry.entryDate] ?? []),
      entry,
    ];
  }

  async function saveEntry(
    planId: string,
    entryId: string | null,
    payload: MealPlanEntryRequest,
  ): Promise<MealPlanEntry> {
    error.value = null;
    try {
      const saved = entryId
        ? await mealPlanService.updateEntry(planId, entryId, payload)
        : await mealPlanService.addEntry(planId, payload);
      const plan = plans.value.find((p) => p.id === planId);
      if (plan) placeEntry(plan, saved);
      return saved;
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
      throw err;
    }
  }

  async function removeEntry(planId: string, entryId: string): Promise<void> {
    error.value = null;
    try {
      await mealPlanService.deleteEntry(planId, entryId);
      const plan = plans.value.find((p) => p.id === planId);
      if (!plan) return;
      for (const [date, entries] of Object.entries(plan.entriesByDate)) {
        const kept = entries.filter((e) => e.id !== entryId);
        if (kept.length !== entries.length) plan.entriesByDate[date] = kept;
      }
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
      throw err;
    }
  }

  function clearError(): void {
    error.value = null;
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
    planCovering,
    ensurePlanFor,
    saveEntry,
    removeEntry,
    clearError,
  };
});
