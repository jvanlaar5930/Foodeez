<script setup lang="ts">
import { computed, ref } from 'vue';
import { MealType } from '@foodeez/shared';
import type { MealPlanEntry } from '@foodeez/shared';
import { MEAL_TYPE_LABELS } from '@foodeez/shared';

interface Props {
  date: Date;
  mealType: MealType;
  entry?: MealPlanEntry;
  /** What was actually logged for this slot, if anything - read-only, from the Meal Log. */
  loggedLabel?: string;
  /** True while any slot on the calendar is being dragged, so empty cells can offer a target. */
  isDragActive?: boolean;
}

const props = defineProps<Props>();
const emit = defineEmits<{
  click: [];
  dragstart: [entry: MealPlanEntry];
  dragend: [];
  drop: [];
}>();

const displayName = computed(() => {
  if (!props.entry) return null;
  // AI-generated entries have no Recipe or FoodItem row behind them, so the meal's own
  // name is in `notes` - without this fallback every generated slot rendered nameless.
  return props.entry.recipeName ?? props.entry.foodItemName ?? props.entry.notes ?? null;
});

/** Highlighted only while a drag is actually over this cell, not for the whole gesture. */
const isOver = ref(false);

function onDragStart(event: DragEvent) {
  if (!props.entry) return;

  // Firefox will not begin a drag unless something is put on the dataTransfer, even when
  // nothing ever reads it back - the entry itself travels through the parent, because a
  // dragged id would have to be looked up again and can name a row that has since changed.
  event.dataTransfer?.setData('text/plain', props.entry.id);
  if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';

  emit('dragstart', props.entry);
}

function onDrop() {
  isOver.value = false;
  emit('drop');
}

function onKeydown(event: KeyboardEvent) {
  // This is a div rather than a button - Firefox does not reliably start a drag from a
  // <button> - so the keyboard behaviour a button gave for free is restored by hand.
  if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault();
    emit('click');
  }
}

const mealColors: Record<MealType, string> = {
  [MealType.Breakfast]: 'bg-orange-100 dark:bg-orange-900/40 text-orange-700 dark:text-orange-400 border-orange-200',
  [MealType.MorningSnack]: 'bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-400 border-green-200 dark:border-green-800',
  [MealType.Lunch]: 'bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-400 border-blue-200 dark:border-blue-900/50',
  [MealType.AfternoonSnack]: 'bg-yellow-100 dark:bg-yellow-900/40 text-yellow-700 dark:text-yellow-400 border-yellow-200 dark:border-yellow-900/50',
  [MealType.Dinner]: 'bg-purple-100 dark:bg-purple-900/40 text-purple-700 dark:text-purple-400 border-purple-200',
  [MealType.EveningSnack]: 'bg-pink-100 dark:bg-pink-900/40 text-pink-700 border-pink-200',
};
</script>

<template>
  <div
    role="button"
    tabindex="0"
    :draggable="!!entry"
    :title="
      entry
        ? `${displayName ?? MEAL_TYPE_LABELS[mealType]} — drag to another day or slot`
        : (loggedLabel ?? `Add ${MEAL_TYPE_LABELS[mealType]}`)
    "
    :class="[
      'w-full h-full min-h-[3rem] p-1 rounded-md text-left transition-colors text-xs cursor-pointer',
      'focus:outline-none focus-visible:ring-2 focus-visible:ring-green-500',
      entry
        ? `${mealColors[mealType] ?? 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-200 border-gray-200 dark:border-gray-700'} border`
        : loggedLabel
          ? 'border border-dashed border-gray-300 text-gray-500 dark:border-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800'
          : 'text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 hover:text-gray-500 dark:hover:text-gray-400',
      // The drop target has to be obvious mid-drag: the cursor is somewhere over a grid of
      // 48 near-identical cells, and dropping on the wrong one silently rearranges the week.
      isOver ? 'ring-2 ring-green-500 ring-inset' : '',
      isDragActive && !isOver && !entry ? 'border border-dashed border-green-300 dark:border-green-800' : '',
    ]"
    @click="emit('click')"
    @keydown="onKeydown"
    @dragstart="onDragStart"
    @dragend="emit('dragend')"
    @dragover.prevent="isOver = true"
    @dragleave="isOver = false"
    @drop.prevent="onDrop"
  >
    <span v-if="displayName" class="line-clamp-2 font-medium leading-snug">
      <!-- The enhanced version carries the same name as the recipe it elevates, so the star
           is the only thing on this cell that says which of the two is being cooked. The
           label is spelled out for a screen reader, which would otherwise read the emoji
           itself - "star" says nothing about which version this is. -->
      <span v-if="entry?.recipeIsEnhanced" title="The enhanced version of this recipe">
        <span aria-hidden="true">⭐</span>
        <span class="sr-only">Enhanced version of</span>
      </span>
      {{ displayName }}
      <span v-if="loggedLabel" title="Also logged as eaten">✓</span>
    </span>
    <span v-else-if="loggedLabel" class="line-clamp-2 italic leading-snug">
      {{ loggedLabel }}
    </span>
    <span v-else class="flex items-center justify-center h-full opacity-0 hover:opacity-100 transition-opacity">
      +
    </span>
  </div>
</template>
