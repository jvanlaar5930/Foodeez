<script setup lang="ts">
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import { computed, ref, watch } from 'vue';
import QuickAddPanel from '@/components/meal/QuickAddPanel.vue';
import MealTypePicker from '@/components/meal/MealTypePicker.vue';
import FoodSearchPanel from '@/components/meal/FoodSearchPanel.vue';
import SelectedFoodRow from '@/components/meal/SelectedFoodRow.vue';
import MealTotals from '@/components/meal/MealTotals.vue';
import MealAnalysisPanel from '@/components/meal/MealAnalysisPanel.vue';
import SaveAsTemplateBar from '@/components/meal/SaveAsTemplateBar.vue';
import { useMealItems, servingsOf } from '@/composables/useMealItems';
import { useScrollLock } from '@/composables/useScrollLock';
import { aiService, type MealAnalysisResult } from '@/services/aiService';
import { extractErrorMessage } from '@/utils/apiError';
import { useMealStore } from '@/stores/meal';
import { useAuthStore } from '@/stores/auth';
import {
  MEAL_TYPE_SHORT_LABELS,
  MealType,
  type FoodItem,
  type MealLog,
  type QuickAddResult,
} from '@foodeez/shared';

const props = defineProps<{
  selectedDate: string;
  mealLog?: MealLog | null;
}>();
const emit = defineEmits<{ close: []; saved: [] }>();

const mealStore = useMealStore();
const authStore = useAuthStore();

useScrollLock(ref(true));

const selectedMealType = ref<MealType>(MealType.Lunch);
const isSaving = ref(false);

const quickAddPanel = ref<InstanceType<typeof QuickAddPanel> | null>(null);
const searchPanel = ref<InstanceType<typeof FoodSearchPanel> | null>(null);
const templateBar = ref<InstanceType<typeof SaveAsTemplateBar> | null>(null);

const analysis = ref<MealAnalysisResult | null>(null);
const isAnalyzing = ref(false);
/** What the model has written so far, shown while it writes. */
const streamedText = ref('');
const analysisError = ref<string | null>(null);
/** True once the meal has been changed here, so a saved meal's stored score no longer applies. */
const mealEdited = ref(false);

/** Any change to the meal makes the current score stale - drop it rather than show a wrong one. */
function invalidateAnalysis(): void {
  analysis.value = null;
  streamedText.value = '';
  analysisError.value = null;
  mealEdited.value = true;
}

const {
  items: mealItems,
  totals,
  add: addToMeal,
  remove: removeFromMeal,
  adjust: adjustAmount,
  setAmount,
  reset: resetMeal,
} = useMealItems(invalidateAnalysis);

const isEditing = computed(() => Boolean(props.mealLog));

function setMealType(mealType: MealType): void {
  selectedMealType.value = mealType;
  invalidateAnalysis();
}

function addFood(item: FoodItem): void {
  addToMeal(item);
}

/**
 * Quick add's items land in the same list a searched-for food does, so everything already
 * built on that list - amounts, removal, the AI analysis, saving - keeps working untouched.
 */
function applyQuickAdd(result: QuickAddResult): void {
  for (const parsed of result.items) {
    addToMeal(parsed.foodItem, { amount: parsed.quantity, source: parsed.source });
  }

  // Only a saved meal knows which meal it is; a described one leaves the choice alone.
  if (result.mealType !== undefined && result.mealType !== null) {
    selectedMealType.value = result.mealType;
  }

  searchPanel.value?.clear();
  invalidateAnalysis();
}

// Seeded on open rather than on mount: one dialog serves both logging and editing.
watch(
  () => props.mealLog,
  (mealLog) => {
    selectedMealType.value = mealLog?.mealType ?? MealType.Lunch;
    resetMeal(mealLog?.items.map((item) => ({ item: item.foodItem, amount: item.quantity })) ?? []);
    searchPanel.value?.clear();
    templateBar.value?.reset();
    analysis.value = mealLog?.analysis ?? null;
    streamedText.value = '';
    analysisError.value = null;
    // Reset last: `resetMeal` deliberately does not count as an edit, but the seeding above
    // must not leave the dialog thinking the stored score is stale.
    mealEdited.value = false;
  },
  { immediate: true },
);

/**
 * For a saved meal that hasn't been touched here, the server answers from the score stored on
 * the meal - only a click on an already-scored meal asks it to spend another AI call.
 */
