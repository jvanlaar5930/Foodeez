<script setup lang="ts">
interface Props {
  padding?: boolean;
}
withDefaults(defineProps<Props>(), { padding: true });
</script>

<template>
  <div :class="['bg-white dark:bg-gray-900 rounded-2xl shadow-sm border border-gray-100 dark:border-gray-800', padding ? 'p-6' : '']">
    <div v-if="$slots.header" class="mb-4 pb-4 border-b border-gray-100 dark:border-gray-800">
      <slot name="header" />
    </div>
    <slot />
    <div v-if="$slots.footer" class="mt-4 pt-4 border-t border-gray-100 dark:border-gray-800">
      <slot name="footer" />
    </div>
  </div>
</template>
