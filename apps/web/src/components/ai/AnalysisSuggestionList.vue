<script setup lang="ts">
defineProps<{
  /** The heading above the list. "Suggestions", "What to have next" - the caller's wording. */
  label: string;
  items: string[];
}>();
</script>

<template>
  <div v-if="items.length > 0">
    <p class="mb-1.5 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
      {{ label }}
    </p>
    <ul class="space-y-1">
      <li
        v-for="item in items"
        :key="item"
        class="flex items-start gap-1.5 text-sm text-gray-700 dark:text-gray-200"
      >
        <span class="mt-0.5 shrink-0 text-green-500" aria-hidden="true">&gt;</span>{{ item }}
      </li>
    </ul>
  </div>
</template>
