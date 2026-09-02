<script setup lang="ts">
import { nextTick, ref, watch } from 'vue';

const props = defineProps<{
  text: string;
  /** Shown until the first words arrive. */
  placeholder?: string;
}>();

const container = ref<HTMLElement | null>(null);

// Follow the writing the way a terminal does: once the text is taller than the box, the
// newest line is the one worth showing.
watch(
  () => props.text,
  async () => {
    await nextTick();
    if (container.value) {
      container.value.scrollTop = container.value.scrollHeight;
    }
  },
);
</script>

<template>
  <div
    ref="container"
    class="max-h-32 overflow-y-auto whitespace-pre-wrap text-sm leading-relaxed text-gray-700 dark:text-gray-200"
  >
    <template v-if="text">{{ text }}</template>
    <span v-else class="text-gray-400 dark:text-gray-500">{{ placeholder ?? 'Thinking...' }}</span>
    <span class="ml-0.5 inline-block h-3.5 w-1.5 animate-pulse bg-purple-500 align-middle" />
  </div>
</template>
