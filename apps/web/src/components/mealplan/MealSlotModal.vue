<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { format } from 'date-fns';
import AppButton from '@/components/ui/AppButton.vue';
import AppModal from '@/components/ui/AppModal.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import AiRecipeThumb from '@/components/recipe/AiRecipeThumb.vue';
import RecipeDetailModal from '@/components/recipe/RecipeDetailModal.vue';
import { useDebouncedSearch } from '@/composables/useDebouncedSearch';
import { recipeService } from '@/services/recipeService';
import { MEAL_TYPE_LABELS, MealType, isAiRecipeImage } from '@foodeez/shared';
import type { MealPlanEntry, MealPlanEntryRequest, Recipe } from '@foodeez/shared';

const props = defineProps<{
  modelValue: boolean;
  date: Date;
  mealType: MealType;
  /** The meal already in this slot, if any. Its presence turns the dialog into an edit. */
  entry?: MealPlanEntry;
  saving?: boolean;
  /** Why the last save was refused. Shown here rather than behind the dialog. */
  error?: string | null;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  save: [payload: MealPlanEntryRequest];
  remove: [];
}>();

const name = ref('');
const recipeId = ref<string | undefined>(undefined);
const servings = ref(1);
const validation = ref<string | null>(null);

// Search-as-you-type over the recipe library, so a slot can be filled by picking rather
// than typing a name the calendar cannot link to anything.
const { results, isSearching: searching, onInput: searchByName, reset: clearSearch } =
  useDebouncedSearch(async (query) => (await recipeService.searchRecipes(query, 1, 6)).items);

/**
 * The recipe this slot is linked to, loaded in full. A meal the assistant planned writes a
 * whole recipe - method, ingredients, timings - and the point of opening the slot is usually
 * to read it, so it is fetched here rather than left as a bare name in a text box.
 */
const linkedRecipe = ref<Recipe | null>(null);
const loadingRecipe = ref(false);
const recipeDetailOpen = ref(false);
/** Guards against a slower earlier fetch landing over the slot that is open now. */
let recipeToken = 0;

const isAiThumb = computed(() => isAiRecipeImage(linkedRecipe.value?.imageUrl));

const totalTime = computed(() => {
  const recipe = linkedRecipe.value;
  if (!recipe) return 0;
  return recipe.prepTimeMinutes + recipe.cookTimeMinutes;
});

const recipeMacros = computed(() => {
  const n = linkedRecipe.value?.nutritionalInfoPerServing;
  if (!n) return [];
  return [
    { label: 'kcal', value: Math.round(n.calories) },
    { label: 'protein', value: `${Math.round(n.protein)}g` },
    { label: 'carbs', value: `${Math.round(n.carbohydrates)}g` },
    { label: 'fat', value: `${Math.round(n.fat)}g` },
  ];
});

async function loadRecipe(id: string | undefined): Promise<void> {
  const token = ++recipeToken;
  if (!id) {
    linkedRecipe.value = null;
    loadingRecipe.value = false;
    return;
  }

  loadingRecipe.value = true;
  try {
    const recipe = await recipeService.getRecipeById(id);
    if (token === recipeToken) linkedRecipe.value = recipe;
  } catch {
    // The slot still edits perfectly well without it, so a failed lookup just means no panel.
    if (token === recipeToken) linkedRecipe.value = null;
  } finally {
    if (token === recipeToken) loadingRecipe.value = false;
  }
}

const title = computed(
  () => `${MEAL_TYPE_LABELS[props.mealType]} · ${format(props.date, 'EEE d MMM')}`,
);
const isEdit = computed(() => props.entry !== undefined);

/**
 * A recipe pick is a link to a real recipe; anything else is just a name. Which one the
 * slot currently holds is worth showing, because typing over a picked recipe turns it back
 * into a plain written-down meal.
 */
const linkedToRecipe = computed(() => recipeId.value !== undefined);

