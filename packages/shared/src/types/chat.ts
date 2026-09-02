import { MealType } from '../enums';

/** Serialised by the API as the C# member name, so these values must match exactly. */
export enum ChatRole {
  User = 'User',
  Assistant = 'Assistant',
}

/** A meal the assistant offered to put on the calendar. */
export interface PlannedMeal {
  date: string;
  mealType: MealType;
  name: string;
  description?: string;
  servings: number;
}

export interface ChatMessage {
  id: string;
  conversationId: string;
  role: ChatRole;
  content: string;
  /** Empty for most turns; non-empty when the reply proposed meals. */
  suggestions: PlannedMeal[];
  /** Set once the suggestions have been added to the calendar. */
  suggestionsAcceptedAt?: string;
  createdAt: string;
}

/** One thread in the list, without its messages. */
export interface ChatConversation {
  id: string;
  title: string;
  lastMessageAt: string;
  createdAt: string;
  messageCount: number;
  /** The opening of the last reply. */
  preview?: string;
}

/** A thread with everything said in it. */
export interface ChatConversationDetail {
  id: string;
  title: string;
  lastMessageAt: string;
  createdAt: string;
  messages: ChatMessage[];
}

export interface SendChatMessageRequest {
  /** Omitted to start a new thread; the reply carries the id it was given. */
  conversationId?: string;
  message: string;
}

/** What a finished reply carries back - including the thread id, for a brand-new thread. */
export interface ChatReply {
  conversationId: string;
  title: string;
  message: ChatMessage;
}
