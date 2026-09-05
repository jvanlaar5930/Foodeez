<script setup lang="ts">
import { RouterLink } from 'vue-router';

interface Props {
  to: string;
  emoji: string;
  label: string;
  /** The tile's accent, one of the palette's colour names. */
  accent: 'green' | 'orange' | 'blue';
}

defineProps<Props>();

/**
 * Spelled out rather than interpolated: Tailwind scans source text for class names, so a
 * class built at runtime from `bg-${accent}-100` is never generated in the first place.
 */
const ACCENTS: Record<string, string> = {
  green:
    'bg-green-100 group-hover:bg-green-200 dark:bg-green-900/40 dark:group-hover:bg-green-900/60',
  orange:
    'bg-orange-100 group-hover:bg-orange-200 dark:bg-orange-900/40 dark:group-hover:bg-orange-900/60',
  blue: 'bg-blue-100 group-hover:bg-blue-200 dark:bg-blue-900/40 dark:group-hover:bg-blue-900/60',
};
</script>

<template>
  <RouterLink
    :to="to"
    class="group flex flex-col items-center gap-2 rounded-2xl bg-white p-5 shadow-sm transition-shadow hover:shadow-md dark:bg-gray-900"
  >
    <div
      class="flex h-12 w-12 items-center justify-center rounded-xl transition-colors"
      :class="ACCENTS[accent]"
    >
      <span class="text-2xl" aria-hidden="true">{{ emoji }}</span>
    </div>
    <span class="text-sm font-semibold text-gray-700 dark:text-gray-200">{{ label }}</span>
  </RouterLink>
</template>
