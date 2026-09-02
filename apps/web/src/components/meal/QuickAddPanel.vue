<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import type { MealTemplate, QuickAddResult, RecentMeal } from '@foodeez/shared';
import { mealService } from '@/services/mealService';
import { mealTemplateService } from '@/services/mealTemplateService';
import { useAuthStore } from '@/stores/auth';

const emit = defineEmits<{ applied: [result: QuickAddResult] }>();

const authStore = useAuthStore();

const description = ref('');
const isBusy = ref(false);
const busyLabel = ref('');
const error = ref<string | null>(null);
const note = ref<string | null>(null);

const savedMeals = ref<MealTemplate[]>([]);
const recentMeals = ref<RecentMeal[]>([]);

const photoInput = ref<HTMLInputElement | null>(null);

const canSubmit = computed(() => description.value.trim().length > 0 && !isBusy.value);

/**
 * A recent meal already carries its items, so logging one again is a local operation - there
 * is nothing to ask the server for, and no reason to make the user wait for it.
 */
function fromRecent(meal: RecentMeal): QuickAddResult {
  return {
    items: meal.items.map((item) => ({
      foodItem: item.foodItem,
      quantity: item.quantity,
      unit: item.unit,
      source: 'Saved' as const,
      confidence: 1,
    })),
    mealType: meal.mealType,
    fromSavedMeal: true,
  };
}

async function loadLists(): Promise<void> {
  try {
    const [saved, recent] = await Promise.all([
      mealTemplateService.list(),
      mealTemplateService.recent(),
    ]);
    savedMeals.value = saved;
    // A meal already saved under a name is offered as that name, so showing it here too would
    // put the same meal on screen twice under two different labels.
    recentMeals.value = recent.filter((meal) => !meal.isSaved);
  } catch {
    // These are a convenience on top of the box below. Failing to load them is not worth an
    // error message that would sit above a perfectly usable text field.
  }
}

async function run(label: string, work: () => Promise<QuickAddResult>): Promise<void> {
  isBusy.value = true;
  busyLabel.value = label;
  error.value = null;
  note.value = null;

  try {
    const result = await work();
    note.value = result.note ?? null;

    if (result.items.length > 0) {
      emit('applied', result);
      description.value = '';
    }
  } catch (err: unknown) {
    error.value =
      err && typeof err === 'object' && 'response' in err
        ? 'That could not be read. Try again, or add the items below.'
        : 'Something went wrong reading that meal.';
  } finally {
    isBusy.value = false;
    busyLabel.value = '';
  }
}

function submit(): void {
  if (!canSubmit.value) {
    return;
  }

  const text = description.value.trim();
  run('Reading your meal...', () => mealService.quickAdd(authStore.user?.id ?? '', text));
}

function pickPhoto(): void {
  photoInput.value?.click();
}

function onPhotoChosen(event: Event): void {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  // Cleared straight away so choosing the same photo twice in a row still fires a change.
  input.value = '';

  if (file) {
    run('Reading the photo...', () => mealService.parseFoodImage(file));
  }
}

function applySaved(meal: MealTemplate): void {
  run('', () => mealTemplateService.apply(meal.id));
}

function applyRecent(meal: RecentMeal): void {
  emit('applied', fromRecent(meal));
}

async function removeSaved(meal: MealTemplate): Promise<void> {
  savedMeals.value = savedMeals.value.filter((m) => m.id !== meal.id);
  try {
    await mealTemplateService.remove(meal.id);
  } finally {
    await loadLists();
  }
}

onMounted(loadLists);

// The dialog asks for a refresh after it saves a new meal, so the new name appears without
// the user having to close and reopen it.
defineExpose({ refresh: loadLists });
</script>

