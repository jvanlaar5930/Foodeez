<template>
  <AppLayout>
    <div class="p-6 max-w-4xl mx-auto">
      <!-- Date nav -->
      <div class="flex items-center justify-between mb-6">
        <div class="flex items-center gap-3">
          <button @click="offsetDay(-1)" class="p-2 rounded-lg hover:bg-gray-100 transition-colors">
            <span class="text-gray-600">‹</span>
          </button>
          <div class="text-center">
            <p class="font-bold text-gray-900">{{ displayDate }}</p>
            <p class="text-sm text-gray-500">{{ isToday ? 'Today' : '' }}</p>
          </div>
          <button @click="offsetDay(1)" :disabled="isToday" class="p-2 rounded-lg hover:bg-gray-100 transition-colors disabled:opacity-30">
            <span class="text-gray-600">›</span>
          </button>
        </div>

        <button @click="showAddModal = true"
          class="flex items-center gap-2 bg-green-600 hover:bg-green-700 text-white font-semibold px-4 py-2 rounded-xl transition-colors">
          <span>+</span> Log Meal
        </button>
      </div>

      <!-- Nutrition summary bar -->
      <div v-if="summary" class="bg-white rounded-2xl shadow-sm p-4 mb-6 grid grid-cols-4 gap-4">
        <div class="text-center">
          <p class="text-xl font-bold text-gray-900">{{ Math.round(summary.totalCalories) }}</p>
          <p class="text-xs text-gray-500">/ {{ summary.targetCalories }} kcal</p>
        </div>
        <div class="text-center">
          <p class="text-xl font-bold text-blue-600">{{ Math.round(summary.totalProtein) }}g</p>
          <p class="text-xs text-gray-500">Protein</p>
        </div>
        <div class="text-center">
          <p class="text-xl font-bold text-orange-500">{{ Math.round(summary.totalCarbs) }}g</p>
          <p class="text-xs text-gray-500">Carbs</p>
        </div>
        <div class="text-center">
          <p class="text-xl font-bold text-yellow-600">{{ Math.round(summary.totalFat) }}g</p>
          <p class="text-xs text-gray-500">Fat</p>
        </div>
      </div>

      <!-- Meal sections -->
      <div v-if="isLoading" class="flex justify-center py-12">
        <LoadingSpinner />
      </div>

      <div v-else-if="mealStore.dailyLogs.length === 0" class="text-center py-16">
        <p class="text-5xl mb-4">🍽️</p>
        <p class="text-lg font-semibold text-gray-700">No meals logged {{ isToday ? 'today' : 'on this day' }}</p>
        <p class="text-gray-500 text-sm mt-1">Tap "Log Meal" to start tracking</p>
      </div>

      <div v-else class="space-y-4">
        <MealSection
          v-for="log in mealStore.dailyLogs"
          :key="log.id"
          :meal-log="log"
          @delete-item="handleDeleteItem"
        />
      </div>
    </div>

    <!-- Add meal modal -->
    <AddMealModal
      v-if="showAddModal"
      :selected-date="currentDateStr"
      @close="showAddModal = false"
      @saved="onMealSaved"
    />
  </AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { format, addDays, isToday as dateFnsIsToday } from 'date-fns';
import AppLayout from '@/components/layout/AppLayout.vue';
import MealSection from '@/components/meal/MealSection.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import AddMealModal from '@/components/meal/AddMealModal.vue';
import { useAuthStore } from '@/stores/auth';
import { useMealStore } from '@/stores/meal';

const authStore = useAuthStore();
const mealStore = useMealStore();

const selectedDate = ref(new Date());
const showAddModal = ref(false);
const isLoading = ref(false);

const currentDateStr = computed(() => format(selectedDate.value, 'yyyy-MM-dd'));
const displayDate = computed(() => format(selectedDate.value, 'EEEE, MMMM d'));
const isToday = computed(() => dateFnsIsToday(selectedDate.value));
const summary = computed(() => mealStore.nutritionSummary);

function offsetDay(delta: number) {
  selectedDate.value = addDays(selectedDate.value, delta);
}

async function loadData() {
  if (!authStore.user?.id) return;
  isLoading.value = true;
  try {
    await Promise.all([
      mealStore.fetchDailyLogs(authStore.user.id, currentDateStr.value),
      mealStore.fetchNutritionSummary(authStore.user.id, currentDateStr.value),
    ]);
  } finally {
    isLoading.value = false;
  }
}

function handleDeleteItem(_itemId: string) {
  // TODO: call API to delete item, then reload
  loadData();
}

function onMealSaved() {
  showAddModal.value = false;
  loadData();
}

watch(currentDateStr, loadData);
onMounted(loadData);
</script>
