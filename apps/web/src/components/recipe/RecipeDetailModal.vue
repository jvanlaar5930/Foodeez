<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount, ref } from 'vue';
import { useScrollLock } from '@/composables/useScrollLock';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import AiRecipeThumb from './AiRecipeThumb.vue';
import { formatQuantity, isAiRecipeImage, type Recipe } from '@foodeez/shared';

const props = defineProps<{
  recipe: Recipe;
  /**
   * The search endpoint returns no ingredients or steps, so the modal opens on partial data
   * and those two sections fill in once the detail request lands.
   */
  loading?: boolean;
}>();
const emit = defineEmits<{ close: []; retry: [] }>();

/** A recipe written in the advice tab carries a marker, not a picture, in its place. */
const isAiThumb = computed(() => isAiRecipeImage(props.recipe.imageUrl));

const steps = computed(() =>
  props.recipe.instructions
    .split('\n')
    .map((s) => s.trim().replace(/^\d+\.\s*/, ''))
    .filter(Boolean)
);

const macros = computed(() => {
  const n = props.recipe.nutritionalInfoPerServing;
  return [
    { label: 'Calories', value: Math.round(n.calories), unit: 'kcal', color: 'text-green-600 dark:text-green-400' },
    { label: 'Protein', value: Math.round(n.protein), unit: 'g', color: 'text-blue-600 dark:text-blue-400' },
    { label: 'Carbs', value: Math.round(n.carbohydrates), unit: 'g', color: 'text-orange-500 dark:text-orange-400' },
    { label: 'Fat', value: Math.round(n.fat), unit: 'g', color: 'text-yellow-600 dark:text-yellow-400' },
    { label: 'Fiber', value: Math.round(n.fiber), unit: 'g', color: 'text-teal-600 dark:text-teal-400' },
    { label: 'Sugar', value: Math.round(n.sugar), unit: 'g', color: 'text-pink-500 dark:text-pink-400' },
  ];
});

function hideImage(e: Event) {
  (e.target as HTMLImageElement).style.display = 'none';
}

/**
 * The print stylesheet hides everything but this dialog, which teleports to <body> for the
 * purpose, so this needs no separate window or re-render - the browser's own dialog is the
 * preview.
 */
function printRecipe() {
  window.print();
}

function onEscape(e: KeyboardEvent) {
  if (e.key === 'Escape') emit('close');
}

// Rendered behind a v-if, so being mounted is what "open" means here.
useScrollLock(ref(true));

onMounted(() => document.addEventListener('keydown', onEscape));
onBeforeUnmount(() => document.removeEventListener('keydown', onEscape));
</script>

