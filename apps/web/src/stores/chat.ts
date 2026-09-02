import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { ChatRole, type ChatConversation, type ChatConversationDetail, type ChatMessage } from '@foodeez/shared';
import { chatService } from '@/services/chatService';
import { extractErrorMessage } from '@/utils/apiError';

export const useChatStore = defineStore('chat', () => {
  const conversations = ref<ChatConversation[]>([]);
  const activeConversation = ref<ChatConversationDetail | null>(null);
  const isLoading = ref(false);
  const isSending = ref(false);
  /** What the assistant has written so far this turn, for the view to show as it arrives. */
  const streamingText = ref('');
  const error = ref<string | null>(null);

  const messages = computed<ChatMessage[]>(() => activeConversation.value?.messages ?? []);

  // Held outside the action so cancel can reach the in-flight request.
  let sendController: AbortController | null = null;

  async function fetchConversations(): Promise<void> {
    isLoading.value = true;
    error.value = null;
    try {
      conversations.value = await chatService.getConversations();
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, 'Your conversations could not be loaded.');
    } finally {
      isLoading.value = false;
    }
  }

  async function openConversation(conversationId: string): Promise<void> {
    isLoading.value = true;
    error.value = null;
    streamingText.value = '';
    try {
      activeConversation.value = await chatService.getConversation(conversationId);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, 'That conversation could not be opened.');
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Clears the thread without touching the server. A new conversation is not created until
   * there is something to say in it, so an abandoned "New chat" leaves nothing behind.
   */
  function startNew(): void {
    activeConversation.value = null;
    streamingText.value = '';
    error.value = null;
  }

  async function send(text: string): Promise<void> {
    const question = text.trim();
    if (question.length === 0 || isSending.value) {
      return;
    }

    isSending.value = true;
    error.value = null;
    streamingText.value = '';

    // The question goes on screen immediately, under a placeholder id. The server saves its
    // own copy before it calls the model, so this is showing what happened, not guessing.
    const asked = localMessage(ChatRole.User, question);
    if (activeConversation.value) {
      activeConversation.value.messages.push(asked);
    } else {
      activeConversation.value = {
        id: '',
        title: question.length <= 60 ? question : `${question.slice(0, 57)}...`,
        createdAt: new Date().toISOString(),
        lastMessageAt: new Date().toISOString(),
        messages: [asked],
      };
    }

    sendController = new AbortController();

    try {
      const reply = await chatService.sendMessage(
        {
          conversationId: activeConversation.value.id.length > 0 ? activeConversation.value.id : undefined,
          message: question,
        },
        (delta) => {
          streamingText.value += delta;
        },
        sendController.signal,
      );

      activeConversation.value.id = reply.conversationId;
      activeConversation.value.title = reply.title;
      activeConversation.value.lastMessageAt = reply.message.createdAt;
      activeConversation.value.messages.push(reply.message);

      // The thread list carries a preview and a count, both of which just changed.
      await fetchConversations();
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, 'The reply could not be completed. Please try again.');

      // The question stays on screen: the server recorded it, and it is what a retry repeats.
    } finally {
      streamingText.value = '';
      isSending.value = false;
      sendController = null;
    }
  }

  /** Aborting closes the connection, which is what actually stops the model call. */
  function cancelSend(): void {
    sendController?.abort();
    sendController = null;
  }

  async function remove(conversationId: string): Promise<void> {
    error.value = null;
    try {
      await chatService.deleteConversation(conversationId);
      conversations.value = conversations.value.filter((c) => c.id !== conversationId);
      if (activeConversation.value?.id === conversationId) {
        startNew();
      }
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, 'That conversation could not be deleted.');
    }
  }

  /**
   * Puts a reply's suggested meals on the calendar. Returns true when they landed, so the
   * view can say so rather than guessing.
   */
  async function addSuggestionsToPlan(messageId: string): Promise<boolean> {
    error.value = null;
    try {
      await chatService.addSuggestionsToPlan(messageId);

      const message = activeConversation.value?.messages.find((m) => m.id === messageId);
      if (message) {
        message.suggestionsAcceptedAt = new Date().toISOString();
      }

      return true;
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, 'Those meals could not be added to your plan.');
      return false;
    }
  }

  function clearError(): void {
    error.value = null;
  }

  /** A message that exists only on screen, until the server's copy arrives. */
  function localMessage(role: ChatRole, content: string): ChatMessage {
    return {
      id: `pending-${Date.now()}`,
      conversationId: activeConversation.value?.id ?? '',
      role,
      content,
      suggestions: [],
      createdAt: new Date().toISOString(),
    };
  }

  return {
    conversations,
    activeConversation,
    messages,
    isLoading,
    isSending,
    streamingText,
    error,
    fetchConversations,
    openConversation,
    startNew,
    send,
    cancelSend,
    remove,
    addSuggestionsToPlan,
    clearError,
  };
});
