<script setup lang="ts">
import { computed, ref } from 'vue';
import type { SuggestedRecipe } from '@foodeez/shared';

const props = defineProps<{
  recipes: SuggestedRecipe[];
  /** Set once these recipes are in the library. */
  savedAt?: string;
  canSave?: boolean;
}>();

const emit = defineEmits<{ save: [] }>();

const isSaved = computed(() => Boolean(props.savedAt));

/** Which recipe's full ingredients and method are open. Collapsed keeps the thread readable. */
const expanded = ref<number | null>(null);

function toggle(index: number) {
  expanded.value = expanded.value === index ? null : index;
}

function totalTime(recipe: SuggestedRecipe): number {
  return recipe.prepTimeMinutes + recipe.cookTimeMinutes;
}

/** "300 g flour", or just the name when the model gave no measurement. */
function ingredientLine(ingredient: SuggestedRecipe['ingredients'][number]): string {
  const amount = [ingredient.quantity > 0 ? ingredient.quantity : '', ingredient.unit]
    .filter(Boolean)
    .join(' ')
    .trim();
  const line = amount.length > 0 ? `${amount} ${ingredient.name}` : ingredient.name;
  return ingredient.notes ? `${line} (${ingredient.notes})` : line;
}

function steps(recipe: SuggestedRecipe): string[] {
  return recipe.instructions
    .split('\n')
    .map((step) => step.trim().replace(/^\d+[.)]\s*/, ''))
    .filter(Boolean);
}
</script>

<template>
  <div class="rounded-2xl border border-amber-200 bg-amber-50 p-3 dark:border-amber-900 dark:bg-amber-950/40">
    <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-amber-700 dark:text-amber-400">
      {{ recipes.length === 1 ? 'Recipe' : 'Recipes' }}
    </p>

    <div class="space-y-2">
      <div
        v-for="(recipe, index) in recipes"
        :key="`${recipe.name}-${index}`"
        class="rounded-xl bg-white/70 p-2.5 dark:bg-gray-900/50"
      >
        <button
          type="button"
          class="flex w-full items-start justify-between gap-2 text-left"
          :aria-expanded="expanded === index"
          @click="toggle(index)"
        >
          <span class="min-w-0">
            <span class="block text-sm font-semibold text-gray-900 dark:text-gray-100">
              {{ recipe.name }}
            </span>
            <span class="mt-0.5 block text-xs text-gray-500 dark:text-gray-400">
              <template v-if="totalTime(recipe) > 0">{{ totalTime(recipe) }} min &middot; </template>
              {{ recipe.servings }} {{ recipe.servings === 1 ? 'serving' : 'servings' }}
              <template v-if="recipe.calories > 0">
                &middot; {{ Math.round(recipe.calories) }} kcal each
              </template>
            </span>
          </span>
          <span class="shrink-0 text-xs font-semibold text-amber-700 dark:text-amber-400">
            {{ expanded === index ? 'Hide' : 'View' }}
          </span>
        </button>

        <div v-if="expanded === index" class="mt-2 space-y-2 border-t border-amber-200 pt-2 dark:border-amber-900">
          <p v-if="recipe.description" class="text-xs text-gray-600 dark:text-gray-400">
            {{ recipe.description }}
          </p>

          <div>
            <p class="text-xs font-semibold text-gray-700 dark:text-gray-300">Ingredients</p>
            <ul class="mt-0.5 space-y-0.5">
              <li
                v-for="(ingredient, i) in recipe.ingredients"
                :key="`${ingredient.name}-${i}`"
                class="text-xs text-gray-600 dark:text-gray-400"
              >
                {{ ingredientLine(ingredient) }}
              </li>
            </ul>
          </div>

          <div v-if="steps(recipe).length > 0">
            <p class="text-xs font-semibold text-gray-700 dark:text-gray-300">Method</p>
            <ol class="mt-0.5 list-decimal space-y-0.5 pl-4">
              <li v-for="(step, i) in steps(recipe)" :key="i" class="text-xs text-gray-600 dark:text-gray-400">
                {{ step }}
              </li>
            </ol>
          </div>
        </div>
      </div>
    </div>

    <p v-if="isSaved" class="mt-3 text-sm font-medium text-amber-700 dark:text-amber-400">
      Saved to your recipes.
    </p>
    <button
      v-else-if="canSave !== false"
      type="button"
      class="mt-3 w-full rounded-xl bg-amber-600 py-2 text-sm font-semibold text-white transition-colors hover:bg-amber-700"
      @click="emit('save')"
    >
      Save {{ recipes.length === 1 ? 'this recipe' : `these ${recipes.length} recipes` }}
    </button>
  </div>
</template>
