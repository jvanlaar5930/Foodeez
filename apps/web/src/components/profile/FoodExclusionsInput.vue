<script setup lang="ts">
import { computed, ref } from 'vue';

const props = defineProps<{
  modelValue: string[];
  /** Hidden once the list has entries, so it does not crowd a filled-in field. */
  showCommon?: boolean;
}>();

const emit = defineEmits<{ 'update:modelValue': [value: string[]] }>();

/** The allergens worth one tap. Everything else is typed. */
const COMMON = ['Peanuts', 'Tree nuts', 'Dairy', 'Eggs', 'Shellfish', 'Fish', 'Soy', 'Gluten', 'Sesame'];

const draft = ref('');

const suggestions = computed(() =>
  COMMON.filter((food) => !isListed(food)),
);

function isListed(food: string): boolean {
  const needle = food.trim().toLowerCase();
  return props.modelValue.some((existing) => existing.toLowerCase() === needle);
}

function add(food: string) {
  const value = food.trim();
  if (value.length === 0 || isListed(value)) {
    draft.value = '';
    return;
  }

  emit('update:modelValue', [...props.modelValue, value]);
  draft.value = '';
}

function remove(index: number) {
  emit('update:modelValue', props.modelValue.filter((_, i) => i !== index));
}

/** Enter or comma commits; backspace on an empty box takes back the last one. */
function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' || event.key === ',') {
    event.preventDefault();
    add(draft.value);
    return;
  }

  if (event.key === 'Backspace' && draft.value.length === 0 && props.modelValue.length > 0) {
    remove(props.modelValue.length - 1);
  }
}
</script>

<template>
  <div>
    <div
      v-if="modelValue.length > 0"
      class="mb-2 flex flex-wrap gap-1.5"
    >
      <span
        v-for="(food, index) in modelValue"
        :key="food"
        class="inline-flex items-center gap-1.5 rounded-full bg-red-100 px-2.5 py-1 text-sm font-medium text-red-700 dark:bg-red-900/40 dark:text-red-300"
      >
        {{ food }}
        <button
          type="button"
          class="text-red-400 transition-colors hover:text-red-600 dark:hover:text-red-200"
          :aria-label="`Remove ${food}`"
          @click="remove(index)"
        >
          &times;
        </button>
      </span>
    </div>

    <div class="flex gap-2">
      <input
        v-model="draft"
        type="text"
        placeholder="e.g. shellfish, cilantro, blue cheese"
        class="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
        @keydown="onKeydown"
        @blur="add(draft)"
      />
      <button
        type="button"
        class="shrink-0 rounded-lg border-2 border-gray-200 px-4 text-sm font-semibold text-gray-600 transition-colors hover:border-gray-300 dark:border-gray-700 dark:text-gray-300 dark:hover:border-gray-600"
        @click="add(draft)"
      >
        Add
      </button>
    </div>

    <div v-if="showCommon !== false && suggestions.length > 0" class="mt-2 flex flex-wrap gap-1.5">
      <button
        v-for="food in suggestions"
        :key="food"
        type="button"
        class="rounded-full border border-gray-200 px-2.5 py-1 text-xs font-medium text-gray-500 transition-colors hover:border-red-300 hover:text-red-600 dark:border-gray-700 dark:text-gray-400 dark:hover:border-red-800 dark:hover:text-red-400"
        @click="add(food)"
      >
        + {{ food }}
      </button>
    </div>
  </div>
</template>
