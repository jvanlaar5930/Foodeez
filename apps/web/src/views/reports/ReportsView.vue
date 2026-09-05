<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { Bar, Doughnut, Line } from 'vue-chartjs';
import {
  ArcElement,
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Filler,
  Legend,
  LinearScale,
  LineElement,
  PointElement,
  Tooltip,
  type ChartData,
  type ChartOptions,
} from 'chart.js';
import { MEAL_TYPE_LABELS, type NutritionReport } from '@foodeez/shared';
import AppLayout from '@/components/layout/AppLayout.vue';
import AppCard from '@/components/ui/AppCard.vue';
import AppAlert from '@/components/ui/AppAlert.vue';
import EmptyState from '@/components/ui/EmptyState.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import { mealService } from '@/services/mealService';
import { useThemeStore } from '@/stores/theme';
import { MACRO_COLORS } from '@/utils/macroColors';
import { addDays, formatDay, formatRange, toISODate } from '@/utils/dateRange';

ChartJS.register(
  ArcElement,
  BarElement,
  CategoryScale,
  Filler,
  Legend,
  LinearScale,
  LineElement,
  PointElement,
  Tooltip,
);

/** The spans worth one click. Anything longer stops being readable as a daily chart. */
const RANGES = [
  { days: 7, label: '7 days' },
  { days: 30, label: '30 days' },
  { days: 90, label: '90 days' },
] as const;

const themeStore = useThemeStore();

const rangeDays = ref<number>(30);
const report = ref<NutritionReport | null>(null);
const isLoading = ref(false);
const error = ref<string | null>(null);

/** The range ends today, so the last point on every chart is the day being lived. */
const endDate = computed(() => toISODate(new Date()));
const startDate = computed(() => toISODate(addDays(new Date(), -(rangeDays.value - 1))));

async function load() {
  isLoading.value = true;
  error.value = null;

  try {
    report.value = await mealService.getReport(startDate.value, endDate.value);
  } catch {
    error.value = 'We could not build your report. Please try again in a moment.';
    report.value = null;
  } finally {
    isLoading.value = false;
  }
}

onMounted(load);
watch(rangeDays, load);

/** Days with meals in them. Everything below is drawn from these, not from empty days. */
const loggedDays = computed(() => report.value?.days.filter((day) => day.hasLogs) ?? []);
const hasData = computed(() => loggedDays.value.length > 0);

/**
 * Chart.js draws to a canvas and cannot resolve `var(--color-…)`, so the palette values used
 * everywhere else in the app are turned into real colours here. Recomputed with the theme so
 * the charts follow a switch between light and dark.
 */
function resolve(swatch: string): string {
  const name = /^var\((--[\w-]+)\)$/.exec(swatch)?.[1];
  if (!name) return swatch;

  return getComputedStyle(document.documentElement).getPropertyValue(name).trim() || swatch;
}

function token(name: string): string {
  return resolve(`var(--color-${name})`);
}

const palette = computed(() => {
  // Read so the computed re-runs on a theme switch; the tokens themselves resolve differently
  // once the `dark` class is on the document.
  const dark = themeStore.isDark;

  return {
    calories: resolve(MACRO_COLORS.calories.swatch),
    protein: resolve(MACRO_COLORS.protein.swatch),
    carbs: resolve(MACRO_COLORS.carbs.swatch),
    fat: resolve(MACRO_COLORS.fat.swatch),
    target: token(dark ? 'gray-500' : 'gray-400'),
    grid: token(dark ? 'gray-800' : 'gray-200'),
    tick: token(dark ? 'gray-400' : 'gray-500'),
    mealTypes: [
      token('green-500'),
      token('teal-400'),
      token('blue-500'),
      token('violet-400'),
      token('orange-500'),
      token('amber-400'),
    ],
  };
});

/** Grid, ticks and legend styling shared by the two axis charts. */
const axisOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index' as const, intersect: false },
  plugins: {
    legend: {
      labels: { color: palette.value.tick, boxWidth: 12, usePointStyle: true },
    },
  },
  scales: {
    x: {
      grid: { display: false },
      ticks: { color: palette.value.tick, maxRotation: 0, autoSkipPadding: 16 },
    },
    y: {
      beginAtZero: true,
      grid: { color: palette.value.grid },
      ticks: { color: palette.value.tick },
    },
  },
}));

const dayLabels = computed(() => (report.value?.days ?? []).map((day) => formatDay(day.date)));

/**
 * Calories per day against the target. A day with nothing logged is null rather than zero:
 * the line breaks over it instead of diving to the floor, which would read as a fast.
 */
const caloriesChart = computed<ChartData<'line'>>(() => {
  const days = report.value?.days ?? [];
  const target = report.value?.targetCalories ?? 0;

  return {
    labels: dayLabels.value,
    datasets: [
      {
        label: 'Calories',
        data: days.map((day) => (day.hasLogs ? day.calories : null)),
        borderColor: palette.value.calories,
        backgroundColor: palette.value.calories + '22',
        pointRadius: 2,
        pointHoverRadius: 5,
        borderWidth: 2,
        tension: 0.3,
        fill: true,
        spanGaps: false,
      },
      ...(target > 0
        ? [
            {
              label: 'Target',
              data: days.map(() => target),
              borderColor: palette.value.target,
              borderDash: [5, 4],
              borderWidth: 1.5,
              pointRadius: 0,
              fill: false,
            },
          ]
        : []),
    ],
  };
});

