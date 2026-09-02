<script setup lang="ts">
defineProps<{
  modelValue: boolean;
  /** Describes what is being switched, for anyone not looking at the label beside it. */
  label?: string;
  disabled?: boolean;
}>();

const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>();
</script>

<template>
  <!--
    The knob is a flex child, not an absolutely positioned one. A button centres its content
    box, so an `absolute` knob with no `left` starts from the middle rather than the left
    edge and the "on" state pushes it clean off the track.
  -->
  <button
    type="button"
    role="switch"
    :aria-checked="modelValue"
    :aria-label="label"
    :disabled="disabled"
    class="relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors focus-visible:ring-2 focus-visible:ring-green-500 focus-visible:ring-offset-2 disabled:opacity-50 dark:focus-visible:ring-offset-gray-900"
    :class="modelValue ? 'bg-green-500' : 'bg-gray-300 dark:bg-gray-600'"
    @click="emit('update:modelValue', !modelValue)"
  >
    <!-- White in both themes: a dark knob disappears against the dark track when off. -->
    <span
      class="inline-block h-5 w-5 rounded-full bg-white shadow transition-transform duration-200"
      :class="modelValue ? 'translate-x-[22px]' : 'translate-x-0.5'"
    />
  </button>
</template>
