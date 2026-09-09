<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount, ref } from 'vue';
import { useScrollLock } from '@/composables/useScrollLock';
import { useRecipeEnhancement } from '@/composables/useRecipeEnhancement';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import AppAlert from '@/components/ui/AppAlert.vue';
import AiRecipeThumb from './AiRecipeThumb.vue';
import { formatQuantity, isAiRecipeImage, type Recipe } from '@foodeez/shared';

const props = defineProps<{
  recipe: Recipe;
  /**
   * The search endpoint returns no ingredients or steps, so the modal opens on partial data
   * and those two sections fill in once the detail request lands.
   */
  loading?: boolean;
  /**
   * Draws the chef's-take controls. Off for a signed-out visitor, who has nowhere to keep an
   * enhanced version, and off where the panel is only being used to look something up.
   */
  canEnhance?: boolean;
}>();
const emit = defineEmits<{ close: []; retry: [] }>();

/**
 * The enhanced version lives here rather than in each view that opens this panel: it belongs
 * to the recipe being read, and both views would otherwise carry their own copy of the same
 * state. The actions slot is handed whichever version is on screen, so "Add to Meal Plan"
 * plans the one the reader is actually looking at.
 */
const {
  active,
  enhanced,
  showingEnhanced,
  isEnhancing,
  error: enhanceError,
  enhance,
  refresh,
  show,
} = useRecipeEnhancement(
  () => props.recipe,
  () => props.canEnhance === true,
);

/** A recipe written in the advice tab carries a marker, not a picture, in its place. */
const isAiThumb = computed(() => isAiRecipeImage(active.value.imageUrl));

const steps = computed(() =>
  active.value.instructions
    .split('\n')
    .map((s) => s.trim().replace(/^\d+\.\s*/, ''))
    .filter(Boolean)
);

/** The chef's reasons, one per line, kept with the enhanced version rather than shown once. */
const chefNotes = computed(() =>
  (active.value.enhancementNotes ?? '')
    .split('\n')
    .map((note) => note.trim().replace(/^[-•]\s*/, ''))
    .filter(Boolean)
);

/** The placeholders only ever stand in for the original; an enhanced version is complete. */
const isFillingIn = computed(() => props.loading === true && !showingEnhanced.value);

