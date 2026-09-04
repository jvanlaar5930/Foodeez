<script setup lang="ts">
import { computed, useAttrs, useId } from 'vue';

defineOptions({ inheritAttrs: false });

interface Props {
  modelValue?: string | number;
  label?: string;
  /** The reason this field is wrong, shown under it and announced to a screen reader. */
  error?: string;
  hint?: string;
  type?: string;
  id?: string;
}

const props = withDefaults(defineProps<Props>(), {
  type: 'text',
});

const emit = defineEmits<{
  'update:modelValue': [value: string];
}>();

const attrs = useAttrs();

/**
 * A stable id, so the label's `for` keeps pointing at this input across re-renders. The
 * previous `Math.random()` produced a new one on every render, which quietly broke the
 * association it exists to make.
 */
const generatedId = useId();
const inputId = computed(() => props.id ?? generatedId);
const errorId = computed(() => `${inputId.value}-error`);
</script>

<template>
  <div class="flex flex-col gap-1">
    <label v-if="label" :for="inputId" class="text-sm font-medium text-gray-700 dark:text-gray-200">
      {{ label }}
    </label>

    <div class="relative">
      <input
        v-bind="attrs"
        :id="inputId"
        :type="type"
        :value="modelValue"
        :aria-invalid="error ? 'true' : undefined"
        :aria-describedby="error ? errorId : undefined"
        :class="[
          'block w-full rounded-lg border px-3 py-2.5 text-sm text-gray-900 transition-colors placeholder-gray-400 focus:outline-none focus:ring-2 dark:text-gray-100 dark:placeholder-gray-500',
          error
            ? 'border-red-400 focus:border-red-400 focus:ring-red-200'
            : 'border-gray-300 focus:border-green-500 focus:ring-green-500 dark:border-gray-600',
          $slots.suffix ? 'pr-10' : '',
        ]"
        @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
      />

      <!-- A control inside the field, at its trailing edge - the show-password eye. -->
      <div v-if="$slots.suffix" class="absolute right-3 top-2.5">
        <slot name="suffix" />
      </div>
    </div>

    <slot name="below" />

    <p v-if="error" :id="errorId" class="text-xs text-red-500 dark:text-red-400">{{ error }}</p>
    <p v-else-if="hint" class="text-xs text-gray-500 dark:text-gray-400">{{ hint }}</p>
  </div>
</template>
