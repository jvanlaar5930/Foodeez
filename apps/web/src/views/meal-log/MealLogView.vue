<template>
  <AppLayout>
    <div class="mx-auto max-w-4xl p-6">
      <div class="mb-6 flex items-center justify-between">
        <div class="flex items-center gap-3">
          <button class="rounded-lg p-2 transition-colors hover:bg-gray-100 dark:hover:bg-gray-700" @click="offsetDay(-1)">
            <span class="text-gray-600 dark:text-gray-300">&lt;</span>
          </button>
          <div class="text-center">
            <p class="font-bold text-gray-900 dark:text-gray-100">{{ displayDate }}</p>
            <p class="text-sm text-gray-500 dark:text-gray-400">{{ isToday ? 'Today' : '' }}</p>
          </div>
          <button
            :disabled="isToday"
            class="rounded-lg p-2 transition-colors hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-30"
            @click="offsetDay(1)"
          >
            <span class="text-gray-600 dark:text-gray-300">&gt;</span>
          </button>
        </div>

        <button
          class="flex items-center gap-2 rounded-xl bg-green-600 px-4 py-2 font-semibold text-white transition-colors hover:bg-green-700"
          @click="openNewMealModal"
        >
          <span>+</span> Log Meal
        </button>
      </div>

      <div v-if="summary" class="mb-6 grid grid-cols-4 gap-4 rounded-2xl bg-white dark:bg-gray-900 p-4 shadow-sm">
        <div class="text-center">
          <p class="text-xl font-bold text-gray-900 dark:text-gray-100">{{ Math.round(summary.totalCalories) }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400">/ {{ summary.targetCalories }} kcal</p>
        </div>
        <div class="text-center">
          <p class="text-xl font-bold text-blue-600 dark:text-blue-400">{{ Math.round(summary.totalProtein) }}g</p>
          <p class="text-xs text-gray-500 dark:text-gray-400">Protein</p>
        </div>
        <div class="text-center">
          <p class="text-xl font-bold text-orange-500 dark:text-orange-400">{{ Math.round(summary.totalCarbs) }}g</p>
          <p class="text-xs text-gray-500 dark:text-gray-400">Carbs</p>
        </div>
        <div class="text-center">
          <p class="text-xl font-bold text-yellow-600 dark:text-yellow-400">{{ Math.round(summary.totalFat) }}g</p>
          <p class="text-xs text-gray-500 dark:text-gray-400">Fat</p>
        </div>
      </div>

      <div v-if="isLoading" class="flex justify-center py-12">
        <LoadingSpinner />
      </div>

      <div v-else-if="mealStore.dailyLogs.length === 0" class="py-16 text-center">
        <p class="mb-4 text-5xl">Meal Log</p>
        <p class="text-lg font-semibold text-gray-700 dark:text-gray-200">No meals logged {{ isToday ? 'today' : 'on this day' }}</p>
        <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Tap "Log Meal" to start tracking</p>
      </div>

      <div v-else class="space-y-4">
        <DayAnalysisCard
          v-if="authStore.user?.id"
          :user-id="authStore.user.id"
          :date="currentDateStr"
          :is-today="isToday"
        />

        <MealSection
          v-for="log in mealStore.dailyLogs"
          :key="log.id"
          :meal-log="log"
          :meal-type="log.mealType"
          :items="log.items"
          :total-nutrition="log.totalNutrition"
          @edit-meal="handleEditMeal"
          @delete-meal="handleDeleteMeal"
        />
      </div>
    </div>

    <AddMealModal
      v-if="showAddModal"
      :selected-date="currentDateStr"
      :meal-log="editingMealLog"
      @close="closeModal"
      @saved="onMealSaved"
    />
  </AppLayout>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { addDays, format, isToday as dateFnsIsToday } from 'date-fns';
import type { MealLog } from '@foodeez/shared';
import AppLayout from '@/components/layout/AppLayout.vue';
import MealSection from '@/components/meal/MealSection.vue';
import AddMealModal from '@/components/meal/AddMealModal.vue';
import DayAnalysisCard from '@/components/meal/DayAnalysisCard.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import { useAuthStore } from '@/stores/auth';
import { useMealStore } from '@/stores/meal';

const authStore = useAuthStore();
const mealStore = useMealStore();

const selectedDate = ref(new Date());
const showAddModal = ref(false);
const editingMealLog = ref<MealLog | null>(null);
const isLoading = ref(false);

const currentDateStr = computed(() => format(selectedDate.value, 'yyyy-MM-dd'));
const displayDate = computed(() => format(selectedDate.value, 'EEEE, MMMM d'));
const isToday = computed(() => dateFnsIsToday(selectedDate.value));
const summary = computed(() => mealStore.nutritionSummary);

function offsetDay(delta: number) {
  selectedDate.value = addDays(selectedDate.value, delta);
}

function openNewMealModal() {
  editingMealLog.value = null;
  showAddModal.value = true;
}

function closeModal() {
  showAddModal.value = false;
  editingMealLog.value = null;
}

async function loadData() {
  if (!authStore.user?.id) {
    return;
  }

  isLoading.value = true;
  mealStore.setSelectedDate(currentDateStr.value);

  try {
    await mealStore.refreshDay(authStore.user.id, currentDateStr.value);
  } finally {
    isLoading.value = false;
  }
}

function handleEditMeal(mealLog: MealLog) {
  editingMealLog.value = mealLog;
  showAddModal.value = true;
}

async function handleDeleteMeal(mealLog: MealLog) {
  if (!authStore.user?.id) {
    return;
  }

  if (!window.confirm('Delete this meal log?')) {
    return;
  }

  await mealStore.deleteMealLog(mealLog.id, authStore.user.id);
}

function onMealSaved() {
  closeModal();
}

watch(currentDateStr, loadData);
onMounted(loadData);
</script>
