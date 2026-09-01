<script setup lang="ts">
import { computed } from 'vue';
import { Doughnut } from 'vue-chartjs';
import { useThemeStore } from '@/stores/theme';
import {
  Chart as ChartJS,
  ArcElement,
  Tooltip,
  Legend,
  type ChartData,
  type ChartOptions,
} from 'chart.js';

ChartJS.register(ArcElement, Tooltip, Legend);

interface Props {
  consumed: number;
  target: number;
}

const props = withDefaults(defineProps<Props>(), {
  consumed: 0,
  target: 2000,
});

// Canvas colours can't come from CSS classes, so the theme has to be read directly.
const themeStore = useThemeStore();

const percentage = computed(() => {
  if (props.target <= 0) return 0;
  return Math.min((props.consumed / props.target) * 100, 150);
});

// The arc is canvas, but this same colour labels the percentage text on the card, so the
// dark variants are the lighter 400 steps that clear AA against the dark surface.
const fillColor = computed(() => {
  const pct = percentage.value;
  const dark = themeStore.isDark;
  if (pct > 100) return dark ? '#f87171' : '#ef4444'; // red — over
  if (pct >= 80) return dark ? '#fbbf24' : '#f59e0b'; // amber — nearly there
  return dark ? '#4ade80' : '#16a34a'; // green — on track / under
});

/** The unfilled remainder of the ring: gray-200 on light, gray-700 on dark. */
const trackColor = computed(() => (themeStore.isDark ? '#374151' : '#e5e7eb'));

const chartData = computed<ChartData<'doughnut'>>(() => ({
  labels: ['Consumed', 'Remaining'],
  datasets: [
    {
      data: [
        Math.round(props.consumed),
        Math.max(0, Math.round(props.target - props.consumed)),
      ],
      backgroundColor: [fillColor.value, trackColor.value],
      borderWidth: 0,
      hoverOffset: 4,
    },
  ],
}));

const chartOptions = computed<ChartOptions<'doughnut'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: '72%',
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (ctx) => ` ${ctx.parsed} kcal`,
      },
    },
  },
}));
</script>

<template>
  <div class="relative flex items-center justify-center">
    <div class="w-48 h-48">
      <Doughnut :data="chartData" :options="chartOptions" />
    </div>
    <!-- Center overlay text -->
    <div class="absolute flex flex-col items-center pointer-events-none">
      <span class="text-2xl font-bold text-gray-900 dark:text-gray-100">{{ Math.round(consumed) }}</span>
      <span class="text-xs text-gray-500 dark:text-gray-400">/ {{ Math.round(target) }} kcal</span>
      <span :style="{ color: fillColor }" class="text-xs font-semibold mt-0.5">
        {{ Math.round(percentage) }}%
      </span>
    </div>
  </div>
</template>
