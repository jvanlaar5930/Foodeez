<script setup lang="ts">
interface Props {
  title: string;
  /** One sentence saying what to do about it. The `description` slot wins over this. */
  description?: string;
  /** Renders the built-in button. For anything else - a link, two buttons - use the slot. */
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
  <div class="flex flex-col items-center justify-center px-4 py-16 text-center">
    <div class="mb-4">
      <slot name="icon">
        <span class="text-5xl" aria-hidden="true">{{ emoji }}</span>
      </slot>
    </div>

    <h3 class="mb-1 text-lg font-semibold text-gray-900 dark:text-gray-100">{{ title }}</h3>

    <!-- A slot as well as a prop: several of these say something different depending on why
         the list is empty, which a single string cannot express. -->
    <p
      v-if="description || $slots.description"
      class="mb-6 max-w-sm text-sm text-gray-500 dark:text-gray-400"
    >
      <slot name="description">{{ description }}</slot>
    </p>

    <slot name="actions">
      <button
        v-if="actionLabel"
        type="button"
        class="inline-flex items-center gap-2 rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-green-700"
        @click="emit('action')"
      >
        {{ actionLabel }}
      </button>
    </slot>
  </div>
</template>
