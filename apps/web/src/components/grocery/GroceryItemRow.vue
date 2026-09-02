<script setup lang="ts">
import { ref } from 'vue';
import { GROCERY_CATEGORIES, type GroceryItem, type GroceryItemRequest } from '@foodeez/shared';

const props = defineProps<{ item: GroceryItem }>();

const emit = defineEmits<{
  toggle: [itemId: string, isChecked: boolean];
  save: [itemId: string, item: GroceryItemRequest];
  remove: [itemId: string];
}>();

const isEditing = ref(false);
const name = ref(props.item.name);
const quantity = ref(props.item.quantity);
const category = ref(props.item.category);

function startEdit() {
  name.value = props.item.name;
  quantity.value = props.item.quantity;
  category.value = props.item.category;
  isEditing.value = true;
}

function save() {
  const trimmed = name.value.trim();
  if (trimmed.length === 0) {
    return;
  }

  emit('save', props.item.id, {
    name: trimmed,
    quantity: quantity.value.trim(),
    category: category.value,
    isChecked: props.item.isChecked,
  });

  isEditing.value = false;
}
</script>

<template>
  <li class="border-b border-gray-100 py-2 last:border-0 dark:border-gray-800">
    <div v-if="!isEditing" class="group flex items-start gap-3">
      <input
        type="checkbox"
        :checked="item.isChecked"
        :aria-label="`Got ${item.name}`"
        class="mt-0.5 h-5 w-5 shrink-0 cursor-pointer rounded border-gray-300 text-green-600 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800"
        @change="emit('toggle', item.id, ($event.target as HTMLInputElement).checked)"
      />

      <div class="min-w-0 flex-1">
        <p
          :class="[
            'text-sm font-medium',
            item.isChecked
              ? 'text-gray-400 line-through dark:text-gray-600'
              : 'text-gray-800 dark:text-gray-100',
          ]"
        >
          {{ item.name }}
          <span v-if="item.quantity" class="font-normal text-gray-500 dark:text-gray-400">
            &middot; {{ item.quantity }}
          </span>
        </p>
        <p v-if="item.source" class="truncate text-xs text-gray-400 dark:text-gray-500">
          {{ item.source }}
        </p>
      </div>

      <div class="flex shrink-0 items-center gap-2 opacity-0 transition group-hover:opacity-100 focus-within:opacity-100">
        <button
          type="button"
          class="text-xs font-semibold text-gray-400 hover:text-green-600"
          :aria-label="`Swap or edit ${item.name}`"
          @click="startEdit"
        >
          Swap
        </button>
        <button
          type="button"
          class="text-xs font-semibold text-gray-400 hover:text-red-600"
          :aria-label="`Remove ${item.name}`"
          @click="emit('remove', item.id)"
        >
          Remove
        </button>
      </div>
    </div>

    <!-- Editing is the swap: rename the line and adjust the amount. -->
    <div v-else class="space-y-2">
      <div class="flex flex-wrap gap-2">
        <input
          v-model="name"
          type="text"
          placeholder="Item"
          class="min-w-0 flex-1 rounded-lg border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
          @keydown.enter.prevent="save"
        />
        <input
          v-model="quantity"
          type="text"
          placeholder="Amount"
          class="w-28 rounded-lg border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
          @keydown.enter.prevent="save"
        />
        <select
          v-model="category"
          class="rounded-lg border border-gray-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
        >
          <option v-for="option in GROCERY_CATEGORIES" :key="option" :value="option">
            {{ option }}
          </option>
        </select>
      </div>

      <div class="flex gap-2">
        <button
          type="button"
          class="rounded-lg bg-green-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-green-700"
          @click="save"
        >
          Save
        </button>
        <button
          type="button"
          class="rounded-lg border-2 border-gray-200 px-3 py-1.5 text-xs font-semibold text-gray-600 dark:border-gray-700 dark:text-gray-300"
          @click="isEditing = false"
        >
          Cancel
        </button>
      </div>
    </div>
  </li>
</template>