const caloriesOptions = computed<ChartOptions<'line'>>(() => ({
  ...axisOptions.value,
  plugins: {
    ...axisOptions.value.plugins,
    tooltip: {
      callbacks: {
        label: (ctx) =>
          ctx.parsed.y === null ? ' Nothing logged' : ` ${ctx.dataset.label}: ${Math.round(ctx.parsed.y)} kcal`,
      },
    },
  },
}));

/** Grams of each macro per day, stacked so the bar height is the day's total intake. */
const macrosChart = computed<ChartData<'bar'>>(() => {
  const days = report.value?.days ?? [];

  return {
    labels: dayLabels.value,
    datasets: [
      {
        label: 'Protein',
        data: days.map((day) => (day.hasLogs ? day.protein : null)),
        backgroundColor: palette.value.protein,
      },
      {
        label: 'Carbs',
        data: days.map((day) => (day.hasLogs ? day.carbs : null)),
        backgroundColor: palette.value.carbs,
      },
      {
        label: 'Fat',
        data: days.map((day) => (day.hasLogs ? day.fat : null)),
        backgroundColor: palette.value.fat,
      },
    ],
  };
});

const macrosOptions = computed<ChartOptions<'bar'>>(() => ({
  ...axisOptions.value,
  scales: {
    x: { ...axisOptions.value.scales.x, stacked: true },
    y: { ...axisOptions.value.scales.y, stacked: true },
  },
  plugins: {
    ...axisOptions.value.plugins,
    tooltip: {
      callbacks: {
        label: (ctx) =>
          ctx.parsed.y === null ? ' Nothing logged' : ` ${ctx.dataset.label}: ${Math.round(ctx.parsed.y)} g`,
      },
    },
  },
}));

/**
 * The average day's macro split, by the calories each macro contributes rather than by its
 * weight - 30 g of fat and 30 g of carbohydrate are not the same share of a day.
 */
const CALORIES_PER_GRAM = { protein: 4, carbs: 4, fat: 9 } as const;

const splitChart = computed<ChartData<'doughnut'>>(() => {
  const averages = report.value?.averages;

  return {
    labels: ['Protein', 'Carbs', 'Fat'],
    datasets: [
      {
        data: [
          Math.round((averages?.protein ?? 0) * CALORIES_PER_GRAM.protein),
          Math.round((averages?.carbs ?? 0) * CALORIES_PER_GRAM.carbs),
          Math.round((averages?.fat ?? 0) * CALORIES_PER_GRAM.fat),
        ],
        backgroundColor: [palette.value.protein, palette.value.carbs, palette.value.fat],
        borderWidth: 0,
        hoverOffset: 4,
      },
    ],
  };
});

const mealTypeChart = computed<ChartData<'doughnut'>>(() => {
  const byMealType = report.value?.byMealType ?? [];

  return {
    labels: byMealType.map((entry) => MEAL_TYPE_LABELS[entry.mealType]),
    datasets: [
      {
        data: byMealType.map((entry) => Math.round(entry.calories)),
        backgroundColor: byMealType.map(
          (_, index) => palette.value.mealTypes[index % palette.value.mealTypes.length],
        ),
        borderWidth: 0,
        hoverOffset: 4,
      },
    ],
  };
});

const doughnutOptions = computed<ChartOptions<'doughnut'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: '62%',
  plugins: {
    legend: {
      position: 'bottom',
      labels: { color: palette.value.tick, boxWidth: 12, usePointStyle: true, padding: 12 },
    },
    tooltip: { callbacks: { label: (ctx) => ` ${ctx.label}: ${ctx.parsed} kcal` } },
  },
}));

/** The four numbers worth reading before any chart. */
const stats = computed(() => {
  const value = report.value;
  if (!value) return [];

  return [
    {
      label: 'Average calories',
      value: Math.round(value.averages.calories).toLocaleString(),
      unit: 'kcal / logged day',
      hint: value.targetCalories > 0 ? `Target ${value.targetCalories.toLocaleString()}` : '',
    },
    {
      label: 'Average protein',
      value: Math.round(value.averages.protein).toLocaleString(),
      unit: 'g / logged day',
      hint: value.targetProtein > 0 ? `Target ${Math.round(value.targetProtein)} g` : '',
    },
    {
      label: 'Days logged',
      value: `${value.daysLogged}`,
      unit: `of ${value.daysInRange}`,
      hint: `${value.totalMeals} meals`,
    },
    {
      label: 'Days on target',
      value: `${value.daysOnTarget}`,
      unit: `of ${value.daysLogged} logged`,
      hint: 'Within 10% of your calorie goal',
    },
  ];
});
</script>

