<script setup lang="ts">
import { computed } from 'vue';
import { isAiRecipeImage, type Recipe } from '@foodeez/shared';
import AiRecipeThumb from './AiRecipeThumb.vue';

const props = defineProps<{
  recipe: Recipe;
  /** Whether this user has marked it. Omitted where there is nothing to mark it with. */
  isSaved?: boolean;
  /** True while a toggle is in flight, so the button cannot be pressed twice. */
  isSavePending?: boolean;
  /** Draws the mark button at all. Off for a signed-out visitor, who cannot keep anything. */
  canSave?: boolean;
}>();

const emit = defineEmits<{ toggleSave: [] }>();

/** A recipe written in the advice tab carries a marker, not a picture, in its place. */
const isAiThumb = computed(() => isAiRecipeImage(props.recipe.imageUrl));

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
  <!-- The mark button is a sibling of the card rather than a child: the card itself is a
       button, and a button inside a button is invalid and unreachable by keyboard.

       This wrapper is the component's root, so a `@click` the parent puts on <RecipeCard>
       lands here by attribute fallthrough - which is why the mark button below stops the
       event. Without that, marking a favourite also opened the recipe. -->
  <div class="relative">
    <button
      v-if="canSave"
      type="button"
      :disabled="isSavePending"
      :aria-pressed="isSaved"
      :title="isSaved ? `Remove ${recipe.name} from favorites` : `Save ${recipe.name} to favorites`"
      :aria-label="isSaved ? `Remove ${recipe.name} from favorites` : `Save ${recipe.name} to favorites`"
      class="absolute right-2 top-2 z-10 rounded-full bg-white/90 px-2 py-1 text-lg leading-none shadow-sm backdrop-blur transition-transform hover:scale-110 disabled:opacity-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-green-500 dark:bg-gray-900/90"
      @click.stop="emit('toggleSave')"
    >
      <span :class="isSaved ? 'text-red-500' : 'text-gray-400'">{{ isSaved ? '♥' : '♡' }}</span>
    </button>

  <button
    type="button"
    class="group flex w-full flex-col overflow-hidden rounded-2xl bg-white text-left shadow-sm ring-1 ring-gray-100 transition-all hover:-translate-y-0.5 hover:shadow-lg focus:outline-none focus-visible:ring-2 focus-visible:ring-green-500 dark:bg-gray-900 dark:ring-gray-800"
  >
    <div
      class="flex h-40 items-center justify-center overflow-hidden bg-gradient-to-br from-green-100 to-emerald-200 dark:from-green-900/40 dark:to-emerald-900/30"
    >
      <AiRecipeThumb v-if="isAiThumb" compact />
      <img
        v-else-if="recipe.imageUrl"
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
  </div>
</template>
