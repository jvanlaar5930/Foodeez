<template>
  <div class="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-4 sm:items-center">
    <div class="flex max-h-[90vh] w-full max-w-lg flex-col rounded-2xl bg-white dark:bg-gray-900">
      <div class="flex items-center justify-between border-b p-5">
        <h2 class="text-lg font-bold text-gray-900 dark:text-gray-100">{{ isEditing ? 'Edit Meal' : 'Log a Meal' }}</h2>
        <button class="text-xl text-gray-400 hover:text-gray-600 dark:hover:text-gray-300" @click="$emit('close')">x</button>
      </div>

      <div class="flex-1 space-y-4 overflow-y-auto p-5">
        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200">Meal Type</label>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="mealTypeOption in mealTypes"
              :key="mealTypeOption.value"
              type="button"
              class="rounded-full border px-3 py-1.5 text-sm font-medium transition-colors"
              :class="
                selectedMealType === mealTypeOption.value
                  ? 'border-green-600 bg-green-600 text-white'
                  : 'border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:border-green-400'
              "
              @click="setMealType(mealTypeOption.value)"
            >
              {{ mealTypeOption.label }}
            </button>
          </div>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200">Search Food</label>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="e.g. chicken breast, oatmeal..."
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
          />
        </div>

        <!-- Nothing matched: the item probably isn't in any database, so let them enter it. -->
        <button
          v-if="!showCustomForm && searchQuery.trim().length > 1 && searchResults.length === 0"
          type="button"
          class="flex w-full items-center gap-2 rounded-lg border border-dashed border-green-400 px-3 py-2.5 text-sm font-medium text-green-700 transition-colors hover:bg-green-50 dark:border-green-700 dark:text-green-400 dark:hover:bg-green-950/30"
          @click="showCustomForm = true"
        >
          <span class="text-base leading-none">+</span>
          Can't find "{{ searchQuery.trim() }}"? Add it as a homemade food
        </button>

        <CustomFoodForm
          v-if="showCustomForm"
          @created="onCustomFoodCreated"
          @cancel="showCustomForm = false"
        />

        <div v-if="searchResults.length > 0" class="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
          <button
            v-for="item in searchResults"
            :key="item.id"
            type="button"
            class="flex w-full items-center justify-between border-b p-3 text-left transition-colors last:border-b-0 hover:bg-gray-50 dark:hover:bg-gray-800"
            @click="selectItem(item)"
          >
            <div>
              <p class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ item.name }}</p>
              <p class="text-xs text-gray-500 dark:text-gray-400">
                {{ item.brand ? `${item.brand} · ` : '' }}{{ item.servingSize }}{{ item.servingUnit }} per serving
              </p>
            </div>
            <span class="ml-3 shrink-0 text-sm text-gray-500 dark:text-gray-400">
              {{ Math.round(item.nutritionalInfo.calories) }} kcal
            </span>
          </button>
        </div>

        <button
          v-if="!showCustomForm && searchResults.length > 0"
          type="button"
          class="text-sm font-medium text-green-700 hover:underline dark:text-green-400"
          @click="showCustomForm = true"
        >
          None of these? Add a homemade food instead
        </button>

        <div v-if="selectedItems.length > 0">
          <label class="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200">Selected Items</label>
          <div class="space-y-2">
            <div
              v-for="(entry, index) in selectedItems"
              :key="`${entry.item.id}-${index}`"
              class="space-y-2 rounded-lg bg-gray-50 dark:bg-gray-800 p-3"
            >
              <div class="flex items-start justify-between gap-2">
                <div>
                  <p class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ entry.item.name }}</p>
                  <p class="text-xs text-gray-400">
                    1 serving = {{ entry.item.servingSize }}{{ entry.item.servingUnit }}
                  </p>
                </div>
                <button
                  class="mt-0.5 shrink-0 text-red-400 hover:text-red-600 dark:hover:text-red-400"
                  @click="removeItem(index)"
                >
                  x
                </button>
              </div>

              <div class="flex items-center gap-2">
                <button
                  class="flex h-7 w-7 items-center justify-center rounded-full bg-gray-200 dark:bg-gray-700 text-base leading-none text-gray-700 dark:text-gray-200 hover:bg-gray-300"
                  @click="adjustAmount(entry, -stepFor(entry.item))"
                >
                  -
                </button>
                <input
                  :value="entry.amount"
                  :min="stepFor(entry.item)"
                  :step="stepFor(entry.item)"
                  type="number"
                  class="w-20 rounded-lg border border-gray-300 dark:border-gray-600 py-1 text-center text-sm font-semibold focus:outline-none focus:ring-2 focus:ring-green-500"
                  @change="onAmountChange(entry, $event)"
                />
                <button
                  class="flex h-7 w-7 items-center justify-center rounded-full bg-gray-200 dark:bg-gray-700 text-base leading-none text-gray-700 dark:text-gray-200 hover:bg-gray-300"
                  @click="adjustAmount(entry, stepFor(entry.item))"
                >
                  +
                </button>
                <span class="text-sm text-gray-500 dark:text-gray-400">{{ entry.item.servingUnit }}</span>
                <span class="ml-auto tabular-nums text-sm text-gray-400">
                  {{ Math.round(entry.item.nutritionalInfo.calories * multiplier(entry)) }} kcal
                </span>
              </div>
            </div>
          </div>

          <div class="mt-3 rounded-xl border border-gray-100 dark:border-gray-800 bg-white dark:bg-gray-900 p-4 shadow-sm">
            <p class="mb-3 text-sm font-semibold text-gray-800 dark:text-gray-100">Meal Totals</p>
            <div class="grid grid-cols-4 gap-2 text-center">
              <div
                v-for="macro in macros"
                :key="macro.label"
              >
                <p class="text-xl font-bold" :class="macro.color">
                  {{ macro.value }}<span class="ml-0.5 text-xs font-normal text-gray-400">{{ macro.unit }}</span>
                </p>
                <p class="mt-0.5 text-xs text-gray-400">{{ macro.label }}</p>
              </div>
            </div>

            <button
              class="mt-4 flex w-full items-center justify-center gap-2 rounded-xl border-2 border-dashed border-purple-300 py-2 text-sm font-semibold text-purple-600 dark:text-purple-400 transition-colors hover:bg-purple-50 dark:hover:bg-purple-950/40 disabled:opacity-50"
              :disabled="isAnalyzing"
              @click="analyzeMeal"
            >
              <span
                v-if="isAnalyzing"
                class="h-4 w-4 animate-spin rounded-full border-2 border-purple-400 border-t-transparent"
              />
              <span v-else>*</span>
              {{ analyzeLabel }}
            </button>
          </div>

          <div
            v-if="isAnalyzing || analysisError"
            class="mt-3 rounded-xl border border-purple-200 dark:border-purple-900 bg-purple-50 dark:bg-purple-950/40 p-4"
          >
            <p v-if="analysisError" class="text-sm text-red-600 dark:text-red-400">{{ analysisError }}</p>
            <StreamingText v-else :text="streamedText" placeholder="Reading your meal..." />
          </div>

          <div v-if="analysis" class="mt-3 space-y-3 rounded-xl border border-purple-200 bg-purple-50 dark:bg-purple-950/40 p-4">
            <div class="flex items-center gap-3">
              <div class="relative h-14 w-14 shrink-0">
                <svg class="h-14 w-14 -rotate-90" viewBox="0 0 56 56">
                  <circle cx="28" cy="28" r="24" fill="none" stroke="#e9d5ff" stroke-width="5" />
                  <circle
                    cx="28"
                    cy="28"
                    r="24"
                    fill="none"
                    :stroke="scoreColor"
                    stroke-width="5"
                    stroke-linecap="round"
                    :stroke-dasharray="`${(analysis.score / 100) * 150.8} 150.8`"
                  />
                </svg>
                <span class="absolute inset-0 flex items-center justify-center text-sm font-bold" :class="scoreTextColor">
                  {{ analysis.score }}
                </span>
              </div>
              <div>
                <p class="text-xs font-semibold uppercase tracking-wide text-purple-700 dark:text-purple-400">Meal Score</p>
                <p class="text-sm leading-snug text-gray-700 dark:text-gray-200">{{ analysis.completeness }}</p>
                <p v-if="analyzedOn" class="mt-0.5 text-xs text-gray-400">Analyzed {{ analyzedOn }}</p>
              </div>
            </div>

            <div v-if="analysis.missing.length > 0">
              <p class="mb-1.5 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">Missing</p>
              <div class="flex flex-wrap gap-1.5">
                <span
                  v-for="missingItem in analysis.missing"
                  :key="missingItem"
                  class="rounded-full bg-red-100 dark:bg-red-900/40 px-2 py-0.5 text-xs font-medium text-red-700 dark:text-red-300"
                >
                  {{ missingItem }}
                </span>
              </div>
            </div>

            <div v-if="analysis.suggestions.length > 0">
              <p class="mb-1.5 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">Suggestions</p>
              <ul class="space-y-1">
                <li
                  v-for="suggestion in analysis.suggestions"
                  :key="suggestion"
                  class="flex items-start gap-1.5 text-sm text-gray-700 dark:text-gray-200"
                >
                  <span class="mt-0.5 shrink-0 text-green-500">></span>{{ suggestion }}
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>

      <div class="flex gap-3 border-t p-5">
        <button
          class="flex-1 rounded-xl border-2 border-gray-200 dark:border-gray-700 py-2.5 font-semibold text-gray-600 dark:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600"
          @click="$emit('close')"
        >
          Cancel
        </button>
        <button
          :disabled="selectedItems.length === 0 || isSaving"
          class="flex flex-1 items-center justify-center gap-2 rounded-xl bg-green-600 py-2.5 font-semibold text-white disabled:bg-green-300 hover:bg-green-700"
          @click="handleSave"
        >
          <span
            v-if="isSaving"
            class="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent"
          />
          {{ isSaving ? 'Saving...' : isEditing ? 'Update Meal' : 'Save Meal' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import CustomFoodForm from '@/components/meal/CustomFoodForm.vue';
import StreamingText from '@/components/ai/StreamingText.vue';
import { computed, ref, watch } from 'vue';
import { useDebounceFn } from '@vueuse/core';
import { foodItemService } from '@/services/foodItemService';
import { aiService, type MealAnalysisResult } from '@/services/aiService';
import { scoreStroke, scoreTextClass } from '@/utils/analysisScore';
import { AIStreamError } from '@/services/aiStream';
import { useMealStore } from '@/stores/meal';
import { useAuthStore } from '@/stores/auth';
import { MealType, type FoodItem, type MealLog } from '@foodeez/shared';

interface SelectedItem {
  item: FoodItem;
  amount: number;
}

const props = defineProps<{
  selectedDate: string;
  mealLog?: MealLog | null;
}>();
const emit = defineEmits<{ close: []; saved: [] }>();

const mealStore = useMealStore();
const authStore = useAuthStore();

const selectedMealType = ref<MealType>(MealType.Lunch);
const showCustomForm = ref(false);
const searchQuery = ref('');
const searchResults = ref<FoodItem[]>([]);
const selectedItems = ref<SelectedItem[]>([]);
const isSaving = ref(false);
const isAnalyzing = ref(false);
const analysis = ref<MealAnalysisResult | null>(null);
/** What the model has written so far, shown while it writes. */
const streamedText = ref('');
const analysisError = ref<string | null>(null);
/** True once the meal has been changed here, so the saved meal's stored score no longer applies. */
const mealEdited = ref(false);

const isEditing = computed(() => Boolean(props.mealLog));

const mealTypeLabels: Record<MealType, string> = {
  [MealType.Breakfast]: 'Breakfast',
  [MealType.MorningSnack]: 'AM Snack',
  [MealType.Lunch]: 'Lunch',
  [MealType.AfternoonSnack]: 'PM Snack',
  [MealType.Dinner]: 'Dinner',
  [MealType.EveningSnack]: 'Evening Snack',
};

const mealTypes = [
  { value: MealType.Breakfast, label: 'Breakfast' },
  { value: MealType.MorningSnack, label: 'AM Snack' },
  { value: MealType.Lunch, label: 'Lunch' },
  { value: MealType.AfternoonSnack, label: 'PM Snack' },
  { value: MealType.Dinner, label: 'Dinner' },
  { value: MealType.EveningSnack, label: 'Evening' },
];

function stepFor(item: FoodItem): number {
  switch (item.servingUnit.toLowerCase()) {
    case 'g':
    case 'gram':
    case 'grams':
    case 'ml':
    case 'milliliter':
    case 'milliliters':
      return 25;
    case 'oz':
    case 'ounce':
    case 'ounces':
      return 0.5;
    default:
      return 0.5;
  }
}

function multiplier(entry: SelectedItem): number {
  const size = entry.item.servingSize;
  if (!size || size <= 0) {
    return entry.amount;
  }

  return entry.amount / size;
}

/** Any change to the meal makes the current score stale - drop it rather than show a wrong one. */
function invalidateAnalysis() {
  analysis.value = null;
  streamedText.value = '';
  analysisError.value = null;
  mealEdited.value = true;
}

function setMealType(mealType: MealType) {
  selectedMealType.value = mealType;
  invalidateAnalysis();
}

function removeItem(index: number) {
  selectedItems.value.splice(index, 1);
  invalidateAnalysis();
}

function adjustAmount(entry: SelectedItem, delta: number) {
  const step = stepFor(entry.item);
  const next = Math.round((entry.amount + delta) * 100) / 100;
  entry.amount = Math.max(step, next);
  invalidateAnalysis();
}

function onAmountChange(entry: SelectedItem, event: Event) {
  const value = parseFloat((event.target as HTMLInputElement).value);
  if (!Number.isNaN(value) && value > 0) {
    entry.amount = value;
    invalidateAnalysis();
  }
}

const totalCalories = computed(() =>
  selectedItems.value.reduce((sum, entry) => sum + entry.item.nutritionalInfo.calories * multiplier(entry), 0),
);
const totalProtein = computed(() =>
  selectedItems.value.reduce((sum, entry) => sum + entry.item.nutritionalInfo.protein * multiplier(entry), 0),
);
const totalCarbs = computed(() =>
  selectedItems.value.reduce((sum, entry) => sum + entry.item.nutritionalInfo.carbohydrates * multiplier(entry), 0),
);
const totalFat = computed(() =>
  selectedItems.value.reduce((sum, entry) => sum + entry.item.nutritionalInfo.fat * multiplier(entry), 0),
);

const macros = computed(() => [
  { label: 'Calories', value: Math.round(totalCalories.value), unit: 'kcal', color: 'text-green-600 dark:text-green-400' },
  { label: 'Protein', value: Math.round(totalProtein.value), unit: 'g', color: 'text-blue-600 dark:text-blue-400' },
  { label: 'Carbs', value: Math.round(totalCarbs.value), unit: 'g', color: 'text-orange-500 dark:text-orange-400' },
  { label: 'Fat', value: Math.round(totalFat.value), unit: 'g', color: 'text-amber-500' },
]);

const doSearch = useDebounceFn(async (query: string) => {
  if (query.trim().length < 2) {
    searchResults.value = [];
    return;
  }

  searchResults.value = await foodItemService.searchFoodItems(query);
}, 300);

watch(searchQuery, doSearch);
watch(
  () => props.mealLog,
  (mealLog) => {
    selectedMealType.value = mealLog?.mealType ?? MealType.Lunch;
    selectedItems.value = mealLog?.items.map((item) => ({
      item: item.foodItem,
      amount: item.quantity,
    })) ?? [];
    searchQuery.value = '';
    searchResults.value = [];
    analysis.value = mealLog?.analysis ?? null;
    streamedText.value = '';
    analysisError.value = null;
    mealEdited.value = false;
  },
  { immediate: true },
);

/** A freshly created homemade food goes straight into the meal. */
function onCustomFoodCreated(item: FoodItem) {
  showCustomForm.value = false;
  selectItem(item);
}

function selectItem(item: FoodItem) {
  const initialAmount = item.servingSize > 0 ? item.servingSize : 1;
  selectedItems.value.push({ item, amount: initialAmount });
  searchQuery.value = '';
  searchResults.value = [];
  invalidateAnalysis();
}

const scoreColor = computed(() => scoreStroke(analysis.value?.score ?? 0));
const scoreTextColor = computed(() => scoreTextClass(analysis.value?.score ?? 0));

const analyzeLabel = computed(() => {
  if (isAnalyzing.value) {
    return 'Analyzing...';
  }

  return analysis.value ? 'Re-run AI Analysis' : 'AI Meal Analysis';
});

const analyzedOn = computed(() =>
  analysis.value?.generatedAt
    ? new Date(analysis.value.generatedAt).toLocaleDateString(undefined, {
        month: 'short',
        day: 'numeric',
      })
    : null,
);

/**
 * For a saved meal that hasn't been touched here, the server answers from the score stored on
 * the meal - only a click on an already-scored meal asks it to spend another AI call.
 */
async function analyzeMeal() {
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

    const mealLabel = mealTypeLabels[selectedMealType.value] ?? 'Meal';
    analysis.value = await aiService.analyzeMealStream(
      authStore.user?.id ?? '',
      mealLabel,
      selectedItems.value.map((entry) => ({
        name: entry.item.name,
        amount: entry.amount,
        unit: entry.item.servingUnit,
        calories: entry.item.nutritionalInfo.calories * multiplier(entry),
        protein: entry.item.nutritionalInfo.protein * multiplier(entry),
        carbs: entry.item.nutritionalInfo.carbohydrates * multiplier(entry),
        fat: entry.item.nutritionalInfo.fat * multiplier(entry),
        fiber: (entry.item.nutritionalInfo.fiber ?? 0) * multiplier(entry),
      })),
      onDelta,
    );
  } catch (err: unknown) {
    analysisError.value =
      err instanceof AIStreamError ? err.message : 'The analysis could not be completed. Please try again.';
  } finally {
    isAnalyzing.value = false;
  }
}

async function handleSave() {
  if (!authStore.user?.id || selectedItems.value.length === 0) {
    return;
  }

  isSaving.value = true;

  try {
    const payload = {
      userId: authStore.user.id,
      logDate: props.selectedDate,
      mealType: selectedMealType.value,
      items: selectedItems.value.map((entry) => ({
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
