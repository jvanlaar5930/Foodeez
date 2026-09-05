<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { RouterLink } from 'vue-router';
import { addDays, format, isSameDay, isToday, startOfWeek } from 'date-fns';
import AppButton from '@/components/ui/AppButton.vue';
import AppModal from '@/components/ui/AppModal.vue';
import { useAuthStore } from '@/stores/auth';
import { useMealPlanStore } from '@/stores/mealPlan';
import {
  MEAL_TYPE_SHORT_LABELS,
  MealType,
  ORDERED_MEAL_TYPES,
  type Recipe,
} from '@foodeez/shared';

const props = defineProps<{
  modelValue: boolean;
  recipe: Recipe;
}>();

const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>();

const authStore = useAuthStore();
const planStore = useMealPlanStore();

const selectedDate = ref(new Date());
const weekStart = ref(startOfWeek(new Date(), { weekStartsOn: 1 }));
const mealType = ref<MealType>(MealType.Dinner);
const servings = ref(1);
const saving = ref(false);
const error = ref<string | null>(null);
/** The day the recipe landed on, which turns the dialog into a confirmation. */
const savedOn = ref<Date | null>(null);

const weekDays = computed(() => Array.from({ length: 7 }, (_, i) => addDays(weekStart.value, i)));

const MEAL_TYPES = ORDERED_MEAL_TYPES.map((value) => ({
  value,
  label: MEAL_TYPE_SHORT_LABELS[value],
}));

/**
 * What is already in the chosen slot. The API replaces whatever occupies a slot, so saying
 * so up front is the difference between adding a meal and quietly losing one.
 */
const occupant = computed(() => {
  const dateStr = format(selectedDate.value, 'yyyy-MM-dd');
  const entry = planStore
    .planCovering(dateStr)
    ?.entriesByDate?.[dateStr]?.find((e) => e.mealType === mealType.value);
  if (!entry) return undefined;
  return entry.recipeName ?? entry.foodItemName ?? entry.notes ?? 'A meal';
});

// Seeded on open rather than on mount: the dialog outlives any one recipe.
watch(
  () => props.modelValue,
  (open) => {
    if (!open) return;
    const today = new Date();
    selectedDate.value = today;
    weekStart.value = startOfWeek(today, { weekStartsOn: 1 });
    mealType.value = MealType.Dinner;
    servings.value = 1;
    error.value = null;
    savedOn.value = null;
    // Needed before the first save so a week that already has a plan is reused rather than
    // given a second one, and it is what fills the "already in this slot" note below.
    if (authStore.user?.id && !planStore.hasLoaded) void planStore.fetchPlans(authStore.user.id);
  },
  { immediate: true },
);

function pickDay(day: Date): void {
  selectedDate.value = day;
}

function shiftWeek(days: number): void {
  weekStart.value = addDays(weekStart.value, days);
}

function close(): void {
  emit('update:modelValue', false);
}

