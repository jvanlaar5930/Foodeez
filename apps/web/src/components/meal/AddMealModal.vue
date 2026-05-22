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
              <p class="text-xs text-gray-500">{{ item.brand ?? '' }} · {{ item.servingSize }} {{ item.servingUnit }}</p>
            </div>
            <span class="text-sm text-gray-500 ml-3">{{ Math.round(item.nutritionalInfo.calories) }} kcal</span>
          </button>
        </div>

        <!-- Selected items -->
        <div v-if="selectedItems.length > 0">
          <label class="block text-sm font-medium text-gray-700 mb-2">Selected Items</label>
          <div class="space-y-2">
            <div v-for="(entry, i) in selectedItems" :key="i"
              class="flex items-center gap-3 bg-gray-50 rounded-lg p-3">
              <div class="flex-1">
                <p class="text-sm font-medium text-gray-900">{{ entry.item.name }}</p>
                <p class="text-xs text-gray-500">{{ Math.round(entry.item.nutritionalInfo.calories * entry.quantity) }} kcal</p>
              </div>
              <div class="flex items-center gap-2">
                <button @click="entry.quantity = Math.max(0.25, entry.quantity - 0.25)" class="w-6 h-6 rounded-full bg-gray-200 text-gray-600 text-sm flex items-center justify-center">−</button>
                <span class="text-sm font-medium w-8 text-center">{{ entry.quantity }}</span>
                <button @click="entry.quantity += 0.25" class="w-6 h-6 rounded-full bg-gray-200 text-gray-600 text-sm flex items-center justify-center">+</button>
                <span class="text-xs text-gray-500">{{ entry.item.servingUnit }}</span>
              </div>
              <button @click="selectedItems.splice(i, 1)" class="text-red-400 hover:text-red-600">✕</button>
            </div>
          </div>

          <!-- Nutrition preview -->
          <div class="mt-3 bg-green-50 rounded-lg p-3">
            <p class="text-xs font-semibold text-green-700 mb-1">Total Nutrition</p>
            <div class="flex gap-4 text-sm">
              <span class="text-gray-700">{{ Math.round(totalCalories) }} kcal</span>
              <span class="text-blue-600">P: {{ Math.round(totalProtein) }}g</span>
              <span class="text-orange-500">C: {{ Math.round(totalCarbs) }}g</span>
              <span class="text-yellow-600">F: {{ Math.round(totalFat) }}g</span>
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
const selectedItems = ref<{ item: FoodItem; quantity: number }[]>([]);
const isSaving = ref(false);

const MEAL_TYPES = [
  { value: MealType.Breakfast, label: 'Breakfast' },
  { value: MealType.MorningSnack, label: 'AM Snack' },
  { value: MealType.Lunch, label: 'Lunch' },
  { value: MealType.AfternoonSnack, label: 'PM Snack' },
  { value: MealType.Dinner, label: 'Dinner' },
  { value: MealType.EveningSnack, label: 'Evening' },
];

const totalCalories = computed(() => selectedItems.value.reduce((sum, e) => sum + e.item.nutritionalInfo.calories * e.quantity, 0));
const totalProtein = computed(() => selectedItems.value.reduce((sum, e) => sum + e.item.nutritionalInfo.protein * e.quantity, 0));
const totalCarbs = computed(() => selectedItems.value.reduce((sum, e) => sum + e.item.nutritionalInfo.carbohydrates * e.quantity, 0));
const totalFat = computed(() => selectedItems.value.reduce((sum, e) => sum + e.item.nutritionalInfo.fat * e.quantity, 0));

const doSearch = useDebounceFn(async (q: string) => {
  if (q.trim().length < 2) { searchResults.value = []; return; }
  searchResults.value = await foodItemService.searchFoodItems(q);
}, 300);

watch(searchQuery, doSearch);

function selectItem(item: FoodItem) {
  selectedItems.value.push({ item, quantity: 1 });
  searchQuery.value = '';
  searchResults.value = [];
}

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
        quantity: e.quantity,
        unit: e.item.servingUnit,
      })),
    });
    emit('saved');
  } finally {
    isSaving.value = false;
  }
}
</script>