function reset(): void {
  const e = props.entry;
  name.value = e?.recipeName ?? e?.foodItemName ?? e?.notes ?? '';
  recipeId.value = e?.recipeId;
  servings.value = e?.servings ?? 1;
  clearSearch();
  validation.value = null;
  recipeDetailOpen.value = false;
  void loadRecipe(e?.recipeId);
}

// Seeded on open rather than on mount: one dialog serves every slot in the grid.
watch(() => props.modelValue, (open) => { if (open) reset(); }, { immediate: true });

function onNameInput(): void {
  // The text no longer describes the picked recipe, so the link goes with it.
  recipeId.value = undefined;
  linkedRecipe.value = null;
  recipeToken++;
  validation.value = null;

  searchByName(name.value);
}

function pick(recipe: Recipe): void {
  name.value = recipe.name;
  recipeId.value = recipe.id;
  clearSearch();
  // Search results carry no ingredients or steps, so this refetches the whole recipe rather
  // than showing a panel with the method missing.
  void loadRecipe(recipe.id);
}

function close(): void {
  emit('update:modelValue', false);
}

function closeRecipeDetail(): void {
  recipeDetailOpen.value = false;
}

function submit(): void {
  const trimmed = name.value.trim();
  if (!trimmed && !recipeId.value) {
    validation.value = 'Pick a recipe or give the meal a name.';
    return;
  }
  if (!(servings.value > 0)) {
    validation.value = 'Servings must be greater than zero.';
    return;
  }

  emit('save', {
    entryDate: format(props.date, 'yyyy-MM-dd'),
    mealType: props.mealType,
    recipeId: recipeId.value,
    // A linked recipe carries its own name; notes would only duplicate it.
    notes: recipeId.value ? undefined : trimmed,
    servings: servings.value,
  });
}
</script>