async function analyzeMeal(): Promise<void> {
  const storedMealLogId = props.mealLog && !mealEdited.value ? props.mealLog.id : null;
  const refresh = analysis.value !== null;
  const onDelta = (text: string) => {
    streamedText.value += text;
  };

  isAnalyzing.value = true;
  analysis.value = null;
  streamedText.value = '';
  analysisError.value = null;

  try {
    if (storedMealLogId) {
      analysis.value = await aiService.analyzeMealLogStream(storedMealLogId, refresh, onDelta);
      return;
    }

    analysis.value = await aiService.analyzeMealStream(
      authStore.user?.id ?? '',
      MEAL_TYPE_SHORT_LABELS[selectedMealType.value] ?? 'Meal',
      mealItems.value.map((entry) => {
        const servings = servingsOf(entry);
        const nutrition = entry.item.nutritionalInfo;

        return {
          name: entry.item.name,
          amount: entry.amount,
          unit: entry.item.servingUnit,
          calories: nutrition.calories * servings,
          protein: nutrition.protein * servings,
          carbs: nutrition.carbohydrates * servings,
          fat: nutrition.fat * servings,
          fiber: (nutrition.fiber ?? 0) * servings,
        };
      }),
      onDelta,
    );
  } catch (err: unknown) {
    analysisError.value = extractErrorMessage(
      err,
      'The analysis could not be completed. Please try again.',
    );
  } finally {
    isAnalyzing.value = false;
  }
}

async function handleSave(): Promise<void> {
  if (!authStore.user?.id || mealItems.value.length === 0) {
    return;
  }

  isSaving.value = true;

  try {
    const payload = {
      userId: authStore.user.id,
      logDate: props.selectedDate,
      mealType: selectedMealType.value,
      items: mealItems.value.map((entry) => ({
        foodItemId: entry.item.id,
        quantity: entry.amount,
        unit: entry.item.servingUnit,
      })),
      // Carry the score just generated onto the saved meal so it is never paid for twice.
      analysis: analysis.value ?? undefined,
    };

    if (props.mealLog) {
      await mealStore.updateMealLog(props.mealLog.id, payload);
    } else {
      await mealStore.logMeal(payload);
    }

    emit('saved');
  } finally {
    isSaving.value = false;
  }
}
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-4 sm:items-center">
    <div class="flex max-h-[90vh] w-full max-w-lg flex-col rounded-2xl bg-white dark:bg-gray-900">
      <div class="flex items-center justify-between border-b p-5">
        <h2 class="text-lg font-bold text-gray-900 dark:text-gray-100">
          {{ isEditing ? 'Edit Meal' : 'Log a Meal' }}
        </h2>
        <button
          type="button"
          class="text-xl text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
          aria-label="Close"
          @click="emit('close')"
        >
          x
        </button>
      </div>

      <div class="flex-1 space-y-4 overflow-y-auto p-5">
        <QuickAddPanel ref="quickAddPanel" @applied="applyQuickAdd" />

        <MealTypePicker :model-value="selectedMealType" @update:model-value="setMealType" />

        <FoodSearchPanel ref="searchPanel" @select="addFood" />

        <div v-if="mealItems.length > 0">
          <label class="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200">
            Selected Items
          </label>
          <div class="space-y-2">
            <SelectedFoodRow
              v-for="(entry, index) in mealItems"
              :key="`${entry.item.id}-${index}`"
              :entry="entry"
              @adjust="adjustAmount(entry, $event)"
              @set-amount="setAmount(entry, $event)"
              @remove="removeFromMeal(index)"
            />
          </div>

          <div
            class="mt-3 rounded-xl border border-gray-100 bg-white p-4 shadow-sm dark:border-gray-800 dark:bg-gray-900"
          >
            <p class="mb-3 text-sm font-semibold text-gray-800 dark:text-gray-100">Meal Totals</p>
            <MealTotals :totals="totals" />

            <MealAnalysisPanel
              :analysis="analysis"
              :is-analyzing="isAnalyzing"
              :streamed-text="streamedText"
              :error="analysisError"
              @analyze="analyzeMeal"
            />
          </div>
        </div>
      </div>

      <SaveAsTemplateBar
        v-if="mealItems.length > 0"
        ref="templateBar"
        :items="mealItems"
        :meal-type="selectedMealType"
        @saved="quickAddPanel?.refresh()"
      />

      <div class="flex gap-3 border-t p-5">
        <button
          type="button"
          class="flex-1 rounded-xl border-2 border-gray-200 py-2.5 font-semibold text-gray-600 hover:border-gray-300 dark:border-gray-700 dark:text-gray-300 dark:hover:border-gray-600"
          @click="emit('close')"
        >
          Cancel
        </button>
        <button
          type="button"
          :disabled="mealItems.length === 0 || isSaving"
          class="flex flex-1 items-center justify-center gap-2 rounded-xl bg-green-600 py-2.5 font-semibold text-white hover:bg-green-700 disabled:bg-green-300"
          @click="handleSave"
        >
          <LoadingSpinner v-if="isSaving" size="sm" color="currentColor" />
          {{ isSaving ? 'Saving...' : isEditing ? 'Update Meal' : 'Save Meal' }}
        </button>
      </div>
    </div>
  </div>
</template>