async function save(): Promise<void> {
  if (!authStore.user?.id) return;
  if (!(servings.value > 0)) {
    error.value = 'Servings must be greater than zero.';
    return;
  }

  saving.value = true;
  error.value = null;
  try {
    // The day picked here can sit in a week that was never planned, so make a plan for that
    // week rather than refusing the save - the same fallback the calendar itself uses.
    const day = selectedDate.value;
    const planWeekStart = startOfWeek(day, { weekStartsOn: 1 });
    const plan = await planStore.ensurePlanFor(
      authStore.user.id,
      format(planWeekStart, 'yyyy-MM-dd'),
      format(addDays(planWeekStart, 6), 'yyyy-MM-dd'),
      `Week of ${format(planWeekStart, 'MMM d, yyyy')}`,
    );

    const dateStr = format(day, 'yyyy-MM-dd');
    const existing = plan.entriesByDate?.[dateStr]?.find((e) => e.mealType === mealType.value);
    // No notes: a linked recipe carries its own name.
    await planStore.saveEntry(plan.id, existing?.id ?? null, {
      entryDate: dateStr,
      mealType: mealType.value,
      recipeId: props.recipe.id,
      servings: servings.value,
    });
    savedOn.value = day;
  } catch {
    error.value = planStore.error ?? 'That meal could not be added. Please try again.';
    planStore.clearError();
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <AppModal
    :model-value="modelValue"
    title="Add to meal plan"
    size="md"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <!-- Confirmation: the recipe is in the plan, and the useful next move is going to look. -->
    <div v-if="savedOn" class="space-y-4">
      <p class="text-sm text-gray-700 dark:text-gray-300">
        <strong>{{ recipe.name }}</strong> is planned for
        {{ MEAL_TYPE_SHORT_LABELS[mealType].toLowerCase() }} on
        {{ format(savedOn, 'EEEE d MMMM') }}.
      </p>
      <RouterLink
        to="/meal-plan"
        class="inline-block rounded-xl bg-green-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-green-700"
      >
        View meal plan
      </RouterLink>
    </div>

    <div v-else class="space-y-5">
      <p class="text-sm text-gray-600 dark:text-gray-400">
        Pick a day and a meal for
        <strong class="text-gray-900 dark:text-gray-100">{{ recipe.name }}</strong
        >.
      </p>

      <div>
        <div class="mb-2 flex items-center justify-between">
          <span class="text-sm font-medium text-gray-700 dark:text-gray-200">Day</span>
          <span class="flex items-center gap-2">
            <button
              type="button"
              aria-label="Previous week"
              class="rounded-lg px-2 py-1 font-bold text-gray-500 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-800"
              @click="shiftWeek(-7)"
            >
              &lsaquo;
            </button>
            <span class="text-xs text-gray-500 dark:text-gray-400">
              {{ format(weekDays[0], 'MMM d') }} &ndash; {{ format(weekDays[6], 'MMM d') }}
            </span>
            <button
              type="button"
              aria-label="Next week"
              class="rounded-lg px-2 py-1 font-bold text-gray-500 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-800"
              @click="shiftWeek(7)"
            >
              &rsaquo;
            </button>
          </span>
        </div>

        <div class="grid grid-cols-7 gap-1">
          <button
            v-for="day in weekDays"
            :key="day.toISOString()"
            type="button"
            class="rounded-xl border py-2 text-center transition-colors"
            :class="
              isSameDay(day, selectedDate)
                ? 'border-green-600 bg-green-600 text-white'
                : 'border-gray-200 text-gray-700 hover:border-green-400 dark:border-gray-700 dark:text-gray-300'
            "
            :aria-pressed="isSameDay(day, selectedDate)"
            @click="pickDay(day)"
          >
            <span class="block text-[10px] font-semibold uppercase tracking-wide opacity-70">
              {{ format(day, 'EEE') }}
            </span>
            <span class="block text-sm font-bold">{{ format(day, 'd') }}</span>
            <span
              v-if="isToday(day)"
              class="mx-auto mt-0.5 block h-1 w-1 rounded-full"
              :class="isSameDay(day, selectedDate) ? 'bg-white' : 'bg-green-500'"
            />
          </button>
        </div>
      </div>

      <div>
        <span class="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200">Meal</span>
        <div class="flex flex-wrap gap-2">
          <button
            v-for="type in MEAL_TYPES"
            :key="type.value"
            type="button"
            class="rounded-full border px-3 py-1.5 text-sm font-medium transition-colors"
            :class="
              mealType === type.value
                ? 'border-green-600 bg-green-600 text-white'
                : 'border-gray-300 text-gray-600 hover:border-green-400 dark:border-gray-700 dark:text-gray-400'
            "
            :aria-pressed="mealType === type.value"
            @click="mealType = type.value"
          >
            {{ type.label }}
          </button>
        </div>
      </div>

      <div>
        <label
          for="plan-servings"
          class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-200"
        >
          Servings
        </label>
        <input
          id="plan-servings"
          v-model.number="servings"
          type="number"
          min="0.25"
          step="0.25"
          class="w-28 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:border-green-500 focus:outline-none focus:ring-1 focus:ring-green-500 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100"
        />
      </div>

      <p v-if="occupant" class="text-xs text-amber-700 dark:text-amber-400">
        {{ occupant }} is already planned for this slot and will be replaced.
      </p>

      <p v-if="error" class="text-sm text-red-600 dark:text-red-400">{{ error }}</p>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2">
        <AppButton variant="outline" :disabled="saving" @click="close">
          {{ savedOn ? 'Done' : 'Cancel' }}
        </AppButton>
        <AppButton v-if="!savedOn" :loading="saving" @click="save">Add to plan</AppButton>
      </div>
    </template>
  </AppModal>
</template>
