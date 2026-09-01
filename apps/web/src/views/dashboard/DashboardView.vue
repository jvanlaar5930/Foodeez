<template>
  <AppLayout>
    <div class="p-6 max-w-6xl mx-auto">
      <!-- Header -->
      <div class="mb-6">
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">{{ greeting }}, {{ authStore.user?.firstName }}!</h1>
        <p class="text-gray-500 dark:text-gray-400 mt-1">{{ todayDisplay }}</p>
      </div>

      <!-- Recipe search -->
      <form class="mb-6" @submit.prevent="goToRecipes">
        <div class="relative">
          <span class="absolute left-4 top-1/2 -translate-y-1/2 text-green-500 text-lg pointer-events-none">🔍</span>
          <input
            v-model="recipeSearch"
            type="search"
            placeholder="Search recipes to start meal prepping…"
            class="w-full pl-11 pr-32 py-3.5 bg-white dark:bg-gray-900 border-2 border-green-200 dark:border-green-800 rounded-2xl text-sm shadow-sm focus:outline-none focus:border-green-500 focus:ring-2 focus:ring-green-100 dark:focus:ring-green-900/50 transition-all placeholder-gray-400 dark:placeholder-gray-500"
          />
          <button
            type="submit"
            class="absolute right-2 top-1/2 -translate-y-1/2 bg-green-600 hover:bg-green-700 text-white text-sm font-semibold px-4 py-2 rounded-xl transition-colors"
          >
            Find Recipes
          </button>
        </div>
      </form>

      <!-- Main grid -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
        <!-- Calorie & Macros card -->
        <div class="lg:col-span-1 bg-white dark:bg-gray-900 rounded-2xl shadow-sm p-6">
          <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Today's Nutrition</h2>

          <div class="flex justify-center mb-6">
            <CalorieDonut
              :consumed="summary?.totalCalories ?? 0"
              :target="summary?.targetCalories ?? 2000"
            />
          </div>

          <div class="space-y-3">
            <MacroBar label="Protein" :current="summary?.totalProtein ?? 0" :target="summary?.targetProtein ?? 150" color="#3B82F6" unit="g" />
            <MacroBar label="Carbs" :current="summary?.totalCarbs ?? 0" :target="summary?.targetCarbs ?? 250" color="#F97316" unit="g" />
            <MacroBar label="Fat" :current="summary?.totalFat ?? 0" :target="summary?.targetFat ?? 65" color="#EAB308" unit="g" />
          </div>
        </div>

        <!-- Today's meals -->
        <div class="lg:col-span-2 bg-white dark:bg-gray-900 rounded-2xl shadow-sm p-6">
          <div class="flex items-center justify-between mb-4">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100">Today's Meals</h2>
            <RouterLink to="/meal-log" class="text-sm text-green-600 dark:text-green-400 font-medium hover:text-green-700 dark:hover:text-green-400">View All →</RouterLink>
          </div>

          <div v-if="isLoading" class="flex justify-center py-8">
            <LoadingSpinner />
          </div>

          <div v-else-if="mealStore.dailyLogs.length === 0" class="text-center py-8">
            <p class="text-4xl mb-3">🍽️</p>
            <p class="text-gray-500 dark:text-gray-400 text-sm">No meals logged today</p>
            <RouterLink to="/meal-log" class="inline-block mt-3 bg-green-600 text-white text-sm font-semibold px-4 py-2 rounded-lg hover:bg-green-700">
              Log Your First Meal
            </RouterLink>
          </div>

          <div v-else class="space-y-3">
            <div v-for="log in mealStore.dailyLogs" :key="log.id"
              class="flex items-center justify-between p-3 rounded-xl bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
              <div>
                <p class="font-medium text-gray-900 dark:text-gray-100 text-sm">{{ MEAL_TYPE_LABELS[log.mealType] }}</p>
                <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ log.items.length }} item{{ log.items.length !== 1 ? 's' : '' }}</p>
              </div>
              <span class="text-sm font-semibold text-gray-700 dark:text-gray-200">
                {{ Math.round(log.totalNutrition.calories) }} kcal
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- AI Tip -->
      <AppAlert
        v-if="aiTip"
        variant="tip"
        title="AI Nutrition Tip"
        :message="aiTip"
        dismissible
        class="mb-6"
        @dismiss="aiTip = ''"
      />

      <!-- Quick actions -->
      <div class="grid grid-cols-3 gap-4">
        <RouterLink to="/meal-log"
          class="bg-white dark:bg-gray-900 rounded-2xl shadow-sm p-5 flex flex-col items-center gap-2 hover:shadow-md transition-shadow group">
          <div class="w-12 h-12 bg-green-100 dark:bg-green-900/40 rounded-xl flex items-center justify-center group-hover:bg-green-200 dark:group-hover:bg-green-900/60 transition-colors">
            <span class="text-2xl">➕</span>
          </div>
          <span class="text-sm font-semibold text-gray-700 dark:text-gray-200">Log Meal</span>
        </RouterLink>

        <RouterLink to="/meal-plan"
          class="bg-white dark:bg-gray-900 rounded-2xl shadow-sm p-5 flex flex-col items-center gap-2 hover:shadow-md transition-shadow group">
          <div class="w-12 h-12 bg-orange-100 dark:bg-orange-900/40 rounded-xl flex items-center justify-center group-hover:bg-orange-200 dark:group-hover:bg-orange-900/60 transition-colors">
            <span class="text-2xl">📅</span>
          </div>
          <span class="text-sm font-semibold text-gray-700 dark:text-gray-200">Meal Plan</span>
        </RouterLink>

        <RouterLink to="/recipes"
          class="bg-white dark:bg-gray-900 rounded-2xl shadow-sm p-5 flex flex-col items-center gap-2 hover:shadow-md transition-shadow group">
          <div class="w-12 h-12 bg-blue-100 dark:bg-blue-900/40 rounded-xl flex items-center justify-center group-hover:bg-blue-200 dark:group-hover:bg-blue-900/60 transition-colors">
            <span class="text-2xl">📖</span>
          </div>
          <span class="text-sm font-semibold text-gray-700 dark:text-gray-200">Recipes</span>
        </RouterLink>
      </div>
    </div>
  </AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { format } from 'date-fns';
