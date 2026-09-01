<script setup lang="ts">
import type { MealLogItem } from '@foodeez/shared';

interface Props {
  item: MealLogItem;
}

defineProps<Props>();
</script>

<template>
  <div class="flex items-center justify-between rounded-lg px-3 py-2.5 transition-colors hover:bg-gray-50 dark:hover:bg-gray-800">
    <div class="min-w-0 flex-1">
      <p class="truncate text-sm font-medium text-gray-900 dark:text-gray-100">{{ item.foodItem.name }}</p>
      <p class="text-xs text-gray-500 dark:text-gray-400">
        <span v-if="item.foodItem.brand">{{ item.foodItem.brand }} · </span>
        {{ item.quantity }} {{ item.unit }}
      </p>
    </div>
    <div class="ml-3 shrink-0">
      <span class="text-sm font-semibold text-gray-700 dark:text-gray-200">
        {{ Math.round(item.nutritionalInfo.calories) }} kcal
      </span>
    </div>
  </div>
</template>
