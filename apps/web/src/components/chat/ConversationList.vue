<script setup lang="ts">
import type { ChatConversation } from '@foodeez/shared';

defineProps<{
  conversations: ChatConversation[];
  activeId?: string;
}>();

const emit = defineEmits<{
  open: [conversationId: string];
  remove: [conversationId: string];
  startNew: [];
}>();

function when(iso: string): string {
  const date = new Date(iso);
  const today = new Date();
  const sameDay = date.toDateString() === today.toDateString();

  return sameDay
    ? date.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })
    : date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}
</script>

<template>
  <div class="flex h-full flex-col">
    <button
      type="button"
      class="mb-3 w-full rounded-xl border-2 border-dashed border-green-300 py-2 text-sm font-semibold text-green-700 transition-colors hover:bg-green-50 dark:border-green-800 dark:text-green-400 dark:hover:bg-green-900/30"
      @click="emit('startNew')"
    >
      + New conversation
    </button>

    <p
      v-if="conversations.length === 0"
      class="px-1 text-sm text-gray-400 dark:text-gray-500"
    >
      Nothing yet. Ask a question to start one.
    </p>

    <ul v-else class="flex-1 space-y-1 overflow-y-auto">
      <li v-for="conversation in conversations" :key="conversation.id">
        <div
          :class="[
            'group flex items-start gap-2 rounded-xl px-3 py-2 transition-colors',
            conversation.id === activeId
              ? 'bg-green-50 dark:bg-green-900/30'
              : 'hover:bg-gray-50 dark:hover:bg-gray-800',
          ]"
        >
          <button
            type="button"
            class="min-w-0 flex-1 text-left"
            @click="emit('open', conversation.id)"
          >
            <p
              :class="[
                'truncate text-sm font-medium',
                conversation.id === activeId
                  ? 'text-green-800 dark:text-green-300'
                  : 'text-gray-800 dark:text-gray-100',
              ]"
            >
              {{ conversation.title }}
            </p>
            <p v-if="conversation.preview" class="truncate text-xs text-gray-400 dark:text-gray-500">
              {{ conversation.preview }}
            </p>
          </button>

          <div class="flex shrink-0 flex-col items-end gap-1">
            <span class="text-[11px] text-gray-400">{{ when(conversation.lastMessageAt) }}</span>
            <button
              type="button"
              class="text-xs text-gray-300 opacity-0 transition group-hover:opacity-100 hover:text-red-500 dark:text-gray-600"
              :aria-label="`Delete ${conversation.title}`"
              @click="emit('remove', conversation.id)"
            >
              Delete
            </button>
          </div>
        </div>
      </li>
    </ul>
  </div>
</template>
