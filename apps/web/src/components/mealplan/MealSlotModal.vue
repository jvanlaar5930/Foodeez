<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { format } from 'date-fns';
import AppButton from '@/components/ui/AppButton.vue';
import AppModal from '@/components/ui/AppModal.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import { recipeService } from '@/services/recipeService';
import { MEAL_TYPE_LABELS, MealType } from '@foodeez/shared';
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
const results = ref<Recipe[]>([]);
const searching = ref(false);
const validation = ref<string | null>(null);

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
  results.value = [];
  validation.value = null;
}

// Seeded on open rather than on mount: one dialog serves every slot in the grid.
watch(() => props.modelValue, (open) => { if (open) reset(); }, { immediate: true });

let searchTimer: ReturnType<typeof setTimeout> | undefined;
let searchToken = 0;

function onNameInput(): void {
  // The text no longer describes the picked recipe, so the link goes with it.
  recipeId.value = undefined;
  validation.value = null;

  clearTimeout(searchTimer);
  const query = name.value.trim();
  if (query.length < 2) {
    results.value = [];
    searching.value = false;
    return;
  }

  searching.value = true;
  searchTimer = setTimeout(async () => {
    const token = ++searchToken;
    try {
      const page = await recipeService.searchRecipes(query, 1, 6);
      // A slower earlier search must not overwrite the results for what is typed now.
      if (token === searchToken) results.value = page.items;
    } catch {
      // Search is a convenience here; the typed name still works on its own.
      if (token === searchToken) results.value = [];
    } finally {
      if (token === searchToken) searching.value = false;
    }
  }, 300);
}

function pick(recipe: Recipe): void {
  name.value = recipe.name;
  recipeId.value = recipe.id;
  results.value = [];
}

function close(): void {
  emit('update:modelValue', false);
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
</template>