<template>
  <Teleport to="body">
    <div
      class="print-root fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-0 backdrop-blur-sm sm:items-center sm:p-4 print:bg-transparent print:backdrop-blur-none"
      role="dialog"
      aria-modal="true"
      @click.self="emit('close')"
    >
      <div
        class="print-area max-h-[92vh] w-full max-w-2xl overflow-y-auto rounded-t-2xl bg-white sm:rounded-2xl dark:bg-gray-900"
      >
        <div
          class="sticky top-0 z-10 flex items-center justify-between border-b border-gray-100 bg-white p-5 dark:border-gray-800 dark:bg-gray-900"
        >
          <h2 class="pr-4 text-lg font-bold text-gray-900 dark:text-gray-100">{{ recipe.name }}</h2>
          <button
            type="button"
            aria-label="Close"
            class="print-hide shrink-0 rounded-lg p-1 text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-600 dark:hover:bg-gray-800 dark:hover:text-gray-300"
            @click="emit('close')"
          >
            ✕
          </button>
        </div>

        <div v-if="isAiThumb" class="h-56 overflow-hidden">
          <AiRecipeThumb />
        </div>
        <div v-else-if="recipe.imageUrl" class="h-56 overflow-hidden">
          <img
            :src="recipe.imageUrl"
            :alt="recipe.name"
            class="h-full w-full object-cover"
            @error="hideImage"
          />
        </div>

        <div class="space-y-6 p-5">
          <p v-if="recipe.description" class="text-sm leading-relaxed text-gray-600 dark:text-gray-400">
            {{ recipe.description }}
          </p>

          <div class="grid grid-cols-4 gap-3 text-center">
            <div
              v-for="stat in [
                { value: `${recipe.prepTimeMinutes}m`, label: 'Prep' },
                { value: `${recipe.cookTimeMinutes}m`, label: 'Cook' },
                { value: recipe.servings, label: 'Servings' },
                { value: Math.round(recipe.nutritionalInfoPerServing.calories), label: 'kcal/srv' },
              ]"
              :key="stat.label"
              class="rounded-xl bg-gray-50 p-3 dark:bg-gray-800"
            >
              <p class="font-bold text-gray-900 dark:text-gray-100">{{ stat.value }}</p>
              <p class="text-xs text-gray-500 dark:text-gray-400">{{ stat.label }}</p>
            </div>
          </div>

          <div>
            <h3 class="mb-3 font-bold text-gray-900 dark:text-gray-100">Nutrition per serving</h3>
            <div class="grid grid-cols-3 gap-2 text-center">
              <div
                v-for="macro in macros"
                :key="macro.label"
                class="rounded-xl bg-gray-50 p-3 dark:bg-gray-800"
              >
                <p class="text-lg font-bold" :class="macro.color">
                  {{ macro.value
                  }}<span class="ml-0.5 text-xs font-normal text-gray-500 dark:text-gray-400">{{
                    macro.unit
                  }}</span>
                </p>
                <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">{{ macro.label }}</p>
              </div>
            </div>
          </div>

          <div>
            <h3 class="mb-3 font-bold text-gray-900 dark:text-gray-100">Ingredients</h3>

            <ul v-if="recipe.ingredients.length" class="space-y-2">
              <li
                v-for="(ing, i) in recipe.ingredients"
                :key="ing.id ?? `${ing.foodItemName}-${i}`"
                class="flex items-start gap-2 text-sm text-gray-700 dark:text-gray-300"
              >
                <span class="mt-0.5 text-green-500">•</span>
                <span>
                  <strong v-if="ing.quantity">{{ formatQuantity(ing.quantity) }} {{ ing.unit }}</strong>
                  {{ ing.foodItemName }}
                </span>
              </li>
            </ul>

            <div v-else-if="loading" class="space-y-2">
              <div
                v-for="n in 5"
                :key="n"
                class="h-4 animate-pulse rounded bg-gray-100 dark:bg-gray-800"
                :style="{ width: `${90 - n * 8}%` }"
              />
            </div>

            <p v-else class="text-sm text-gray-500 dark:text-gray-400">
              No ingredient list is available for this recipe.
            </p>
          </div>

          <div>
            <div class="mb-3 flex items-center justify-between gap-4">
              <h3 class="font-bold text-gray-900 dark:text-gray-100">Instructions</h3>

              <button
                type="button"
                class="print-hide inline-flex shrink-0 items-center gap-1.5 rounded-lg border border-gray-200 px-2.5 py-1.5 text-xs font-semibold text-gray-600 transition-colors hover:border-green-400 hover:text-green-700 dark:border-gray-700 dark:text-gray-300 dark:hover:border-green-600 dark:hover:text-green-400"
                @click="printRecipe"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"
                  />
                </svg>
                Print this recipe
              </button>
            </div>

            <ol v-if="steps.length" class="space-y-3">
              <li v-for="(step, i) in steps" :key="i" class="flex gap-3 text-sm">
                <span
                  class="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-green-600 text-xs font-bold text-white"
                  >{{ i + 1 }}</span
                >
                <span class="leading-relaxed text-gray-700 dark:text-gray-300">{{ step }}</span>
              </li>
            </ol>

            <div v-else-if="loading" class="flex items-center gap-3 py-2">
              <LoadingSpinner size="sm" />
              <span class="text-sm text-gray-500 dark:text-gray-400">Fetching the full recipe…</span>
            </div>

            <!--
              Some sources publish ingredients and nutrition but no method. Sending the reader
              to the original is the only useful answer there.
            -->
            <div
              v-else-if="recipe.sourceUrl"
              class="rounded-xl border border-gray-100 bg-gray-50 p-4 dark:border-gray-800 dark:bg-gray-800"
            >
              <p class="text-sm text-gray-600 dark:text-gray-300">
                The method for this recipe is published by
                <strong>{{ recipe.sourceName || 'the original site' }}</strong> rather than here.
              </p>
              <a
                :href="recipe.sourceUrl"
                target="_blank"
                rel="noopener noreferrer"
                class="mt-2 inline-flex items-center gap-1.5 text-sm font-semibold text-green-700 hover:underline dark:text-green-400"
              >
                View the full recipe
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"
                  />
                </svg>
              </a>
            </div>

            <!--
              The recipe service could not be reached for the method this time. Saying "no
              method exists" here would be wrong - it is a temporary failure, so offer a retry.
            -->
            <div
              v-else-if="recipe.detailUnavailable"
              class="rounded-xl border border-amber-200 bg-amber-50 p-4 dark:border-amber-900/40 dark:bg-amber-950/30"
            >
              <p class="text-sm text-amber-800 dark:text-amber-200">
                We couldn't load the steps for this recipe just now. This is usually temporary.
              </p>
              <button
                type="button"
                class="print-hide mt-2 text-sm font-semibold text-amber-900 hover:underline dark:text-amber-100"
                @click="emit('retry')"
              >
                Try again
              </button>
            </div>

            <p v-else class="text-sm text-gray-500 dark:text-gray-400">
              No method is available for this recipe.
            </p>
          </div>

          <!-- Attribution, shown whenever we know where the recipe came from -->
          <p
            v-if="recipe.sourceUrl && steps.length"
            class="border-t border-gray-100 pt-4 text-xs text-gray-400 dark:border-gray-800 dark:text-gray-500"
          >
            Recipe from
            <a
              :href="recipe.sourceUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="font-medium text-gray-500 hover:underline dark:text-gray-400"
              >{{ recipe.sourceName || 'the original source' }}</a
            >
          </p>

          <div class="print-hide">
            <slot name="actions" />
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