<template>
  <div class="rounded-xl border border-green-200 bg-green-50/60 p-4 dark:border-green-900 dark:bg-green-950/30">
    <div class="mb-2 flex items-center justify-between">
      <label for="quick-add" class="text-sm font-semibold text-green-800 dark:text-green-300">
        Quick add
      </label>
      <span class="text-xs text-green-700/70 dark:text-green-400/70">
        Describe the whole meal at once
      </span>
    </div>

    <textarea
      id="quick-add"
      v-model="description"
      rows="2"
      :disabled="isBusy"
      placeholder="e.g. turkey sandwich on 2 slices of rye with mayo, and an apple"
      class="w-full resize-none rounded-lg border border-green-300 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-60 dark:border-green-800 dark:bg-gray-900"
      @keydown.enter.exact.prevent="submit"
    />

    <div class="mt-2 flex items-center gap-2">
      <button
        type="button"
        :disabled="!canSubmit"
        class="flex items-center justify-center gap-2 rounded-lg bg-green-600 px-4 py-1.5 text-sm font-semibold text-white transition-colors hover:bg-green-700 disabled:bg-green-300"
        @click="submit"
      >
        <span
          v-if="isBusy"
          class="h-3.5 w-3.5 animate-spin rounded-full border-2 border-white border-t-transparent"
        />
        {{ isBusy ? busyLabel || 'Working...' : 'Add these' }}
      </button>

      <button
        type="button"
        :disabled="isBusy"
        class="rounded-lg border border-green-300 px-3 py-1.5 text-sm font-medium text-green-800 transition-colors hover:bg-green-100 disabled:opacity-50 dark:border-green-800 dark:text-green-300 dark:hover:bg-green-900/40"
        @click="pickPhoto"
      >
        Photo of the plate
      </button>
      <input
        ref="photoInput"
        type="file"
        accept="image/*"
        capture="environment"
        class="hidden"
        @change="onPhotoChosen"
      />
    </div>

    <p v-if="error" class="mt-2 text-xs text-red-600 dark:text-red-400">{{ error }}</p>
    <p v-else-if="note" class="mt-2 text-xs text-gray-600 dark:text-gray-300">{{ note }}</p>

    <div v-if="savedMeals.length > 0" class="mt-3">
      <p class="mb-1.5 text-xs font-semibold uppercase tracking-wide text-green-800/70 dark:text-green-400/70">
        Your meals
      </p>
      <div class="flex flex-wrap gap-1.5">
        <span
          v-for="meal in savedMeals"
          :key="meal.id"
          class="group inline-flex items-center gap-1 rounded-full border border-green-300 bg-white pl-2.5 pr-1 text-xs font-medium text-green-800 dark:border-green-800 dark:bg-gray-900 dark:text-green-300"
        >
          <button
            type="button"
            :disabled="isBusy"
            class="py-1 transition-opacity hover:opacity-70 disabled:opacity-50"
            :title="`${Math.round(meal.totalNutrition.calories)} kcal`"
            @click="applySaved(meal)"
          >
            {{ meal.name }}
          </button>
          <button
            type="button"
            class="rounded-full px-1 text-green-400 transition-colors hover:text-red-500"
            :title="`Forget ${meal.name}`"
            @click="removeSaved(meal)"
          >
            &times;
          </button>
        </span>
      </div>
    </div>

    <div v-if="recentMeals.length > 0" class="mt-3">
      <p class="mb-1.5 text-xs font-semibold uppercase tracking-wide text-green-800/70 dark:text-green-400/70">
        Log again
      </p>
      <div class="flex flex-wrap gap-1.5">
        <button
          v-for="meal in recentMeals"
          :key="meal.mealLogId"
          type="button"
          class="max-w-full truncate rounded-full border border-gray-300 bg-white px-2.5 py-1 text-xs text-gray-600 transition-colors hover:border-green-400 hover:text-green-800 dark:border-gray-700 dark:bg-gray-900 dark:text-gray-300 dark:hover:text-green-300"
          :title="`${meal.summary} — ${Math.round(meal.totalNutrition.calories)} kcal`"
          @click="applyRecent(meal)"
        >
          {{ meal.summary }}
        </button>
      </div>
    </div>
  </div>
</template>
