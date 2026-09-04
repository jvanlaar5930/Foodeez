<script setup lang="ts">
import { ref } from 'vue';
import { mealTemplateService } from '@/services/mealTemplateService';
import { useAuthStore } from '@/stores/auth';
import type { MealItem } from '@/composables/useMealItems';
import type { MealType } from '@foodeez/shared';

const props = defineProps<{
  items: MealItem[];
  mealType: MealType;
}>();

/** Told so the quick-add panel above can pick the new saved meal up. */
const emit = defineEmits<{ saved: [] }>();

const authStore = useAuthStore();

/** The name being typed, or null while the field is not open. */
const name = ref<string | null>(null);
const isSaving = ref(false);
const error = ref<string | null>(null);
const savedName = ref<string | null>(null);

async function save(): Promise<void> {
  const trimmed = name.value?.trim();
  if (!trimmed || !authStore.user?.id || props.items.length === 0) {
    return;
  }

  isSaving.value = true;
  error.value = null;

  try {
    const saved = await mealTemplateService.save({
      userId: authStore.user.id,
      name: trimmed,
      mealType: props.mealType,
      items: props.items.map((entry) => ({
        foodItemId: entry.item.id,
        quantity: entry.amount,
        unit: entry.item.servingUnit,
      })),
    });

    savedName.value = saved.name;
    name.value = null;
    emit('saved');
  } catch {
    // The likely cause is a name already in use, which is the one thing the reader can fix.
    error.value = 'That could not be saved. Try a different name.';
  } finally {
    isSaving.value = false;
  }
}

/** Called when the dialog is reopened on a different meal. */
function reset(): void {
  name.value = null;
  error.value = null;
  savedName.value = null;
}

defineExpose({ reset });
</script>

<template>
  <div class="border-t px-5 py-3">
    <div v-if="name === null" class="flex items-center gap-3">
      <button
        type="button"
        class="text-sm font-medium text-green-700 hover:underline dark:text-green-400"
        @click="name = ''"
      >
        Save as a meal
      </button>
      <span v-if="savedName" class="text-xs text-gray-500 dark:text-gray-400">
        Saved as &ldquo;{{ savedName }}&rdquo; - it is in Quick add now.
      </span>
      <span v-else class="text-xs text-gray-400">
        Eat this often? Save it and log it again in one tap.
      </span>
    </div>

    <div v-else class="space-y-2">
      <div class="flex items-center gap-2">
        <input
          v-model="name"
          type="text"
          maxlength="100"
          placeholder="Name it, e.g. My turkey sandwich"
          aria-label="Name for this saved meal"
          class="flex-1 rounded-lg border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-900"
          @keydown.enter.prevent="save"
        />
        <button
          type="button"
          :disabled="!name.trim() || isSaving"
          class="rounded-lg bg-green-600 px-3 py-1.5 text-sm font-semibold text-white hover:bg-green-700 disabled:bg-green-300"
          @click="save"
        >
          {{ isSaving ? 'Saving...' : 'Save' }}
        </button>
        <button
          type="button"
          class="text-sm text-gray-500 hover:text-gray-700 dark:text-gray-400"
          @click="name = null"
        >
          Cancel
        </button>
      </div>
      <p v-if="error" class="text-xs text-red-600 dark:text-red-400">{{ error }}</p>
    </div>
  </div>
</template>
