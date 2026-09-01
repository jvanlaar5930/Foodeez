<script setup lang="ts">
interface Props {
  title: string;
  description?: string;
  actionLabel?: string;
  emoji?: string;
}

withDefaults(defineProps<Props>(), {
  emoji: '🍽️',
});

const emit = defineEmits<{
  action: [];
}>();
</script>

<template>
  <div class="flex flex-col items-center justify-center py-16 px-4 text-center">
    <div class="mb-4">
      <slot name="icon">
        <span class="text-5xl">{{ emoji }}</span>
      </slot>
    </div>
    <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-1">{{ title }}</h3>
    <p v-if="description" class="text-sm text-gray-500 dark:text-gray-400 max-w-sm mb-6">{{ description }}</p>
    <button
      v-if="actionLabel"
      class="inline-flex items-center gap-2 px-4 py-2 bg-green-600 text-white text-sm font-medium rounded-lg hover:bg-green-700 transition-colors"
      @click="emit('action')"
    >
      {{ actionLabel }}
    </button>
  </div>
</template>
