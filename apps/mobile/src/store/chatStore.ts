import { create } from 'zustand';
import * as chatService from '@/services/chatService';
import { AIStreamError } from '@/services/aiStream';
import { describeApiError } from '@/utils/apiError';
import {
  ChatRole,
  type ChatConversationDetailDto,
  type ChatConversationDto,
  type ChatMessageDto,
} from '@/types';

interface ChatState {
  conversations: ChatConversationDto[];
  activeConversation: ChatConversationDetailDto | null;
  isLoading: boolean;
  isSending: boolean;
  /** What the assistant has written so far this turn, for the screen to show as it arrives. */
  streamingText: string;
  error: string | null;

  fetchConversations: () => Promise<void>;
  openConversation: (conversationId: string) => Promise<void>;
  startNew: () => void;
  send: (text: string) => Promise<void>;
  cancelSend: () => void;
  remove: (conversationId: string) => Promise<void>;
  addSuggestionsToPlan: (messageId: string) => Promise<boolean>;
  clearError: () => void;
}

/** Held outside the store so cancel can reach the in-flight request. */
let inFlight: { cancel: () => void } | null = null;

export const useChatStore = create<ChatState>()((set, get) => ({
  conversations: [],
  activeConversation: null,
  isLoading: false,
  isSending: false,
  streamingText: '',
  error: null,

  fetchConversations: async () => {
    set({ isLoading: true, error: null });
    try {
      set({ conversations: await chatService.getConversations(), isLoading: false });
    } catch (err: unknown) {
      set({
        isLoading: false,
        error: describeApiError(err, 'Your conversations could not be loaded.'),
      });
    }
  },

  openConversation: async (conversationId: string) => {
    set({ isLoading: true, error: null, streamingText: '' });
    try {
      const conversation = await chatService.getConversation(conversationId);
      set({ activeConversation: conversation, isLoading: false });
    } catch (err: unknown) {
      set({
        isLoading: false,
        error: describeApiError(err, 'That conversation could not be opened.'),
      });
    }
  },

  /**
   * Clears the thread without touching the server. A conversation is not created until there
   * is something to say in it, so an abandoned "New" leaves nothing behind.
   */
  startNew: () => {
    set({ activeConversation: null, streamingText: '', error: null });
  },

  send: async (text: string) => {
    const question = text.trim();
    if (question.length === 0 || get().isSending) {
      return;
    }

    // The question goes on screen immediately, under a placeholder id. The server saves its
    // own copy before it calls the model, so this is showing what happened, not guessing.
    const asked: ChatMessageDto = {
      id: `pending-${Date.now()}`,
      conversationId: get().activeConversation?.id ?? '',
      role: ChatRole.User,
      content: question,
      suggestions: [],
      createdAt: new Date().toISOString(),
    };

    const existing = get().activeConversation;
    const conversation: ChatConversationDetailDto = existing
      ? { ...existing, messages: [...existing.messages, asked] }
      : {
          id: '',
          title: question.length <= 60 ? question : `${question.slice(0, 57)}...`,
          createdAt: new Date().toISOString(),
          lastMessageAt: new Date().toISOString(),
          messages: [asked],
        };

    set({ isSending: true, error: null, streamingText: '', activeConversation: conversation });

    const request = chatService.sendMessage(
      { conversationId: conversation.id.length > 0 ? conversation.id : undefined, message: question },
      (delta) => set((state) => ({ streamingText: state.streamingText + delta })),
    );
    inFlight = request;

    try {
      const reply = await request;
      const current = get().activeConversation ?? conversation;

      set({
        activeConversation: {
          ...current,
          id: reply.conversationId,
          title: reply.title,
          lastMessageAt: reply.message.createdAt,
          messages: [...current.messages, reply.message],
        },
      });

      // The thread list carries a preview and a count, both of which just changed.
      await get().fetchConversations();
    } catch (err: unknown) {
      // The question stays on screen: the server recorded it, and it is what a retry repeats.
      set({
        error:
          err instanceof AIStreamError
            ? err.message
            : describeApiError(err, 'The reply could not be completed. Please try again.'),
      });
    } finally {
      inFlight = null;
      set({ isSending: false, streamingText: '' });
    }
  },

  /** Aborting closes the connection, which is what actually stops the model call. */
  cancelSend: () => {
    inFlight?.cancel();
    inFlight = null;
  },

  remove: async (conversationId: string) => {
    try {
      await chatService.deleteConversation(conversationId);
      set((state) => ({
        conversations: state.conversations.filter((c) => c.id !== conversationId),
        activeConversation:
          state.activeConversation?.id === conversationId ? null : state.activeConversation,
      }));
    } catch (err: unknown) {
      set({ error: describeApiError(err, 'That conversation could not be deleted.') });
    }
  },

  addSuggestionsToPlan: async (messageId: string) => {
    try {
      await chatService.addSuggestionsToPlan(messageId);

      set((state) => ({
        activeConversation: state.activeConversation
          ? {
              ...state.activeConversation,
              messages: state.activeConversation.messages.map((message) =>
                message.id === messageId
                  ? { ...message, suggestionsAcceptedAt: new Date().toISOString() }
                  : message,
              ),
            }
          : null,
      }));

      return true;
    } catch (err: unknown) {
      set({ error: describeApiError(err, 'Those meals could not be added to your plan.') });
      return false;
    }
  },

  clearError: () => set({ error: null }),
}));
