<script setup lang="ts">
import { computed } from 'vue';
import { ChatRole, type ChatMessage } from '@foodeez/shared';
import PlannedMealsCard from './PlannedMealsCard.vue';
import SuggestedRecipesCard from './SuggestedRecipesCard.vue';

const props = defineProps<{
  message: ChatMessage;
  /** False while the message exists only on screen, so its actions stay out of the way. */
  isSaved?: boolean;
}>();

const emit = defineEmits<{
  addToPlan: [messageId: string];
  saveRecipes: [messageId: string];
}>();

const isUser = computed(() => props.message.role === ChatRole.User);

const time = computed(() =>
  new Date(props.message.createdAt).toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit',
  }),
);
</script>

<template>
  <div :class="['flex', isUser ? 'justify-end' : 'justify-start']">
    <div :class="['max-w-[85%] sm:max-w-[75%]', isUser ? 'items-end' : 'items-start']">
      <div
        :class="[
          'whitespace-pre-wrap rounded-2xl px-4 py-2.5 text-sm leading-relaxed',
          isUser
            ? 'bg-green-600 text-white'
            : 'bg-white text-gray-800 shadow-sm dark:bg-gray-900 dark:text-gray-100',
        ]"
      >
        {{ message.content }}
      </div>

      <PlannedMealsCard
        v-if="!isUser && message.suggestions.length > 0"
        class="mt-2"
        :suggestions="message.suggestions"
        :accepted-at="message.suggestionsAcceptedAt"
        :can-add="isSaved !== false"
        @add="emit('addToPlan', message.id)"
      />

      <SuggestedRecipesCard
        v-if="!isUser && message.recipes.length > 0"
        class="mt-2"
        :recipes="message.recipes"
        :saved-at="message.recipesSavedAt"
        :can-save="isSaved !== false"
        @save="emit('saveRecipes', message.id)"
      />

      <p :class="['mt-1 text-xs text-gray-400', isUser ? 'text-right' : 'text-left']">
        {{ time }}
      </p>
    </div>
  </div>
</template>
