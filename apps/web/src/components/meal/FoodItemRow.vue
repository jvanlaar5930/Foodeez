<script setup lang="ts">
import type { MealLogItem } from '@foodeez/shared';

interface Props {
  item: MealLogItem;
}

defineProps<Props>();
const emit = defineEmits<{ delete: [id: string] }>();
</script>

<template>
  <div
    class="flex items-center justify-between py-2.5 px-3 rounded-lg hover:bg-gray-50 transition-colors group"
  >
    <div class="flex-1 min-w-0">
      <p class="text-sm font-medium text-gray-900 truncate">{{ item.foodItem.name }}</p>
      <p class="text-xs text-gray-500">
        <span v-if="item.foodItem.brand">{{ item.foodItem.brand }} · </span>
        {{ item.quantity }} {{ item.unit }}
      </p>
    </div>
    <div class="flex items-center gap-3 ml-3 shrink-0">
      <span class="text-sm font-semibold text-gray-700">
        {{ Math.round(item.nutritionalInfo.calories) }} kcal
      </span>
      <button
        class="opacity-0 group-hover:opacity-100 p-1 rounded-md text-gray-400 hover:text-red-500 hover:bg-red-50 transition-all"
        title="Remove item"
        @click="emit('delete', item.id)"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
        </svg>
      </button>
    </div>
  </div>
</template>
