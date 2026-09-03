<script setup lang="ts">
import { ref } from 'vue';
import { GROCERY_CATEGORIES, type GroceryItemRequest } from '@foodeez/shared';

const emit = defineEmits<{ add: [item: GroceryItemRequest] }>();

const name = ref('');
const quantity = ref('');
const category = ref<string>('Other');

function add() {
  const trimmed = name.value.trim();
  if (trimmed.length === 0) {
    return;
  }

  emit('add', { name: trimmed, quantity: quantity.value.trim(), category: category.value });

  // Category stays put: someone adding three things from the same aisle should not have to
  // pick it three times.
  name.value = '';
  quantity.value = '';
}
</script>

<template>
  <div class="flex flex-wrap gap-2">
    <input
      v-model="name"
      type="text"
      placeholder="Add something else..."
      class="min-w-0 flex-1 rounded-xl border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
      @keydown.enter.prevent="add"
    />
    <input
      v-model="quantity"
      type="text"
      placeholder="Amount"
      class="w-24 rounded-xl border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
      @keydown.enter.prevent="add"
    />
    <select
      v-model="category"
      class="rounded-xl border border-gray-300 px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
    >
      <option v-for="option in GROCERY_CATEGORIES" :key="option" :value="option">
        {{ option }}
      </option>
    </select>
    <button
      type="button"
      :disabled="name.trim().length === 0"
      class="shrink-0 rounded-xl bg-green-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-green-700 disabled:opacity-40"
      @click="add"
    >
      Add
    </button>
  </div>
</template>
