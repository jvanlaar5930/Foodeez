<script setup lang="ts">
defineOptions({ inheritAttrs: false });

interface Props {
  modelValue?: string | number;
  label?: string;
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

import { useAttrs, computed } from 'vue';
const attrs = useAttrs();
const inputId = computed(() => props.id ?? `input-${Math.random().toString(36).slice(2)}`);
</script>

<template>
  <div class="flex flex-col gap-1">
    <label v-if="label" :for="inputId" class="text-sm font-medium text-gray-700">
      {{ label }}
    </label>
    <input
      v-bind="attrs"
      :id="inputId"
      :type="type"
      :value="modelValue"
      :class="[
        'block w-full rounded-lg border px-3 py-2 text-sm text-gray-900 placeholder-gray-400 transition-colors focus:outline-none focus:ring-2',
        error
          ? 'border-red-400 focus:border-red-400 focus:ring-red-200'
          : 'border-gray-300 focus:border-green-500 focus:ring-green-200',
      ]"
      @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
    <p v-if="error" class="text-xs text-red-600">{{ error }}</p>
    <p v-else-if="hint" class="text-xs text-gray-500">{{ hint }}</p>
  </div>
</template>
