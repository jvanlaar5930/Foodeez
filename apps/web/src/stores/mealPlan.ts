import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { extractErrorMessage } from '@/utils/apiError';
import { useAsyncState } from '@/stores/asyncState';
import { mealPlanService } from '@/services/mealPlanService';
import type {
  CreateMealPlanRequest,
  GenerateMealPlanRequest,
  MealPlan,
  MealPlanDay,
  MealPlanEntry,
  MealPlanEntryMoveRequest,
  MealPlanEntryRequest,
  MealPlanGenerationResult,
  MealPlanProgress,
} from '@foodeez/shared';

export const useMealPlanStore = defineStore('mealPlan', () => {
  const plans = ref<MealPlan[]>([]);
  const activePlan = ref<MealPlan | null>(null);
  /**
   * Whether the plan list has been fetched this session. Screens other than the calendar
   * can now write into a plan, and `ensurePlanFor` would create a second plan for a week
   * that already has one if it decided from an empty, never-fetched list.
   */
  const hasLoaded = ref(false);
  const isGenerating = ref(false);
  /** What the model has written so far this generation, for the view to show as it arrives. */
  const generationText = ref('');
  /**
   * Where the generation has got to. A week is written a day at a time, and each day against a
   * slow local model is a wait of its own - without this the screen would sit on one spinner
   * for several minutes with nothing to say whether it was working or hung.
   */
  const generationProgress = ref<MealPlanProgress | null>(null);
  /**
   * How the last generation turned out, kept after it finishes because a partly written week
   * is now a normal outcome and the dates that failed are worth showing.
   */
  const lastGeneration = ref<MealPlanGenerationResult | null>(null);
  const { isLoading, error, run, runOrThrow } = useAsyncState();

  const sortedPlans = computed(() =>
    [...plans.value].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    ),
  );

  async function fetchPlans(userId: string): Promise<void> {
    await run('Your meal plans could not be loaded.', async () => {
      plans.value = await mealPlanService.getMealPlans(userId);
      hasLoaded.value = true;
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

  async function generatePlan(data: GenerateMealPlanRequest): Promise<MealPlanGenerationResult> {
    // A second click should replace the first request, not race it.
    generateController?.abort();
    generateController = new AbortController();
    const controller = generateController;

    /** Only the current request may write to shared state; an aborted older one may not. */
    const isCurrent = () => generateController === controller;

    isGenerating.value = true;
    generationText.value = '';
    generationProgress.value = null;
    lastGeneration.value = null;
    error.value = null;
    try {
      const outcome = await mealPlanService.generateAIMealPlanStream(
        data,
        (text) => {
          if (isCurrent()) {
            generationText.value += text;
          }
        },
        {
          onProgress: (progress) => {
            if (isCurrent()) {
              generationProgress.value = progress;
              // Each day starts its own narration, so the previous day's is cleared rather
              // than left to accumulate into a wall of text nobody reads.
              if (progress.status === 'planning') {
                generationText.value = '';
              }
            }
          },
          onPart: (day) => {
            // The day is already saved server-side. Merging it as it lands is what makes the
            // calendar fill in while the rest of the week is still being written.
            if (isCurrent()) {
              mergeGeneratedDay(day);
            }
          },
        },
        controller.signal,
      );

      lastGeneration.value = outcome;

      if (outcome.plan) {
        upsertPlan(outcome.plan);
        activePlan.value = outcome.plan;
      }

      return outcome;
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

  /**
   * Stops an in-flight generation. Closing the connection is what stops the model call.
   *
   * Days already written are left exactly where they are: they were saved as they were
   * generated, so cancelling half way through a week keeps the half that was done.
   */
  function cancelGeneration(): void {
    generateController?.abort();
    generateController = null;
    isGenerating.value = false;
    generationText.value = '';
    generationProgress.value = null;
  }

  /** Replaces a plan already in the list, or adds it if this is the first sight of it. */
  function upsertPlan(plan: MealPlan): void {
    const index = plans.value.findIndex((p) => p.id === plan.id);

    if (index >= 0) {
      plans.value[index] = plan;
    } else {
      plans.value.unshift(plan);
    }
  }

  /**
   * Folds one freshly generated day into the plan the calendar is rendering.
   *
   * The plan row exists from the first successful day, but the client only learns its shape
   * from the final result - so the first day to arrive may be for a plan not in the list yet,
   * and a stub is created for it rather than the day being dropped.
   */
  function mergeGeneratedDay(day: MealPlanDay): void {
    let plan = plans.value.find((p) => p.id === day.planId);

    if (!plan) {
      plan = {
        id: day.planId,
        userId: '',
        name: 'AI Meal Plan',
        startDate: day.date,
        endDate: day.date,
        isAIGenerated: true,
        entriesByDate: {},
        createdAt: new Date().toISOString(),
      };
      plans.value.unshift(plan);
      activePlan.value = plan;
    }

    plan.entriesByDate = { ...plan.entriesByDate, [day.date]: day.entries };

    // The range grows as days land, so the calendar's `planCovering` lookup keeps matching.
    if (day.date < plan.startDate) plan.startDate = day.date;
    if (day.date > plan.endDate) plan.endDate = day.date;
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
    // A never-fetched list looks the same as a user with no plans, and guessing wrong here
    // makes a duplicate plan for the week.
    if (!hasLoaded.value) await fetchPlans(userId);

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

  /**
   * Puts one or two moved entries where the server says they now are.
   *
   * Not `placeEntry` twice: that drops any other occupant of the slot it is writing into, so
   * placing the dragged meal would delete the very entry the swap is about to put back.
   * Both are pulled out first, then both are re-added.
   */
  function placeMoved(plan: MealPlan, moved: MealPlanEntry, swapped?: MealPlanEntry): void {
    const ids = new Set([moved.id, swapped?.id].filter((id): id is string => !!id));

    for (const [date, entries] of Object.entries(plan.entriesByDate)) {
      const kept = entries.filter((e) => !ids.has(e.id));
      if (kept.length !== entries.length) plan.entriesByDate[date] = kept;
    }

    for (const entry of [moved, swapped].filter((e): e is MealPlanEntry => !!e)) {
      plan.entriesByDate[entry.entryDate] = [
        ...(plan.entriesByDate[entry.entryDate] ?? []),
        entry,
      ];
    }
  }

  /**
   * Drags one meal to another day or slot, swapping with whatever is already there.
   *
   * The grid moves on the drop rather than on the response. The move is a single PUT, but that
   * PUT can be waiting on a server that has spun down, and a cell that stays where it was for
   * several seconds reads as the drag not having worked - which invites a second drag, and a
   * second move. The snapshot is what puts it back when the request turns out to have failed.
   */
  async function moveEntry(
    planId: string,
    entryId: string,
    destination: MealPlanEntryMoveRequest,
  ): Promise<void> {
    error.value = null;

    const plan = plans.value.find((p) => p.id === planId);
    if (!plan) return;

    const snapshot = Object.fromEntries(
      Object.entries(plan.entriesByDate).map(([date, entries]) => [date, [...entries]]),
    );

    const moving = Object.values(plan.entriesByDate)
      .flat()
      .find((e) => e.id === entryId);

    if (moving) {
      const occupant = (plan.entriesByDate[destination.entryDate] ?? []).find(
        (e) => e.mealType === destination.mealType && e.id !== entryId,
      );

      placeMoved(
        plan,
        { ...moving, entryDate: destination.entryDate, mealType: destination.mealType },
        occupant
          ? { ...occupant, entryDate: moving.entryDate, mealType: moving.mealType }
          : undefined,
      );
    }

    try {
      const result = await mealPlanService.moveEntry(planId, entryId, destination);
      // Reconciled against what actually happened rather than left on the guess above - the
      // server decides, and the two disagree if anything changed the plan in between.
      placeMoved(plan, result.entry, result.swapped);
    } catch (err: unknown) {
      plan.entriesByDate = snapshot;
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
    hasLoaded,
    cancelGeneration,
    sortedPlans,
    isLoading,
    isGenerating,
    generationText,
    generationProgress,
    lastGeneration,
    error,
    fetchPlans,
    createPlan,
    generatePlan,
    setActivePlan,
    planCovering,
    ensurePlanFor,
    saveEntry,
    moveEntry,
    removeEntry,
    clearError,
  };
});
