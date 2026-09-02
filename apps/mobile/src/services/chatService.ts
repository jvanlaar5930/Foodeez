import { api } from './api';
import { authToken, streamAI, type StreamHandle } from './aiStream';
import type {
  ChatConversationDetailDto,
  ChatConversationDto,
  ChatReplyDto,
  MealPlanDto,
  RecipeDto,
  SendChatMessageRequest,
} from '@/types';

export async function getConversations(): Promise<ChatConversationDto[]> {
  const response = await api.get<ChatConversationDto[]>('/chat/conversations');
  return Array.isArray(response.data) ? response.data : [];
}

export async function getConversation(conversationId: string): Promise<ChatConversationDetailDto> {
  const response = await api.get<ChatConversationDetailDto>(`/chat/conversations/${conversationId}`);
  return response.data;
}

export async function deleteConversation(conversationId: string): Promise<void> {
  await api.delete(`/chat/conversations/${conversationId}`);
}

/**
 * Ask a question. `onDelta` receives the reply as it is written; the saved message comes back
 * at the end, along with the thread id - which is how a brand-new conversation learns its own.
 * The returned promise carries a `cancel` that stops the model call.
 */
export function sendMessage(
  request: SendChatMessageRequest,
  onDelta: (text: string) => void,
): Promise<ChatReplyDto> & StreamHandle {
  return streamAI<ChatReplyDto>('/chat/messages/stream', { body: request, token: authToken() }, onDelta);
}

/** Put the meals a reply suggested onto the calendar. */
export async function addSuggestionsToPlan(
  messageId: string,
  indexes: number[] = [],
): Promise<MealPlanDto[]> {
  const response = await api.post<MealPlanDto[]>(`/chat/messages/${messageId}/plan`, { indexes });
  return Array.isArray(response.data) ? response.data : [];
}

/**
 * Keep the recipes a reply wrote out. They join the shared recipe library and the caller's
 * own collection, and come back as full recipes.
 */
export async function saveRecipes(messageId: string, indexes: number[] = []): Promise<RecipeDto[]> {
  const response = await api.post<RecipeDto[]>(`/chat/messages/${messageId}/recipes`, { indexes });
  return Array.isArray(response.data) ? response.data : [];
}
