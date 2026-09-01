<script setup lang="ts">
import { computed } from 'vue';
import type { Recipe } from '@foodeez/shared';

const props = defineProps<{ recipe: Recipe }>();

const totalTime = computed(
  () => props.recipe.prepTimeMinutes + props.recipe.cookTimeMinutes
);

const tags = computed(() =>
  (props.recipe.tags ?? '')
    .split(',')
    .map((t) => t.trim())
    .filter(Boolean)
    .slice(0, 3)
);

function hideImage(e: Event) {
  (e.target as HTMLImageElement).style.display = 'none';
}
</script>

<template>
  <button
    type="button"
    class="group flex w-full flex-col overflow-hidden rounded-2xl bg-white text-left shadow-sm ring-1 ring-gray-100 transition-all hover:-translate-y-0.5 hover:shadow-lg focus:outline-none focus-visible:ring-2 focus-visible:ring-green-500 dark:bg-gray-900 dark:ring-gray-800"
  >
    <div
      class="flex h-40 items-center justify-center overflow-hidden bg-gradient-to-br from-green-100 to-emerald-200 dark:from-green-900/40 dark:to-emerald-900/30"
    >
      <img
        v-if="recipe.imageUrl"
        :src="recipe.imageUrl"
        :alt="recipe.name"
        loading="lazy"
        class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
        @error="hideImage"
      />
      <span v-else class="text-4xl">🍽️</span>
    </div>

    <div class="flex flex-1 flex-col p-4">
      <div class="mb-2 flex items-start justify-between gap-2">
        <p class="font-bold leading-snug text-gray-900 dark:text-gray-100">{{ recipe.name }}</p>
        <span
          v-if="recipe.isAIGenerated"
          class="shrink-0 rounded-full bg-orange-100 px-2 py-0.5 text-xs font-semibold text-orange-600 dark:bg-orange-900/40 dark:text-orange-300"
          >✨ AI</span
        >
      </div>

      <p
        v-if="recipe.description"
        class="mb-3 line-clamp-2 text-sm text-gray-500 dark:text-gray-400"
      >
        {{ recipe.description }}
      </p>

      <div class="mt-auto flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
        <span v-if="totalTime > 0">⏱ {{ totalTime }}m</span>
        <span>🔥 {{ Math.round(recipe.nutritionalInfoPerServing.calories) }} kcal</span>
        <span>👥 {{ recipe.servings }} srv</span>
      </div>

      <div v-if="tags.length" class="mt-3 flex flex-wrap gap-1">
        <span
          v-for="tag in tags"
          :key="tag"
          class="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700 dark:bg-green-900/40 dark:text-green-300"
        >
          {{ tag }}
        </span>
      </div>
    </div>
  </button>
</template>
