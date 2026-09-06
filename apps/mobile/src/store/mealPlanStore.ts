import { create } from 'zustand';
import { describeApiError } from '@/utils/apiError';
import { runAsync } from '@/store/asyncState';
import * as mealPlanService from '@/services/mealPlanService';
import type {
  GenerateMealPlanRequest,
  MealPlanDayDto,
  MealPlanDto,
  MealPlanEntryRequest,
  MealPlanGenerationResultDto,
  MealPlanProgressDto,
} from '@/types';

/** Replaces a plan already in the list, or adds it if this is the first sight of it. */
function upsertPlan(plans: MealPlanDto[], plan: MealPlanDto): MealPlanDto[] {
  const index = plans.findIndex((p) => p.id === plan.id);
  if (index < 0) return [plan, ...plans];

  const next = [...plans];
  next[index] = plan;
  return next;
}

/**
 * Folds one freshly generated day into the plan being rendered.
 *
 * The plan row exists from the first successful day, but its full shape only arrives with the
 * final result - so the first day to land may belong to a plan not in the list yet, and a stub
 * is created for it rather than the day being dropped on the floor.
 */
function mergeGeneratedDay(plans: MealPlanDto[], day: MealPlanDayDto): MealPlanDto[] {
  const existing = plans.find((p) => p.id === day.planId);

  const merged: MealPlanDto = existing
    ? {
        ...existing,
        entriesByDate: { ...existing.entriesByDate, [day.date]: day.entries },
        // The range grows as days land, so the calendar's plan lookup keeps matching.
        startDate: day.date < existing.startDate ? day.date : existing.startDate,
        endDate: day.date > existing.endDate ? day.date : existing.endDate,
      }
    : {
        id: day.planId,
        userId: '',
        name: 'AI Meal Plan',
        startDate: day.date,
        endDate: day.date,
        isAIGenerated: true,
        entriesByDate: { [day.date]: day.entries },
        createdAt: new Date().toISOString(),
      };

  return upsertPlan(plans, merged);
}

interface MealPlanState {
  plans: MealPlanDto[];
  activePlan: MealPlanDto | null;
  /** Whether the plan list has been fetched this session - see `ensurePlanFor`. */
  hasLoaded: boolean;
  isLoading: boolean;
  isGenerating: boolean;
  /** What the model has written for the day in progress. Cleared as each new day starts. */
  generationText: string;
  /** Which day of the run is being worked on, so a multi-minute wait has something to show. */
  generationProgress: MealPlanProgressDto | null;
  /** How the last run turned out, kept afterwards because a partly written week is normal. */
  lastGeneration: MealPlanGenerationResultDto | null;
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
  hasLoaded: false,
  isLoading: false,
  isGenerating: false,
  generationText: '',
  generationProgress: null,
  lastGeneration: null,
  error: null,

  fetchPlans: async (userId: string) => {
    await runAsync(set, { fallback: 'Your meal plans could not be loaded.' }, async () => {
      const plans = await mealPlanService.getMealPlans(userId);
      set({ plans, activePlan: plans.length > 0 ? plans[0] : null, hasLoaded: true });
    });
  },

  generatePlan: async (data: GenerateMealPlanRequest) => {
    set({
      isGenerating: true,
      error: null,
      generationText: '',
      generationProgress: null,
      lastGeneration: null,
    });

    try {
      const outcome = await mealPlanService.generateAIMealPlanStream(
        data,
        (text) => set((state) => ({ generationText: state.generationText + text })),
        {
          onProgress: (progress) =>
            set({
              generationProgress: progress,
              // Each day narrates itself, so the previous day's text is cleared rather than
              // left to grow into a wall nobody reads on a phone screen.
              ...(progress.status === 'planning' ? { generationText: '' } : {}),
            }),
          onPart: (day) => set((state) => ({ plans: mergeGeneratedDay(state.plans, day) })),
        },
      );

      set((state) => ({
        isGenerating: false,
        generationProgress: null,
        lastGeneration: outcome,
        plans: outcome.plan ? upsertPlan(state.plans, outcome.plan) : state.plans,
        activePlan: outcome.plan ?? state.activePlan,
      }));
    } catch (err: unknown) {
      // Days written before the failure are already saved server-side, so the error is about
      // the rest of the week rather than about losing what was done.
      set({
        isGenerating: false,
        generationProgress: null,
        error: describeApiError(err, 'That plan could not be generated.'),
      });
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
    // The recipe screen can reach this without the meal-plan tab ever having been opened, and
    // a never-fetched list looks exactly like a user with no plans - which would quietly make
    // a second plan for a week that already has one.
    if (!get().hasLoaded) await get().fetchPlans(userId);

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
