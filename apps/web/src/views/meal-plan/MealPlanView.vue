<template>
  <AppLayout>
    <div class="p-6 max-w-6xl mx-auto">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Meal Plan</h1>
          <p class="text-gray-500 dark:text-gray-400 text-sm mt-1">Plan and manage your weekly meals</p>
        </div>
        <div class="flex items-center gap-2">
          <button
            @click="handleGeneratePlan"
            :disabled="isGenerating"
            class="flex items-center gap-2 bg-orange-500 hover:bg-orange-600 disabled:bg-orange-300 text-white font-semibold px-4 py-2 rounded-xl transition-colors">
            <span v-if="isGenerating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
            <span v-else>✨</span>
            {{ isGenerating ? 'Generating...' : 'AI Generate' }}
          </button>
          <!-- Generation can take minutes on a self-hosted model, so there has to be a
               way out that actually stops the work rather than just hiding the spinner. -->
          <button
            v-if="isGenerating"
            type="button"
            @click="planStore.cancelGeneration()"
            class="rounded-xl border border-gray-300 px-4 py-2 font-semibold text-gray-600 transition-colors hover:border-gray-400 hover:text-gray-800 dark:border-gray-700 dark:text-gray-300 dark:hover:text-gray-100">
            Cancel
          </button>
        </div>
      </div>

      <!-- The model's thinking, as it writes it: a week of meals takes a while, and a
           spinner alone gives no sign that anything is happening. -->
      <div
        v-if="isGenerating"
        class="mb-4 rounded-xl border border-purple-200 bg-purple-50 p-4 dark:border-purple-900 dark:bg-purple-950/40"
      >
        <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-purple-700 dark:text-purple-400">
          Writing your plan
        </p>
        <StreamingText :text="generationText" placeholder="Reading your profile and targets..." />
      </div>

      <!-- Generation can fail for reasons worth reading: the AI provider being overloaded
           is temporary and retrying is the right response. -->
      <div
        v-if="error"
        class="mb-4 flex items-start justify-between gap-4 rounded-xl border border-amber-200 bg-amber-50 p-4 dark:border-amber-900/40 dark:bg-amber-950/30"
      >
        <p class="text-sm text-amber-800 dark:text-amber-200">{{ error }}</p>
        <button
          type="button"
          class="shrink-0 text-sm font-semibold text-amber-900 hover:underline dark:text-amber-100"
          @click="handleGeneratePlan"
        >
          Try again
        </button>
      </div>

      <!-- Week navigation -->
      <div class="flex items-center gap-4 mb-4">
        <button @click="prevWeek" class="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 font-bold text-gray-600 dark:text-gray-300">‹</button>
        <span class="font-semibold text-gray-800 dark:text-gray-100">
          {{ format(weekDays[0], 'MMM d') }} – {{ format(weekDays[6], 'MMM d, yyyy') }}
        </span>
        <button @click="nextWeek" class="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 font-bold text-gray-600 dark:text-gray-300">›</button>
        <button @click="goToCurrentWeek" class="text-sm text-green-600 dark:text-green-400 font-medium hover:text-green-700 dark:hover:text-green-400 ml-2">
          This Week
        </button>
      </div>

      <!-- Weekly calendar grid -->
      <div v-if="isLoading" class="flex justify-center py-16">
        <LoadingSpinner />
      </div>

      <div v-else class="bg-white dark:bg-gray-900 rounded-2xl shadow-sm overflow-hidden">
        <!-- Day headers -->
        <div class="grid grid-cols-8 border-b">
          <div class="p-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase"></div>
          <div v-for="day in weekDays" :key="day.toISOString()"
            class="p-3 text-center border-l"
            :class="isToday(day) ? 'bg-green-50 dark:bg-green-950/40' : ''">
            <p class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">{{ format(day, 'EEE') }}</p>
            <p class="text-lg font-bold mt-0.5" :class="isToday(day) ? 'text-green-600 dark:text-green-400' : 'text-gray-900 dark:text-gray-100'">
              {{ format(day, 'd') }}
            </p>
          </div>
        </div>

        <!-- Meal rows -->
        <div v-for="mealType in MEAL_TYPES" :key="mealType.value" class="grid grid-cols-8 border-b last:border-b-0">
          <div class="p-3 text-xs font-semibold text-gray-500 dark:text-gray-400 flex items-center">{{ mealType.label }}</div>
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
      <div v-if="!isLoading && !hasEntriesThisWeek" class="mt-6 text-center py-8 bg-white dark:bg-gray-900 rounded-2xl shadow-sm">
        <p class="text-4xl mb-3">📅</p>
        <p class="font-semibold text-gray-700 dark:text-gray-200">No meals planned this week</p>
        <p class="text-gray-500 dark:text-gray-400 text-sm mt-1">Click a slot to add a meal or use AI to generate a full plan</p>
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
import StreamingText from '@/components/ai/StreamingText.vue';
import { useMealPlanStore } from '@/stores/mealPlan';
import { useAuthStore } from '@/stores/auth';
import { MealType, type MealPlanEntry } from '@foodeez/shared';

const authStore = useAuthStore();
const planStore = useMealPlanStore();

const weekStart = ref(startOfWeek(new Date(), { weekStartsOn: 1 }));
const weekDays = computed(() => Array.from({ length: 7 }, (_, i) => addDays(weekStart.value, i)));
const isLoading = computed(() => planStore.isLoading);
const isGenerating = computed(() => planStore.isGenerating);
const generationText = computed(() => planStore.generationText);
const error = computed(() => planStore.error);

const MEAL_TYPES = [
  { value: MealType.Breakfast, label: 'Breakfast' },
  { value: MealType.MorningSnack, label: 'AM Snack' },
  { value: MealType.Lunch, label: 'Lunch' },
  { value: MealType.AfternoonSnack, label: 'PM Snack' },
  { value: MealType.Dinner, label: 'Dinner' },
  { value: MealType.EveningSnack, label: 'Eve Snack' },
];

function getEntry(date: Date, mealType: MealType): MealPlanEntry | undefined {
  // Grouped by date on the server, so this is a lookup. The `?? []` is load-bearing: a day
  // with no meals has no key at all, and the old `.entries` did not exist on the payload.
  const dateStr = format(date, 'yyyy-MM-dd');
  const forDay = planStore.activePlan?.entriesByDate?.[dateStr] ?? [];
  return forDay.find(e => e.mealType === mealType);
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
  try {
    await planStore.generatePlan({
      userId: authStore.user.id,
      startDate: format(weekDays.value[0], 'yyyy-MM-dd'),
      endDate: format(weekDays.value[6], 'yyyy-MM-dd'),
    });
  } catch {
    // The store has already put the reason in `error`, which the banner above renders.
    // Without this catch the rethrow escaped as an unhandled rejection and the button
    // just silently stopped spinning.
  }
}

onMounted(() => {
  if (authStore.user?.id) planStore.fetchPlans(authStore.user.id);
});
</script>
