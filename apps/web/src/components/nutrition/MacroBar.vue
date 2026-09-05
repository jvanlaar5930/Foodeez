<script setup lang="ts">
import { OVER_TARGET_COLOR } from '@/utils/macroColors';
import { computed } from 'vue';

interface Props {
  label: string;
  current: number;
  target: number;
  color: string;
  unit?: string;
}

const props = withDefaults(defineProps<Props>(), {
  unit: 'g',
});

const percentage = computed(() => {
  if (props.target <= 0) return 0;
  return Math.min((props.current / props.target) * 100, 100);
});

const isOver = computed(() => props.current > props.target);
</script>

<template>
  <div class="space-y-1">
    <div class="flex justify-between items-baseline">
      <span class="text-xs font-medium text-gray-600 dark:text-gray-300">{{ label }}</span>
      <span class="text-xs text-gray-500 dark:text-gray-400">
        <span :class="isOver ? 'text-red-500 dark:text-red-400 font-semibold' : 'text-gray-900 dark:text-gray-100'">
          {{ Math.round(current) }}
        </span>
        / {{ Math.round(target) }} {{ unit }}
      </span>
    </div>
    <div class="w-full bg-gray-100 dark:bg-gray-800 rounded-full h-2 overflow-hidden">
      <div
        :style="{ width: `${percentage}%`, backgroundColor: isOver ? OVER_TARGET_COLOR : color }"
        class="h-2 rounded-full transition-all duration-500"
      />
    </div>
    <div v-if="isOver" class="text-xs text-red-500 dark:text-red-400">
      +{{ Math.round(current - target) }}{{ unit }} over target
    </div>
  </div>
</template>
