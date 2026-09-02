<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import type { GroceryItemRequest } from '@foodeez/shared';
import AppLayout from '@/components/layout/AppLayout.vue';
import AddGroceryItem from '@/components/grocery/AddGroceryItem.vue';
import GroceryItemRow from '@/components/grocery/GroceryItemRow.vue';
import GroceryRangePicker from '@/components/grocery/GroceryRangePicker.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import StreamingText from '@/components/ai/StreamingText.vue';
import { useGroceryStore } from '@/stores/grocery';
import { addDays, startOfWeek, toISODate, type RangeMode } from '@/utils/dateRange';

const groceryStore = useGroceryStore();

// A week is what people shop for, so that is where the tab opens.
const mode = ref<RangeMode>('week');
const monday = startOfWeek(new Date());
const startDate = ref(toISODate(monday));
const endDate = ref(toISODate(addDays(monday, 6)));

const list = computed(() => groceryStore.list);

const itemCount = computed(() => list.value?.items.length ?? 0);

const progressLabel = computed(() => {
  if (itemCount.value === 0) {
    return '';
  }

  const got = itemCount.value - groceryStore.remainingCount;
  return `${got} of ${itemCount.value} in the trolley`;
});

onMounted(load);

watch([startDate, endDate], load);

async function load() {
  await groceryStore.load(startDate.value, endDate.value);
}

function onRangeChange(range: { startDate: string; endDate: string }) {
  startDate.value = range.startDate;
  endDate.value = range.endDate;
}

async function build(refresh: boolean) {
  await groceryStore.generate(startDate.value, endDate.value, refresh);
}

function save(itemId: string, item: GroceryItemRequest) {
  void groceryStore.updateItem(itemId, item);
}
</script>

<template>
  <AppLayout>
    <div class="mx-auto max-w-3xl p-4 sm:p-6">
      <header class="mb-5">
        <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">Grocery list</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400">
          Everything your planned meals need, in one shop
        </p>
      </header>

      <div class="mb-5 rounded-2xl bg-white p-4 shadow-sm dark:bg-gray-900">
        <GroceryRangePicker
          :mode="mode"
          :start-date="startDate"
          :end-date="endDate"
          @update:mode="mode = $event"
          @change="onRangeChange"
        />
      </div>

      <div v-if="groceryStore.isGenerating" class="rounded-2xl bg-white p-4 shadow-sm dark:bg-gray-900">
        <div class="mb-2 flex items-center justify-between">
          <p class="text-xs font-semibold uppercase tracking-wide text-purple-700 dark:text-purple-400">
            Working out the shop
          </p>
          <button
            type="button"
            class="text-xs font-semibold text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"
            @click="groceryStore.cancelGenerate"
          >
            Stop
          </button>
        </div>
        <StreamingText
          :text="groceryStore.generationText"
          placeholder="Reading your planned meals..."
        />
      </div>

      <div v-else-if="groceryStore.isLoading" class="flex justify-center py-12">
        <LoadingSpinner size="lg" />
      </div>

      <!-- Nothing planned: a shopping list cannot be conjured from an empty calendar. -->
      <div
        v-else-if="groceryStore.plannedMealCount === 0"
        class="rounded-2xl bg-white px-6 py-14 text-center shadow-sm dark:bg-gray-900"
      >
        <p class="mb-2 text-4xl">🛒</p>
        <p class="text-lg font-semibold text-gray-800 dark:text-gray-100">
          No meals planned for these dates
        </p>
        <p class="mx-auto mt-1 max-w-sm text-sm text-gray-500 dark:text-gray-400">
          Plan some meals on the calendar - or ask for a few in the Advice tab - and the list
          will follow.
        </p>
        <RouterLink
          to="/meal-plan"
          class="mt-5 inline-block rounded-xl bg-green-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-green-700"
        >
          Go to meal plan
        </RouterLink>
      </div>

      <!-- Planned, not yet compiled. -->
      <div
        v-else-if="!list"
        class="rounded-2xl bg-white px-6 py-14 text-center shadow-sm dark:bg-gray-900"
      >
        <p class="mb-2 text-4xl">🛒</p>
        <p class="text-lg font-semibold text-gray-800 dark:text-gray-100">
          {{ groceryStore.plannedMealCount }}
          {{ groceryStore.plannedMealCount === 1 ? 'meal' : 'meals' }} planned
        </p>
        <p class="mx-auto mt-1 max-w-sm text-sm text-gray-500 dark:text-gray-400">
          Pull them together into one list, with the amounts added up and the aisles in order.
        </p>
        <button
          type="button"
          class="mt-5 rounded-xl bg-green-600 px-5 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-green-700"
          @click="build(false)"
        >
          Build my list
        </button>
      </div>

      <template v-else>
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
          <p class="text-sm text-gray-500 dark:text-gray-400">
            {{ progressLabel }}
            <span v-if="list.plannedMealCount > 0" class="text-gray-400 dark:text-gray-500">
              &middot; from {{ list.plannedMealCount }}
              {{ list.plannedMealCount === 1 ? 'meal' : 'meals' }}
            </span>
          </p>
          <button
            type="button"
            class="rounded-lg border-2 border-gray-200 px-3 py-1.5 text-xs font-semibold text-gray-600 transition-colors hover:border-gray-300 dark:border-gray-700 dark:text-gray-300"
            @click="build(true)"
          >
            Rebuild
          </button>
        </div>

        <!-- The plan moved on. The list is not replaced: it may be half shopped. -->
        <div
          v-if="list.isStale"
          class="mb-4 flex flex-wrap items-center justify-between gap-2 rounded-xl bg-amber-50 px-3 py-2 text-sm text-amber-800 dark:bg-amber-900/30 dark:text-amber-300"
        >
          <span>Your meal plan has changed since this list was made.</span>
          <button
            type="button"
            class="font-semibold underline"
            @click="build(true)"
          >
            Rebuild it
          </button>
        </div>

        <div class="space-y-4">
          <section
            v-for="group in groceryStore.grouped"
            :key="group.category"
            class="rounded-2xl bg-white p-4 shadow-sm dark:bg-gray-900"
          >
            <p class="mb-1 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
              {{ group.category }}
            </p>
            <ul>
              <GroceryItemRow
                v-for="item in group.items"
                :key="item.id"
                :item="item"
                @toggle="groceryStore.setChecked"
                @save="save"
                @remove="groceryStore.removeItem"
              />
            </ul>
          </section>
        </div>

        <div class="mt-4 rounded-2xl bg-white p-4 shadow-sm dark:bg-gray-900">
          <AddGroceryItem @add="groceryStore.addItem" />
          <p class="mt-2 text-xs text-gray-400 dark:text-gray-500">
            Anything you add or change by hand is kept when the list is rebuilt.
          </p>
        </div>
      </template>

      <p
        v-if="groceryStore.error"
        class="mt-4 rounded-xl bg-red-50 px-3 py-2 text-sm text-red-600 dark:bg-red-900/30 dark:text-red-300"
      >
        {{ groceryStore.error }}
      </p>
    </div>
  </AppLayout>
</template>