import AppLayout from '@/components/layout/AppLayout.vue';
import CalorieDonut from '@/components/nutrition/CalorieDonut.vue';
import MacroBar from '@/components/nutrition/MacroBar.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import AppAlert from '@/components/ui/AppAlert.vue';
import { useAuthStore } from '@/stores/auth';
import { useMealStore } from '@/stores/meal';
import { MealType } from '@foodeez/shared';

const router = useRouter();
const authStore = useAuthStore();
const mealStore = useMealStore();

const isLoading = ref(false);
const aiTip = ref('Consider adding more leafy greens to boost your iron and vitamin K intake.');
const recipeSearch = ref('');

function goToRecipes() {
  const q = recipeSearch.value.trim();
  router.push({ path: '/search', query: q ? { q } : undefined });
  recipeSearch.value = '';
}

const today = new Date();
const todayDisplay = format(today, 'EEEE, MMMM d, yyyy');
const todayStr = format(today, 'yyyy-MM-dd');

const hour = today.getHours();
const greeting = computed(() =>
  hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening'
);

const summary = computed(() => mealStore.nutritionSummary);

const MEAL_TYPE_LABELS: Record<MealType, string> = {
  [MealType.Breakfast]: 'Breakfast',
  [MealType.MorningSnack]: 'Morning Snack',
  [MealType.Lunch]: 'Lunch',
  [MealType.AfternoonSnack]: 'Afternoon Snack',
  [MealType.Dinner]: 'Dinner',
  [MealType.EveningSnack]: 'Evening Snack',
};

onMounted(async () => {
  if (!authStore.user?.id) return;
  isLoading.value = true;
  try {
    await Promise.all([
      mealStore.fetchDailyLogs(authStore.user.id, todayStr),
      mealStore.fetchNutritionSummary(authStore.user.id, todayStr),
    ]);
  } finally {
    isLoading.value = false;
  }
});
</script>
