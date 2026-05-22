<template>
  <AppLayout>
    <div class="p-6 max-w-6xl mx-auto">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Meal Plan</h1>
          <p class="text-gray-500 text-sm mt-1">Plan and manage your weekly meals</p>
        </div>
        <button
          @click="handleGeneratePlan"
          :disabled="isGenerating"
          class="flex items-center gap-2 bg-orange-500 hover:bg-orange-600 disabled:bg-orange-300 text-white font-semibold px-4 py-2 rounded-xl transition-colors">
          <span v-if="isGenerating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
          <span v-else>✨</span>
          {{ isGenerating ? 'Generating...' : 'AI Generate' }}
        </button>
      </div>

      <!-- Week navigation -->
      <div class="flex items-center gap-4 mb-4">
        <button @click="prevWeek" class="p-2 rounded-lg hover:bg-gray-100 font-bold text-gray-600">‹</button>
        <span class="font-semibold text-gray-800">
          {{ format(weekDays[0], 'MMM d') }} – {{ format(weekDays[6], 'MMM d, yyyy') }}
        </span>
        <button @click="nextWeek" class="p-2 rounded-lg hover:bg-gray-100 font-bold text-gray-600">›</button>
        <button @click="goToCurrentWeek" class="text-sm text-green-600 font-medium hover:text-green-700 ml-2">
          This Week
        </button>
      </div>

      <!-- Weekly calendar grid -->
      <div v-if="isLoading" class="flex justify-center py-16">
        <LoadingSpinner />
      </div>

      <div v-else class="bg-white rounded-2xl shadow-sm overflow-hidden">
        <!-- Day headers -->
        <div class="grid grid-cols-8 border-b">
          <div class="p-3 text-xs font-semibold text-gray-500 uppercase"></div>
          <div v-for="day in weekDays" :key="day.toISOString()"
            class="p-3 text-center border-l"
            :class="isToday(day) ? 'bg-green-50' : ''">
            <p class="text-xs font-semibold text-gray-500 uppercase">{{ format(day, 'EEE') }}</p>
            <p class="text-lg font-bold mt-0.5" :class="isToday(day) ? 'text-green-600' : 'text-gray-900'">
              {{ format(day, 'd') }}
            </p>
          </div>
        </div>

        <!-- Meal rows -->
        <div v-for="mealType in MEAL_TYPES" :key="mealType.value" class="grid grid-cols-8 border-b last:border-b-0">
          <div class="p-3 text-xs font-semibold text-gray-500 flex items-center">{{ mealType.label }}</div>
          <DayMealSlot
            v-for="day in weekDays"
            :key="`${day.toISOString()}-${mealType.value}`"
            :date="day"
            :meal-type="mealType.value"
            :entry="getEntry(day, mealType.value)"
            class="border-l"
            @click="openSlot(day, mealType.value)"
          />
        </div>
      </div>

      <!-- Empty state -->
      <div v-if="!isLoading && !hasEntriesThisWeek" class="mt-6 text-center py-8 bg-white rounded-2xl shadow-sm">
        <p class="text-4xl mb-3">📅</p>
        <p class="font-semibold text-gray-700">No meals planned this week</p>
        <p class="text-gray-500 text-sm mt-1">Click a slot to add a meal or use AI to generate a full plan</p>
      </div>
    </div>
  </AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { format, startOfWeek, addDays, isToday } from 'date-fns';
import AppLayout from '@/components/layout/AppLayout.vue';
import DayMealSlot from '@/components/mealplan/DayMealSlot.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import { useMealPlanStore } from '@/stores/mealPlan';
import { useAuthStore } from '@/stores/auth';
import { MealType, type MealPlanEntry } from '@foodeez/shared';

const authStore = useAuthStore();
const planStore = useMealPlanStore();

const weekStart = ref(startOfWeek(new Date(), { weekStartsOn: 1 }));
const weekDays = computed(() => Array.from({ length: 7 }, (_, i) => addDays(weekStart.value, i)));
const isLoading = computed(() => planStore.isLoading);
const isGenerating = computed(() => planStore.isGenerating);

const MEAL_TYPES = [
  { value: MealType.Breakfast, label: 'Breakfast' },
  { value: MealType.MorningSnack, label: 'AM Snack' },
  { value: MealType.Lunch, label: 'Lunch' },
  { value: MealType.AfternoonSnack, label: 'PM Snack' },
  { value: MealType.Dinner, label: 'Dinner' },
  { value: MealType.EveningSnack, label: 'Eve Snack' },
];

function getEntry(date: Date, mealType: MealType): MealPlanEntry | undefined {
  const dateStr = format(date, 'yyyy-MM-dd');
  return planStore.activePlan?.entries.find(e => e.entryDate === dateStr && e.mealType === mealType);
}

const hasEntriesThisWeek = computed(() =>
  weekDays.value.some(d => MEAL_TYPES.some(mt => getEntry(d, mt.value) !== undefined))
);

function prevWeek() { weekStart.value = addDays(weekStart.value, -7); }
function nextWeek() { weekStart.value = addDays(weekStart.value, 7); }
function goToCurrentWeek() { weekStart.value = startOfWeek(new Date(), { weekStartsOn: 1 }); }

function openSlot(date: Date, _mealType: MealType) {
  // TODO: open slot assignment modal
  console.log('Open slot for', format(date, 'yyyy-MM-dd'), _mealType);
}

async function handleGeneratePlan() {
  if (!authStore.user?.id) return;
  await planStore.generatePlan({
    userId: authStore.user.id,
    startDate: format(weekDays.value[0], 'yyyy-MM-dd'),
    endDate: format(weekDays.value[6], 'yyyy-MM-dd'),
  });
}

onMounted(() => {
  if (authStore.user?.id) planStore.fetchPlans(authStore.user.id);
});
</script>
