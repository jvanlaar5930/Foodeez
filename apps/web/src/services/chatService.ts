import type {
  ChatConversation,
  ChatConversationDetail,
  ChatReply,
  MealPlan,
  SendChatMessageRequest,
} from '@foodeez/shared';

import { streamAI } from './aiStream';
import api from './api';

export const chatService = {
  /** Every thread, newest activity first. */
  async getConversations(): Promise<ChatConversation[]> {
    const response = await api.get<ChatConversation[]>('/chat/conversations');
    return response.data;
  },

  async getConversation(conversationId: string): Promise<ChatConversationDetail> {
    const response = await api.get<ChatConversationDetail>(`/chat/conversations/${conversationId}`);
    return response.data;
  },

  async deleteConversation(conversationId: string): Promise<void> {
    await api.delete(`/chat/conversations/${conversationId}`);
  },

  /**
   * Ask a question. `onDelta` receives the reply as it is written; the saved message comes
   * back at the end, along with the thread id - which is how a brand-new conversation finds
   * out what it is called.
   */
  async sendMessage(
    request: SendChatMessageRequest,
    onDelta: (text: string) => void,
    signal?: AbortSignal,
  ): Promise<ChatReply> {
    return streamAI<ChatReply>('/chat/messages/stream', { body: request, signal }, onDelta);
  },

  /**
   * Put the meals a reply suggested onto the calendar. Returns the plans that ended up
   * holding them.
   */
  async addSuggestionsToPlan(messageId: string, indexes: number[] = []): Promise<MealPlan[]> {
    const response = await api.post<MealPlan[]>(`/chat/messages/${messageId}/plan`, { indexes });
    return response.data;
  },
};
