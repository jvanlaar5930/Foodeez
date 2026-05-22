<template>
  <div class="fixed inset-0 bg-black/50 z-50 flex items-end sm:items-center justify-center p-4">
    <div class="bg-white rounded-2xl w-full max-w-lg max-h-[90vh] flex flex-col">
      <!-- Header -->
      <div class="flex items-center justify-between p-5 border-b">
        <h2 class="text-lg font-bold text-gray-900">Log a Meal</h2>
        <button @click="$emit('close')" class="text-gray-400 hover:text-gray-600 text-xl">✕</button>
      </div>

      <div class="flex-1 overflow-y-auto p-5 space-y-4">
        <!-- Meal type selector -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Meal Type</label>
          <div class="flex flex-wrap gap-2">
            <button v-for="mt in MEAL_TYPES" :key="mt.value" type="button"
              class="px-3 py-1.5 rounded-full border text-sm font-medium transition-colors"
              :class="selectedMealType === mt.value ? 'bg-green-600 border-green-600 text-white' : 'border-gray-300 text-gray-600 hover:border-green-400'"
              @click="selectedMealType = mt.value">
              {{ mt.label }}
            </button>
          </div>
        </div>

        <!-- Food search -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Search Food</label>
          <input v-model="searchQuery" type="text" placeholder="e.g. chicken breast, oatmeal..."
            class="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500" />
        </div>

        <!-- Search results -->
        <div v-if="searchResults.length > 0" class="border border-gray-200 rounded-lg overflow-hidden">
          <button v-for="item in searchResults" :key="item.id" type="button"
            class="w-full text-left p-3 hover:bg-gray-50 transition-colors border-b last:border-b-0 flex items-center justify-between"
            @click="selectItem(item)">
            <div>
              <p class="font-medium text-sm text-gray-900">{{ item.name }}</p>
              <p class="text-xs text-gray-500">
                {{ item.brand ? item.brand + ' · ' : '' }}{{ item.servingSize }}{{ item.servingUnit }} per serving
              </p>
            </div>
            <span class="text-sm text-gray-500 ml-3 flex-shrink-0">{{ Math.round(item.nutritionalInfo.calories) }} kcal</span>
          </button>
        </div>

        <!-- Selected items -->
        <div v-if="selectedItems.length > 0">
          <label class="block text-sm font-medium text-gray-700 mb-2">Selected Items</label>
          <div class="space-y-2">
            <div v-for="(entry, i) in selectedItems" :key="i"
              class="bg-gray-50 rounded-lg p-3 space-y-2">
              <!-- Item name + remove -->
              <div class="flex items-start justify-between gap-2">
                <div>
                  <p class="text-sm font-medium text-gray-900">{{ entry.item.name }}</p>
                  <p class="text-xs text-gray-400">1 serving = {{ entry.item.servingSize }}{{ entry.item.servingUnit }}</p>
                </div>
                <button @click="selectedItems.splice(i, 1)" class="text-red-400 hover:text-red-600 flex-shrink-0 mt-0.5">✕</button>
              </div>
              <!-- Amount controls -->
              <div class="flex items-center gap-2">
                <button
                  class="w-7 h-7 rounded-full bg-gray-200 hover:bg-gray-300 text-gray-700 flex items-center justify-center text-base leading-none"
                  @click="adjustAmount(entry, -stepFor(entry.item))">−</button>
                <input
                  type="number"
                  :min="stepFor(entry.item)"
                  :step="stepFor(entry.item)"
                  :value="entry.amount"
                  @change="onAmountChange(entry, $event)"
                  class="w-20 text-center text-sm font-semibold border border-gray-300 rounded-lg py-1 focus:outline-none focus:ring-2 focus:ring-green-500" />
                <button
                  class="w-7 h-7 rounded-full bg-gray-200 hover:bg-gray-300 text-gray-700 flex items-center justify-center text-base leading-none"
                  @click="adjustAmount(entry, stepFor(entry.item))">+</button>
                <span class="text-sm text-gray-500">{{ entry.item.servingUnit }}</span>
                <span class="ml-auto text-sm text-gray-400 tabular-nums">
                  {{ Math.round(entry.item.nutritionalInfo.calories * multiplier(entry)) }} kcal
                </span>
              </div>
            </div>
          </div>

          <!-- Nutrition totals -->
          <div class="mt-3 bg-white border border-gray-100 rounded-xl shadow-sm p-4">
            <p class="text-sm font-semibold text-gray-800 mb-3">Meal Totals</p>
            <div class="grid grid-cols-4 gap-2 text-center">
              <div v-for="macro in [
                { label: 'Calories', value: Math.round(totalCalories), unit: 'kcal', color: 'text-green-600' },
                { label: 'Protein',  value: Math.round(totalProtein),  unit: 'g',    color: 'text-blue-600' },
                { label: 'Carbs',    value: Math.round(totalCarbs),    unit: 'g',    color: 'text-orange-500' },
                { label: 'Fat',      value: Math.round(totalFat),      unit: 'g',    color: 'text-amber-500' },
              ]" :key="macro.label">
                <p class="text-xl font-bold" :class="macro.color">
                  {{ macro.value }}<span class="text-xs font-normal text-gray-400 ml-0.5">{{ macro.unit }}</span>
                </p>
                <p class="text-xs text-gray-400 mt-0.5">{{ macro.label }}</p>
              </div>
            </div>

            <!-- AI analyse button -->
            <button
              class="mt-4 w-full flex items-center justify-center gap-2 py-2 rounded-xl border-2 border-dashed border-purple-300 text-purple-600 text-sm font-semibold hover:bg-purple-50 transition-colors disabled:opacity-50"
              :disabled="isAnalyzing"
              @click="analyzeMeal">
              <span v-if="isAnalyzing" class="w-4 h-4 border-2 border-purple-400 border-t-transparent rounded-full animate-spin" />
              <span v-else>✨</span>
              {{ isAnalyzing ? 'Analysing…' : 'AI Meal Analysis' }}
            </button>
          </div>

          <!-- AI analysis result -->
          <div v-if="analysis" class="mt-3 rounded-xl border border-purple-200 bg-purple-50 p-4 space-y-3">
            <!-- Score -->
            <div class="flex items-center gap-3">
              <div class="relative w-14 h-14 flex-shrink-0">
                <svg class="w-14 h-14 -rotate-90" viewBox="0 0 56 56">
                  <circle cx="28" cy="28" r="24" fill="none" stroke="#e9d5ff" stroke-width="5" />
                  <circle cx="28" cy="28" r="24" fill="none"
                    :stroke="scoreColor"
                    stroke-width="5"
                    stroke-linecap="round"
                    :stroke-dasharray="`${(analysis.score / 100) * 150.8} 150.8`" />
                </svg>
                <span class="absolute inset-0 flex items-center justify-center text-sm font-bold" :class="scoreTextColor">
                  {{ analysis.score }}
                </span>
              </div>
              <div>
                <p class="text-xs font-semibold text-purple-700 uppercase tracking-wide">Meal Score</p>
                <p class="text-sm text-gray-700 leading-snug">{{ analysis.completeness }}</p>
              </div>
            </div>

            <!-- Missing -->
            <div v-if="analysis.missing.length > 0">
              <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1.5">Missing</p>
              <div class="flex flex-wrap gap-1.5">
                <span v-for="m in analysis.missing" :key="m"
                  class="text-xs bg-red-100 text-red-700 font-medium px-2 py-0.5 rounded-full">
                  {{ m }}
                </span>
              </div>
            </div>

            <!-- Suggestions -->
            <div v-if="analysis.suggestions.length > 0">
              <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1.5">Suggestions</p>
              <ul class="space-y-1">
                <li v-for="s in analysis.suggestions" :key="s" class="flex items-start gap-1.5 text-sm text-gray-700">
                  <span class="text-green-500 mt-0.5 flex-shrink-0">→</span>{{ s }}
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="p-5 border-t flex gap-3">
        <button @click="$emit('close')" class="flex-1 py-2.5 border-2 border-gray-200 rounded-xl font-semibold text-gray-600 hover:border-gray-300">
          Cancel
        </button>
        <button
          :disabled="selectedItems.length === 0 || isSaving"
          class="flex-1 py-2.5 bg-green-600 hover:bg-green-700 disabled:bg-green-300 text-white rounded-xl font-semibold flex items-center justify-center gap-2"
          @click="handleSave">
          <span v-if="isSaving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
          {{ isSaving ? 'Saving...' : 'Save Meal' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useDebounceFn } from '@vueuse/core';
import { foodItemService } from '@/services/foodItemService';
import { aiService, type MealAnalysisResult } from '@/services/aiService';
import { useMealStore } from '@/stores/meal';
import { useAuthStore } from '@/stores/auth';
import { MealType, type FoodItem } from '@foodeez/shared';

const props = defineProps<{ selectedDate: string }>();
const emit = defineEmits<{ close: []; saved: [] }>();

const mealStore = useMealStore();
const authStore = useAuthStore();

const selectedMealType = ref<MealType>(MealType.Lunch);
const searchQuery = ref('');
const searchResults = ref<FoodItem[]>([]);
const selectedItems = ref<{ item: FoodItem; amount: number }[]>([]);
const isSaving = ref(false);
const isAnalyzing = ref(false);
const analysis = ref<MealAnalysisResult | null>(null);

const MEAL_TYPE_LABELS: Record<MealType, string> = {
  [MealType.Breakfast]: 'Breakfast',
  [MealType.MorningSnack]: 'AM Snack',
  [MealType.Lunch]: 'Lunch',
  [MealType.AfternoonSnack]: 'PM Snack',
  [MealType.Dinner]: 'Dinner',
  [MealType.EveningSnack]: 'Evening Snack',
};

const MEAL_TYPES = [
  { value: MealType.Breakfast, label: 'Breakfast' },
  { value: MealType.MorningSnack, label: 'AM Snack' },
  { value: MealType.Lunch, label: 'Lunch' },
  { value: MealType.AfternoonSnack, label: 'PM Snack' },
  { value: MealType.Dinner, label: 'Dinner' },
  { value: MealType.EveningSnack, label: 'Evening' },
];

// ── Unit helpers ─────────────────────────────────────────────────────────────

function stepFor(item: FoodItem): number {
  switch (item.servingUnit.toLowerCase()) {
    case 'g': case 'gram': case 'grams': return 25;
    case 'ml': case 'milliliter': case 'milliliters': return 25;
    case 'oz': case 'ounce': case 'ounces': return 0.5;
    default: return 0.5;
  }
}

function multiplier(entry: { item: FoodItem; amount: number }): number {
  const size = entry.item.servingSize;
  if (!size || size <= 0) return entry.amount;
  return entry.amount / size;
}

function adjustAmount(entry: { item: FoodItem; amount: number }, delta: number) {
  const step = stepFor(entry.item);
  const next = Math.round((entry.amount + delta) * 100) / 100;
  entry.amount = Math.max(step, next);
  analysis.value = null;
}

function onAmountChange(entry: { item: FoodItem; amount: number }, event: Event) {
  const val = parseFloat((event.target as HTMLInputElement).value);
  if (!isNaN(val) && val > 0) {
    entry.amount = val;
    analysis.value = null;
  }
}

// ── Totals ───────────────────────────────────────────────────────────────────

const totalCalories = computed(() =>
  selectedItems.value.reduce((s, e) => s + e.item.nutritionalInfo.calories * multiplier(e), 0));
const totalProtein = computed(() =>
  selectedItems.value.reduce((s, e) => s + e.item.nutritionalInfo.protein * multiplier(e), 0));
const totalCarbs = computed(() =>
  selectedItems.value.reduce((s, e) => s + e.item.nutritionalInfo.carbohydrates * multiplier(e), 0));
const totalFat = computed(() =>
  selectedItems.value.reduce((s, e) => s + e.item.nutritionalInfo.fat * multiplier(e), 0));
const totalFiber = computed(() =>
  selectedItems.value.reduce((s, e) => s + (e.item.nutritionalInfo.fiber ?? 0) * multiplier(e), 0));

// ── Search ───────────────────────────────────────────────────────────────────

const doSearch = useDebounceFn(async (q: string) => {
  if (q.trim().length < 2) { searchResults.value = []; return; }
  searchResults.value = await foodItemService.searchFoodItems(q);
}, 300);

watch(searchQuery, doSearch);

function selectItem(item: FoodItem) {
  const initialAmount = item.servingSize > 0 ? item.servingSize : 1;
  selectedItems.value.push({ item, amount: initialAmount });
  searchQuery.value = '';
  searchResults.value = [];
  analysis.value = null;
}

// ── AI analysis ──────────────────────────────────────────────────────────────

const scoreColor = computed(() => {
  const s = analysis.value?.score ?? 0;
  if (s >= 75) return '#16a34a';
  if (s >= 50) return '#f59e0b';
  return '#ef4444';
});

const scoreTextColor = computed(() => {
  const s = analysis.value?.score ?? 0;
  if (s >= 75) return 'text-green-600';
  if (s >= 50) return 'text-amber-600';
  return 'text-red-600';
});

async function analyzeMeal() {
  isAnalyzing.value = true;
  analysis.value = null;
  try {
    const mealLabel = MEAL_TYPE_LABELS[selectedMealType.value] ?? 'Meal';
    const items = selectedItems.value.map(e => ({
      name: e.item.name,
      amount: e.amount,
      unit: e.item.servingUnit,
      calories: e.item.nutritionalInfo.calories * multiplier(e),
      protein: e.item.nutritionalInfo.protein * multiplier(e),
      carbs: e.item.nutritionalInfo.carbohydrates * multiplier(e),
      fat: e.item.nutritionalInfo.fat * multiplier(e),
      fiber: (e.item.nutritionalInfo.fiber ?? 0) * multiplier(e),
    }));
    analysis.value = await aiService.analyzeMeal(mealLabel, items);
  } finally {
    isAnalyzing.value = false;
  }
}

// ── Save ─────────────────────────────────────────────────────────────────────

async function handleSave() {
  if (!authStore.user?.id || selectedItems.value.length === 0) return;
  isSaving.value = true;
  try {
    await mealStore.logMeal({
      userId: authStore.user.id,
      logDate: props.selectedDate,
      mealType: selectedMealType.value,
      items: selectedItems.value.map(e => ({
        foodItemId: e.item.id,
        quantity: multiplier(e),
        unit: e.item.servingUnit,
      })),
    });
    isSaving.value = false;
    emit('saved');
  } catch {
    isSaving.value = false;
  }
}
</script>