<template>
  <AppModal
    :model-value="modelValue"
    :title="title"
    size="md"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div class="space-y-4">
      <div>
        <label class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-200">
          Meal
        </label>
        <input
          v-model="name"
          type="text"
          autocomplete="off"
          placeholder="Search recipes, or just type a meal"
          class="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-green-500 focus:outline-none focus:ring-1 focus:ring-green-500 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100"
          @input="onNameInput"
          @keyup.enter="submit"
        />

        <p
          v-if="linkedToRecipe"
          class="mt-1 flex items-center gap-1 text-xs text-green-600 dark:text-green-400"
        >
          <span aria-hidden="true">🔗</span> Linked to a recipe
        </p>
        <p v-else-if="name.trim()" class="mt-1 text-xs text-gray-500 dark:text-gray-400">
          Saved as a written-down meal. Pick a search result to link a recipe instead.
        </p>
      </div>

      <div v-if="loadingRecipe" class="flex justify-center py-3">
        <LoadingSpinner size="sm" />
      </div>

      <!-- The recipe behind this slot. A meal the assistant planned has a full method and
           ingredient list written for it, and this is where someone goes looking for it. -->
      <div
        v-else-if="linkedRecipe"
        class="overflow-hidden rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <div class="flex gap-3 p-3">
          <div class="h-16 w-16 shrink-0 overflow-hidden rounded-lg bg-gray-100 dark:bg-gray-800">
            <AiRecipeThumb v-if="isAiThumb" compact />
            <img
              v-else-if="linkedRecipe.imageUrl"
              :src="linkedRecipe.imageUrl"
              alt=""
              class="h-full w-full object-cover"
            />
          </div>

          <div class="min-w-0 flex-1">
            <p class="flex items-center gap-1.5 text-sm font-semibold text-gray-900 dark:text-gray-100">
              <span class="truncate">{{ linkedRecipe.name }}</span>
              <!-- Which of the two versions this slot is linked to. They share a name. -->
              <span
                v-if="linkedRecipe.isEnhanced"
                title="The enhanced version of this recipe"
                class="shrink-0 rounded-full bg-amber-100 px-1.5 py-0.5 text-[10px] font-semibold text-amber-700 dark:bg-amber-900/40 dark:text-amber-300"
                >⭐ Enhanced</span
              >
            </p>
            <p
              v-if="linkedRecipe.description"
              class="mt-0.5 line-clamp-2 text-xs text-gray-500 dark:text-gray-400"
            >
              {{ linkedRecipe.description }}
            </p>
            <p class="mt-1 text-xs text-gray-400">
              <span v-if="totalTime > 0">{{ totalTime }} min &middot; </span>
              <span>serves {{ linkedRecipe.servings }}</span>
              <span v-if="linkedRecipe.ingredients.length > 0">
                &middot; {{ linkedRecipe.ingredients.length }} ingredients
              </span>
            </p>
          </div>
        </div>

        <div
          v-if="recipeMacros.length > 0"
          class="grid grid-cols-4 gap-1 border-t border-gray-100 px-3 py-2 text-center dark:border-gray-800"
        >
          <div v-for="macro in recipeMacros" :key="macro.label">
            <p class="text-sm font-semibold text-gray-800 dark:text-gray-100">{{ macro.value }}</p>
            <p class="text-[10px] uppercase tracking-wide text-gray-400">{{ macro.label }}</p>
          </div>
        </div>

        <button
          type="button"
          class="w-full border-t border-gray-100 py-2 text-sm font-semibold text-green-700 transition-colors hover:bg-green-50 dark:border-gray-800 dark:text-green-400 dark:hover:bg-green-950/30"
          @click="recipeDetailOpen = true"
        >
          View full recipe
        </button>
      </div>

      <div v-if="searching" class="flex justify-center py-3">
        <LoadingSpinner size="sm" />
      </div>

      <ul
        v-else-if="results.length > 0"
        class="divide-y divide-gray-100 overflow-hidden rounded-lg border border-gray-200 dark:divide-gray-800 dark:border-gray-700"
      >
        <li v-for="recipe in results" :key="recipe.id">
          <button
            type="button"
            class="flex w-full items-center gap-3 px-3 py-2 text-left transition-colors hover:bg-gray-50 dark:hover:bg-gray-800"
            @click="pick(recipe)"
          >
            <img
              v-if="recipe.imageUrl"
              :src="recipe.imageUrl"
              alt=""
              class="h-10 w-10 shrink-0 rounded-md object-cover"
            />
            <span class="min-w-0">
              <span class="block truncate text-sm font-medium text-gray-900 dark:text-gray-100">
                {{ recipe.name }}
              </span>
              <span class="block text-xs text-gray-500 dark:text-gray-400">
                {{ Math.round(recipe.nutritionalInfoPerServing?.calories ?? 0) }} kcal per serving
              </span>
            </span>
          </button>
        </li>
      </ul>

      <div>
        <label class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-200">
          Servings
        </label>
        <input
          v-model.number="servings"
          type="number"
          min="0.25"
          step="0.25"
          class="w-28 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:border-green-500 focus:outline-none focus:ring-1 focus:ring-green-500 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100"
        />
      </div>

      <p v-if="validation ?? error" class="text-sm text-red-600 dark:text-red-400">
        {{ validation ?? error }}
      </p>
    </div>

    <template #footer>
      <div class="flex items-center justify-between gap-3">
        <AppButton
          v-if="isEdit"
          variant="danger"
          size="sm"
          :disabled="saving"
          @click="emit('remove')"
        >
          Remove
        </AppButton>
        <span v-else />

        <div class="flex items-center gap-2">
          <AppButton variant="outline" :disabled="saving" @click="close">Cancel</AppButton>
          <AppButton :loading="saving" @click="submit">
            {{ isEdit ? 'Save' : 'Add meal' }}
          </AppButton>
        </div>
      </div>
    </template>
  </AppModal>

  <!-- Sits outside AppModal so it is not clipped by the dialog it was opened from. -->
  <RecipeDetailModal
    v-if="recipeDetailOpen && linkedRecipe"
    :recipe="linkedRecipe"
    @close="closeRecipeDetail"
  />
</template>