const macros = computed(() => {
  const n = active.value.nutritionalInfoPerServing;
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
 * preview. Whichever version is on screen is the one that prints.
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
          <div class="flex min-w-0 items-center gap-2 pr-4">
            <h2 class="truncate text-lg font-bold text-gray-900 dark:text-gray-100">{{ active.name }}</h2>
            <!--
              The recipe's own flag rather than the toggle above it, so an enhanced version
              opened directly - from a calendar slot, say - is badged too, not only one
              reached by switching versions here.
            -->
            <span
              v-if="active.isEnhanced"
              title="A chef's take on this recipe, written by the AI"
              class="shrink-0 rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-700 dark:bg-amber-900/40 dark:text-amber-300"
              >⭐ Enhanced</span
            >
          </div>
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
        <div v-else-if="active.imageUrl" class="h-56 overflow-hidden">
          <img
            :src="active.imageUrl"
            :alt="active.name"
            class="h-full w-full object-cover"
            @error="hideImage"
          />
        </div>

        <div class="space-y-6 p-5">
          <!--
            Which of the two versions is being read. Drawn only once an enhanced one exists,
            because until then there is nothing to switch between and the button below says
            so far more clearly than an empty toggle would.
          -->
          <div
            v-if="enhanced"
            class="print-hide grid grid-cols-2 gap-1 rounded-xl bg-gray-100 p-1 dark:bg-gray-800"
            role="group"
            aria-label="Recipe version"
          >
            <button
              v-for="option in [
                { label: 'Original', enhanced: false },
                { label: '⭐ Enhanced', enhanced: true },
              ]"
              :key="option.label"
              type="button"
              :aria-pressed="showingEnhanced === option.enhanced"
              class="rounded-lg px-3 py-2 text-sm font-semibold transition-colors"
              :class="
                showingEnhanced === option.enhanced
                  ? 'bg-white text-gray-900 shadow-sm dark:bg-gray-900 dark:text-gray-100'
                  : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'
              "
              @click="show(option.enhanced)"
            >
              {{ option.label }}
            </button>
          </div>

          <p v-if="active.description" class="text-sm leading-relaxed text-gray-600 dark:text-gray-400">
            {{ active.description }}
          </p>

          <div class="grid grid-cols-4 gap-3 text-center">
            <div
              v-for="stat in [
                { value: `${active.prepTimeMinutes}m`, label: 'Prep' },
                { value: `${active.cookTimeMinutes}m`, label: 'Cook' },
                { value: active.servings, label: 'Servings' },
                { value: Math.round(active.nutritionalInfoPerServing.calories), label: 'kcal/srv' },
              ]"
              :key="stat.label"
              class="rounded-xl bg-gray-50 p-3 dark:bg-gray-800"
            >
              <p class="font-bold text-gray-900 dark:text-gray-100">{{ stat.value }}</p>
              <p class="text-xs text-gray-500 dark:text-gray-400">{{ stat.label }}</p>
            </div>
          </div>

          <!--
            The advice itself, which is the point of an enhanced version: what the chef
            changed and why. Saved with the recipe, so it is here every time it is opened
            rather than only in the moment it was written.
          -->
          <div
            v-if="chefNotes.length"
            class="rounded-xl border border-amber-200 bg-amber-50 p-4 dark:border-amber-900/40 dark:bg-amber-950/30"
          >
            <h3 class="mb-2 font-bold text-amber-900 dark:text-amber-200">Chef's notes</h3>
            <ul class="space-y-1.5">
              <li
                v-for="(note, i) in chefNotes"
                :key="i"
                class="flex items-start gap-2 text-sm text-amber-800 dark:text-amber-100"
              >
                <span class="mt-0.5">•</span>
                <span class="leading-relaxed">{{ note }}</span>
              </li>
            </ul>
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

            <ul v-if="active.ingredients.length" class="space-y-2">
              <li
                v-for="(ing, i) in active.ingredients"
                :key="ing.id ?? `${ing.foodItemName}-${i}`"
                class="flex items-start gap-2 text-sm text-gray-700 dark:text-gray-300"
              >
                <span class="mt-0.5 text-green-500">•</span>
                <span>
                  <strong v-if="ing.quantity">{{ formatQuantity(ing.quantity) }} {{ ing.unit }}</strong>
                  {{ ing.foodItemName }}
                  <span v-if="ing.notes" class="text-gray-500 dark:text-gray-400">, {{ ing.notes }}</span>
                </span>
              </li>
            </ul>

            <div v-else-if="isFillingIn" class="space-y-2">
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

            <div v-else-if="isFillingIn" class="flex items-center gap-3 py-2">
              <LoadingSpinner size="sm" />
              <span class="text-sm text-gray-500 dark:text-gray-400">Fetching the full recipe…</span>
            </div>

            <!--
              Some sources publish ingredients and nutrition but no method. Sending the reader
              to the original is the only useful answer there.
            -->
            <div
              v-else-if="active.sourceUrl"
              class="rounded-xl border border-gray-100 bg-gray-50 p-4 dark:border-gray-800 dark:bg-gray-800"
            >
              <p class="text-sm text-gray-600 dark:text-gray-300">
                The method for this recipe is published by
                <strong>{{ active.sourceName || 'the original site' }}</strong> rather than here.
              </p>
              <a
                :href="active.sourceUrl"
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
              v-else-if="active.detailUnavailable"
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
            v-if="active.sourceUrl && steps.length"
            class="border-t border-gray-100 pt-4 text-xs text-gray-400 dark:border-gray-800 dark:text-gray-500"
          >
            Recipe from
            <a
              :href="active.sourceUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="font-medium text-gray-500 hover:underline dark:text-gray-400"
              >{{ active.sourceName || 'the original source' }}</a
            >
          </p>

          <div class="print-hide space-y-3">
            <!-- Whichever version is on screen is the one the actions act on. -->
            <slot name="actions" :recipe="active" />

            <!--
              An enhanced version cannot itself be enhanced - the server refuses it, and there
              are only ever two versions of a dish. Reachable by opening one directly, which a
              deep link or a calendar slot can do.
            -->
            <template v-if="canEnhance && !recipe.isEnhanced">
              <AppAlert v-if="enhanceError" variant="error" :message="enhanceError" />

              <div
                v-if="isEnhancing"
                class="flex items-center justify-center gap-3 rounded-xl border border-amber-200 py-3 dark:border-amber-900/40"
              >
                <LoadingSpinner size="sm" />
                <span class="text-sm font-medium text-amber-800 dark:text-amber-200">
                  A chef is reworking this dish…
                </span>
              </div>

              <!--
                One enhanced version per recipe, so this button changes its job once one
                exists: writing a second would leave the reader with three versions of their
                dinner and no way to tell them apart. Wanting a different one is the refresh
                below, which replaces it.
              -->
              <button
                v-else-if="!enhanced"
                type="button"
                class="block w-full rounded-xl border border-amber-300 bg-amber-50 py-3 text-center font-semibold text-amber-800 transition-colors hover:bg-amber-100 dark:border-amber-900/50 dark:bg-amber-950/30 dark:text-amber-200 dark:hover:bg-amber-950/60"
                @click="enhance()"
              >
                ⭐ Enhance meal with AI
              </button>

              <button
                v-else-if="!showingEnhanced"
                type="button"
                class="block w-full rounded-xl border border-amber-300 bg-amber-50 py-3 text-center font-semibold text-amber-800 transition-colors hover:bg-amber-100 dark:border-amber-900/50 dark:bg-amber-950/30 dark:text-amber-200 dark:hover:bg-amber-950/60"
                @click="show(true)"
              >
                ⭐ View the enhanced version
              </button>

              <button
                v-else
                type="button"
                class="block w-full rounded-xl border border-gray-200 py-2.5 text-center text-sm font-semibold text-gray-600 transition-colors hover:border-amber-400 hover:text-amber-700 dark:border-gray-700 dark:text-gray-300 dark:hover:border-amber-700 dark:hover:text-amber-300"
                @click="refresh()"
              >
                Not quite right? Try another take
              </button>

              <p v-if="!isEnhancing && !enhanced" class="text-center text-xs text-gray-500 dark:text-gray-400">
                A Michelin-star take on this same dish, kept alongside the original.
              </p>
            </template>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