<template>
  <AppLayout>
    <div class="mx-auto max-w-6xl p-6">
      <div class="mb-6 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Reporting</h1>
          <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
            {{ formatRange(startDate, endDate) }}
          </p>
        </div>

        <div class="flex gap-2">
          <button
            v-for="range in RANGES"
            :key="range.days"
            type="button"
            :class="[
              'rounded-full border px-3 py-1.5 text-sm font-medium transition-colors',
              rangeDays === range.days
                ? 'border-green-600 bg-green-600 text-white'
                : 'border-gray-300 text-gray-600 hover:border-green-400 dark:border-gray-700 dark:text-gray-400 dark:hover:border-green-600',
            ]"
            @click="rangeDays = range.days"
          >
            {{ range.label }}
          </button>
        </div>
      </div>

      <AppAlert v-if="error" variant="error" :message="error" class="mb-6" />

      <div v-if="isLoading" class="flex justify-center py-16">
        <LoadingSpinner size="lg" />
      </div>

      <EmptyState
        v-else-if="report && !hasData"
        emoji="📊"
        title="Nothing to report yet"
        description="Log a few meals and this page will fill in with how your days compare against your targets."
      />

      <template v-else-if="report">
        <!-- Headline numbers -->
        <div class="mb-6 grid grid-cols-2 gap-4 lg:grid-cols-4">
          <AppCard v-for="stat in stats" :key="stat.label" padding="sm">
            <p class="text-xs font-medium text-gray-500 dark:text-gray-400">{{ stat.label }}</p>
            <p class="mt-1 text-2xl font-bold text-gray-900 dark:text-gray-100">{{ stat.value }}</p>
            <p class="text-xs text-gray-500 dark:text-gray-400">{{ stat.unit }}</p>
            <p v-if="stat.hint" class="mt-1 text-xs text-gray-400 dark:text-gray-500">{{ stat.hint }}</p>
          </AppCard>
        </div>

        <!-- Calories per day -->
        <AppCard class="mb-6">
          <h2 class="mb-1 text-lg font-semibold text-gray-900 dark:text-gray-100">Calories per day</h2>
          <p class="mb-4 text-xs text-gray-500 dark:text-gray-400">
            Days with nothing logged are left as gaps rather than drawn as zero.
          </p>
          <div class="h-72">
            <Line :data="caloriesChart" :options="caloriesOptions" />
          </div>
        </AppCard>

        <!-- Macros per day -->
        <AppCard class="mb-6">
          <h2 class="mb-1 text-lg font-semibold text-gray-900 dark:text-gray-100">Macros per day</h2>
          <p class="mb-4 text-xs text-gray-500 dark:text-gray-400">Grams of protein, carbs and fat, stacked.</p>
          <div class="h-72">
            <Bar :data="macrosChart" :options="macrosOptions" />
          </div>
        </AppCard>

        <div class="mb-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
          <!-- Macro split -->
          <AppCard>
            <h2 class="mb-1 text-lg font-semibold text-gray-900 dark:text-gray-100">Average macro split</h2>
            <p class="mb-4 text-xs text-gray-500 dark:text-gray-400">
              By calories contributed, not by weight.
            </p>
            <div class="h-64">
              <Doughnut :data="splitChart" :options="doughnutOptions" />
            </div>
          </AppCard>

          <!-- Calories by meal -->
          <AppCard>
            <h2 class="mb-1 text-lg font-semibold text-gray-900 dark:text-gray-100">Calories by meal</h2>
            <p class="mb-4 text-xs text-gray-500 dark:text-gray-400">
              Where the calories landed across the day, over the whole range.
            </p>
            <div class="h-64">
              <Doughnut :data="mealTypeChart" :options="doughnutOptions" />
            </div>
          </AppCard>
        </div>

        <!-- Top foods -->
        <AppCard v-if="report.topFoods.length" padding="none">
          <div class="border-b border-gray-100 px-6 py-4 dark:border-gray-800">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100">Most logged foods</h2>
            <p class="text-xs text-gray-500 dark:text-gray-400">
              What you actually ate most often in this range.
            </p>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-left text-xs uppercase tracking-wide text-gray-400 dark:text-gray-500">
                  <th class="px-6 py-3 font-medium">Food</th>
                  <th class="px-6 py-3 text-right font-medium">Times logged</th>
                  <th class="px-6 py-3 text-right font-medium">Total calories</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="food in report.topFoods"
                  :key="food.name"
                  class="border-t border-gray-100 dark:border-gray-800"
                >
                  <td class="px-6 py-3 text-gray-900 dark:text-gray-100">{{ food.name }}</td>
                  <td class="px-6 py-3 text-right text-gray-600 dark:text-gray-400">{{ food.timesLogged }}</td>
                  <td class="px-6 py-3 text-right text-gray-600 dark:text-gray-400">
                    {{ Math.round(food.calories).toLocaleString() }} kcal
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </AppCard>
      </template>
    </div>
  </AppLayout>
</template>
