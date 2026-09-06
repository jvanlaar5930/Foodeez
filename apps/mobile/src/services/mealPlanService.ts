import { api } from './api';
import { authToken, streamAI, type StreamHandlers } from './aiStream';
import type {
  GenerateMealPlanRequest,
  MealPlanDayDto,
  MealPlanDto,
  MealPlanEntryDto,
  MealPlanEntryMoveRequest,
  MealPlanEntryMoveResponse,
  MealPlanEntryRequest,
  MealPlanGenerationResultDto,
  MealPlanProgressDto,
} from '@/types';

export async function getMealPlans(userId: string): Promise<MealPlanDto[]> {
  // The controller takes userId from the query string; `/meal-plans/{userId}` matches no
  // route at all and was returning 404 on every load.
  const response = await api.get<MealPlanDto[]>('/meal-plans', { params: { userId } });
  return Array.isArray(response.data) ? response.data : [];
}

export async function getMealPlanById(planId: string): Promise<MealPlanDto> {
  const response = await api.get<MealPlanDto>(`/meal-plans/detail/${planId}`);
  return response.data;
}

export async function createMealPlan(data: {
  userId: string;
  name: string;
  startDate: string;
  endDate: string;
}): Promise<MealPlanDto> {
  const response = await api.post<MealPlanDto>('/meal-plans', data);
  return response.data;
}

/**
 * Generation, streamed a day at a time.
 *
 * This used to POST to the blocking endpoint and wait, which on a self-hosted model could not
 * work at all: one day alone can take longer than the app's whole AI timeout, so a week never
 * had a chance of arriving before the client gave up - and when it did give up, the week was
 * lost rather than merely unfinished. Streaming means each day is saved as it is written, the
 * screen can say which day it is on, and the connection carries bytes throughout instead of
 * sitting idle for minutes waiting for one enormous answer.
 */
export function generateAIMealPlanStream(
  data: GenerateMealPlanRequest,
  onDelta: (text: string) => void,
  handlers: StreamHandlers<MealPlanProgressDto, MealPlanDayDto> = {},
) {
  return streamAI<MealPlanGenerationResultDto, MealPlanProgressDto, MealPlanDayDto>(
    '/meal-plans/generate/stream',
    { body: data, token: authToken() },
    onDelta,
    handlers,
  );
}

export async function deleteMealPlan(planId: string): Promise<void> {
  await api.delete(`/meal-plans/${planId}`);
}

/** Fills one slot. The API replaces whatever was already in it, so this is also a move. */
export async function addEntry(planId: string, entry: MealPlanEntryRequest): Promise<MealPlanEntryDto> {
  const response = await api.post<MealPlanEntryDto>(`/meal-plans/${planId}/entries`, entry);
  return response.data;
}

export async function updateEntry(
  planId: string,
  entryId: string,
  entry: MealPlanEntryRequest,
): Promise<MealPlanEntryDto> {
  const response = await api.put<MealPlanEntryDto>(`/meal-plans/${planId}/entries/${entryId}`, entry);
  return response.data;
}

/**
 * Sends one meal to a different day or slot, swapping with whatever is already there.
 *
 * Not `updateEntry` with the date changed: that endpoint deletes whatever occupies the
 * destination, and a slot picked from a list is as easy to mis-tap as a cell is to mis-drop.
 * `targetPlanId` is what lets the destination be a day this plan does not cover, which on the
 * day screen is every other day.
 */
export async function moveEntry(
  planId: string,
  entryId: string,
  destination: MealPlanEntryMoveRequest,
): Promise<MealPlanEntryMoveResponse> {
  const response = await api.put<MealPlanEntryMoveResponse>(
    `/meal-plans/${planId}/entries/${entryId}/move`,
    destination,
  );
  return response.data;
}

export async function deleteEntry(planId: string, entryId: string): Promise<void> {
  await api.delete(`/meal-plans/${planId}/entries/${entryId}`);
}
