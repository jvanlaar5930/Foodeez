<script setup lang="ts">
import type { MacroTotal } from '@/composables/useMealItems';

defineProps<{ totals: MacroTotal[] }>();
</script>

<template>
  <div class="grid grid-cols-4 gap-2 text-center">
    <div v-for="macro in totals" :key="macro.label">
      <p class="text-xl font-bold" :class="macro.color">
        {{ macro.value }}<span class="ml-0.5 text-xs font-normal text-gray-400">{{ macro.unit }}</span>
      </p>
      <p class="mt-0.5 text-xs text-gray-400">{{ macro.label }}</p>
    </div>
  </div>
</template>
