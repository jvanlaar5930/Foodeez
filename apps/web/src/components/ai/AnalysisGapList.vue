<script setup lang="ts">
defineProps<{
  /** The heading above the pills. "Missing", "Still short on" - the wording is the caller's. */
  label: string;
  items: string[];
}>();
</script>

<template>
  <div v-if="items.length > 0">
    <p class="mb-1.5 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
      {{ label }}
    </p>
    <div class="flex flex-wrap gap-1.5">
      <span
        v-for="item in items"
        :key="item"
        class="rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-700 dark:bg-red-900/40 dark:text-red-300"
      >
        {{ item }}
      </span>
    </div>
  </div>
</template>
