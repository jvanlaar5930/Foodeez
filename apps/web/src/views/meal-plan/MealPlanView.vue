<template>
  <AppLayout>
    <div class="p-6 max-w-6xl mx-auto">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Meal Plan</h1>
          <p class="text-gray-500 dark:text-gray-400 text-sm mt-1">Plan and manage your weekly meals</p>
        </div>
        <div class="flex items-center gap-2">
          <button
            @click="openGenerate"
            :disabled="isGenerating"
            class="flex items-center gap-2 bg-orange-500 hover:bg-orange-600 disabled:bg-orange-300 text-white font-semibold px-4 py-2 rounded-xl transition-colors">
            <span v-if="isGenerating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
            <span v-else>✨</span>
            {{ isGenerating ? 'Generating...' : 'Generate Plan' }}
          </button>
          <!-- Generation can take minutes on a self-hosted model, so there has to be a
               way out that actually stops the work rather than just hiding the spinner. -->
          <button
            v-if="isGenerating"
            type="button"
            @click="planStore.cancelGeneration()"
            class="rounded-xl border border-gray-300 px-4 py-2 font-semibold text-gray-600 transition-colors hover:border-gray-400 hover:text-gray-800 dark:border-gray-700 dark:text-gray-300 dark:hover:text-gray-100">
            Cancel
          </button>
        </div>
      </div>

      <!-- The model's thinking, as it writes it: a week of meals takes a while, and a
           spinner alone gives no sign that anything is happening. -->
      <div
        v-if="isGenerating"
        class="mb-4 rounded-xl border border-purple-200 bg-purple-50 p-4 dark:border-purple-900 dark:bg-purple-950/40"
      >
        <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-purple-700 dark:text-purple-400">
          Writing your plan
        </p>
        <StreamingText :text="generationText" placeholder="Reading your profile and targets..." />
      </div>

      <!-- Generation can fail for reasons worth reading: the AI provider being overloaded
           is temporary and retrying is the right response. -->
      <div
        v-if="error"
        class="mb-4 flex items-start justify-between gap-4 rounded-xl border border-amber-200 bg-amber-50 p-4 dark:border-amber-900/40 dark:bg-amber-950/30"
      >
        <p class="text-sm text-amber-800 dark:text-amber-200">{{ error }}</p>
        <button
          type="button"
          class="shrink-0 text-sm font-semibold text-amber-900 hover:underline dark:text-amber-100"
          @click="handleGeneratePlan"
        >
          Try again
        </button>
      </div>

      <!-- Week navigation -->
      <div class="flex items-center gap-4 mb-4">
        <button @click="prevWeek" class="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 font-bold text-gray-600 dark:text-gray-300">‹</button>
        <span class="font-semibold text-gray-800 dark:text-gray-100">
          {{ format(weekDays[0], 'MMM d') }} – {{ format(weekDays[6], 'MMM d, yyyy') }}
        </span>
        <button @click="nextWeek" class="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 font-bold text-gray-600 dark:text-gray-300">›</button>
        <button @click="goToCurrentWeek" class="text-sm text-green-600 dark:text-green-400 font-medium hover:text-green-700 dark:hover:text-green-400 ml-2">
          This Week
        </button>
      </div>

      <!-- Weekly calendar grid -->
      <div v-if="isLoading" class="flex justify-center py-16">
        <LoadingSpinner />
      </div>

      <div v-else class="bg-white dark:bg-gray-900 rounded-2xl shadow-sm overflow-hidden">
        <!-- Day headers -->
        <div class="grid grid-cols-8 border-b">
          <div class="p-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase"></div>
          <div v-for="day in weekDays" :key="day.toISOString()"
            class="p-3 text-center border-l"
            :class="isToday(day) ? 'bg-green-50 dark:bg-green-950/40' : ''">
            <p class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">{{ format(day, 'EEE') }}</p>
            <p class="text-lg font-bold mt-0.5" :class="isToday(day) ? 'text-green-600 dark:text-green-400' : 'text-gray-900 dark:text-gray-100'">
              {{ format(day, 'd') }}
            </p>
          </div>
        </div>

        <!-- Meal rows -->
        <div v-for="mealType in MEAL_TYPES" :key="mealType.value" class="grid grid-cols-8 border-b last:border-b-0">
          <div class="p-3 text-xs font-semibold text-gray-500 dark:text-gray-400 flex items-center">{{ mealType.label }}</div>
          <DayMealSlot
            v-for="day in weekDays"
            :key="`${day.toISOString()}-${mealType.value}`"
            :date="day"
            :meal-type="mealType.value"
            :entry="getEntry(day, mealType.value)"
            :logged-label="getLoggedLabel(day, mealType.value)"
            class="border-l"
            @click="openSlot(day, mealType.value)"
          />
        </div>
      </div>

      <!-- Empty state -->
      <div
        v-if="!isLoading && !hasEntriesThisWeek"
        class="mt-6 rounded-2xl bg-white shadow-sm dark:bg-gray-900"
      >
        <EmptyState
          emoji="📅"
          title="No meals planned this week"
          description="Click a slot to add a meal, or use AI to generate a full plan"
        />
      </div>

      <!-- Generate dialog: the guidance box is optional, so Enter-to-submit and an empty
           field both just generate. -->
      <div
        v-if="generateOpen"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
        @click.self="generateOpen = false"
      >
        <div class="w-full max-w-lg rounded-2xl bg-white p-5 shadow-xl dark:bg-gray-900">
          <h2 class="text-lg font-bold text-gray-900 dark:text-gray-100">Generate Plan</h2>
          <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
            A full week for {{ format(weekDays[0], 'MMM d') }} &ndash;
            {{ format(weekDays[6], 'MMM d') }}, built around your targets and the foods you avoid.
          </p>

          <label for="plan-guidance" class="mt-4 block text-sm font-semibold text-gray-700 dark:text-gray-200">
            Anything specific? <span class="font-normal text-gray-400">(optional)</span>
          </label>
          <textarea
            id="plan-guidance"
            v-model="guidance"
            rows="3"
            maxlength="1000"
            placeholder="e.g. more variety in the dinners, and reuse last week's breakfasts and lunches"
            class="mt-1 w-full resize-none rounded-xl border border-gray-200 px-3 py-2 text-sm text-gray-800 placeholder:text-gray-400 focus:border-orange-400 focus:outline-none dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100"
          />
          <p class="mt-1 text-xs text-gray-400">
            Mention last week and the plan you already have is used as the reference.
          </p>

          <div class="mt-4 flex justify-end gap-2">
            <button
              type="button"
              class="rounded-xl border border-gray-200 px-4 py-2 text-sm font-semibold text-gray-600 transition-colors hover:border-gray-300 dark:border-gray-700 dark:text-gray-300"
              @click="generateOpen = false"
            >
              Cancel
            </button>
            <button
              type="button"
              class="rounded-xl bg-orange-500 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-orange-600"
              @click="confirmGenerate"
            >
              Generate
            </button>
          </div>
        </div>
      </div>

      <MealSlotModal
        v-model="slotOpen"
        :date="slotDate"
        :meal-type="slotMealType"
        :entry="slotEntry"
        :saving="slotSaving"
        :error="slotError"
        @save="handleSaveSlot"
        @remove="handleRemoveSlot"
      />

      <AddMealModal
        v-if="logModalOpen && editingLog"
        :selected-date="editingLog.logDate"
        :meal-log="editingLog"
        @close="closeLogModal"
        @saved="onLogSaved"
      />
    </div>
  </AppLayout>
</template>

<script setup lang="ts">
import EmptyState from '@/components/ui/EmptyState.vue';
import { ref, computed, onMounted, watch } from 'vue';
import { format, startOfWeek, addDays, isToday } from 'date-fns';
import AppLayout from '@/components/layout/AppLayout.vue';
import DayMealSlot from '@/components/mealplan/DayMealSlot.vue';
import MealSlotModal from '@/components/mealplan/MealSlotModal.vue';
import AddMealModal from '@/components/meal/AddMealModal.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import StreamingText from '@/components/ai/StreamingText.vue';
import { useMealPlanStore } from '@/stores/mealPlan';
import { useAuthStore } from '@/stores/auth';
import { mealService } from '@/services/mealService';
import {
  MEAL_TYPE_SHORT_LABELS,
  MealType,
  ORDERED_MEAL_TYPES,
  type MealLog,
  type MealPlanEntry,
  type MealPlanEntryRequest,
} from '@foodeez/shared';

const authStore = useAuthStore();
const planStore = useMealPlanStore();

const weekStart = ref(startOfWeek(new Date(), { weekStartsOn: 1 }));
const weekDays = computed(() => Array.from({ length: 7 }, (_, i) => addDays(weekStart.value, i)));
const isLoading = computed(() => planStore.isLoading);
const isGenerating = computed(() => planStore.isGenerating);
const generationText = computed(() => planStore.generationText);
const error = computed(() => planStore.error);

const MEAL_TYPES = [
  ...ORDERED_MEAL_TYPES.map((value) => ({ value, label: MEAL_TYPE_SHORT_LABELS[value] })),
];

function getEntry(date: Date, mealType: MealType): MealPlanEntry | undefined {
  // Grouped by date on the server, so this is a lookup. The `?? []` is load-bearing: a day
  // with no meals has no key at all, and the old `.entries` did not exist on the payload.
  //
  // Read from the plan that covers the day rather than the most recent one, or paging back
  // a week would show an empty grid even where meals exist.
  const dateStr = format(date, 'yyyy-MM-dd');
  const forDay = planStore.planCovering(dateStr)?.entriesByDate?.[dateStr] ?? [];
  return forDay.find(e => e.mealType === mealType);
}

// What was actually logged for the visible week, shown read-only alongside what was
// planned - keyed by "date|mealType" since a day's logs are fetched as a flat list.
const loggedLogs = ref<MealLog[]>([]);

function loggedLabelKey(date: string, mealType: MealType) { return `${date}|${mealType}`; }

const loggedByKey = computed(() => {
  const map = new Map<string, MealLog>();
  for (const log of loggedLogs.value) {
    if (log.items.length > 0) map.set(loggedLabelKey(log.logDate, log.mealType), log);
  }
  return map;
});

function getLoggedEntry(date: Date, mealType: MealType): MealLog | undefined {
  return loggedByKey.value.get(loggedLabelKey(format(date, 'yyyy-MM-dd'), mealType));
}

function getLoggedLabel(date: Date, mealType: MealType): string | undefined {
  const log = getLoggedEntry(date, mealType);
  return log?.items.map(item => item.foodItem.name).filter(Boolean).join(', ') || undefined;
}

async function fetchLoggedWeek() {
  if (!authStore.user?.id) return;
  try {
    loggedLogs.value = await mealService.getLogsRange(
      authStore.user.id,
      format(weekDays.value[0], 'yyyy-MM-dd'),
      format(weekDays.value[6], 'yyyy-MM-dd'),
    );
  } catch {
    // Purely a display overlay on top of the plan grid - failing to load it should not
    // block the calendar itself from rendering.
    loggedLogs.value = [];
  }
}

const hasEntriesThisWeek = computed(() =>
  weekDays.value.some(d => MEAL_TYPES.some(mt => getEntry(d, mt.value) !== undefined || getLoggedLabel(d, mt.value) !== undefined))
);

function prevWeek() { weekStart.value = addDays(weekStart.value, -7); }
function nextWeek() { weekStart.value = addDays(weekStart.value, 7); }
function goToCurrentWeek() { weekStart.value = startOfWeek(new Date(), { weekStartsOn: 1 }); }

const generateOpen = ref(false);
/** Free text for the next generation. Survives the dialog so "Try again" repeats it. */
const guidance = ref('');

const slotOpen = ref(false);
const slotDate = ref<Date>(new Date());
const slotMealType = ref<MealType>(MealType.Breakfast);
const slotSaving = ref(false);
/**
 * Kept apart from the store's `error`, which the page banner owns: that banner offers
 * "Try again", meaning regenerate the whole plan, which is the wrong response to a slot
 * that would not save.
 */
const slotError = ref<string | null>(null);

const slotEntry = computed(() => getEntry(slotDate.value, slotMealType.value));

/**
 * A logged meal with no plan entry has nothing else editable behind it, so its slot opens
 * the meal-log editor instead of the plan editor. A slot with a plan entry always opens that
 * one - if both exist, the ✓ marker on the plan entry already says so, and the log itself is
 * still reachable from the daily log view.
 */
const logModalOpen = ref(false);
const editingLog = ref<MealLog | null>(null);

function openSlot(date: Date, mealType: MealType) {
  const entry = getEntry(date, mealType);
  if (!entry) {
    const log = getLoggedEntry(date, mealType);
    if (log) {
      editingLog.value = log;
      logModalOpen.value = true;
      return;
    }
  }

  planStore.clearError();
  slotError.value = null;
  slotDate.value = date;
  slotMealType.value = mealType;
  slotOpen.value = true;
}

function closeLogModal() {
  logModalOpen.value = false;
  editingLog.value = null;
}

async function onLogSaved() {
  closeLogModal();
  await fetchLoggedWeek();
}

async function handleSaveSlot(payload: MealPlanEntryRequest) {
  if (!authStore.user?.id) return;
  slotSaving.value = true;
  slotError.value = null;
  try {
    // A week you have never generated a plan for has no plan to hang the meal on, so make
    // one for that week rather than refusing the click.
    const plan = await planStore.ensurePlanFor(
      authStore.user.id,
      format(weekDays.value[0], 'yyyy-MM-dd'),
      format(weekDays.value[6], 'yyyy-MM-dd'),
      `Week of ${format(weekDays.value[0], 'MMM d, yyyy')}`,
    );
    await planStore.saveEntry(plan.id, slotEntry.value?.id ?? null, payload);
    slotOpen.value = false;
  } catch {
    // Shown in the dialog, which stays open so the typed-in meal is not lost.
    slotError.value = planStore.error;
    planStore.clearError();
  } finally {
    slotSaving.value = false;
  }
}

async function handleRemoveSlot() {
  const entry = slotEntry.value;
  if (!entry) return;
  slotSaving.value = true;
  slotError.value = null;
  try {
    await planStore.removeEntry(entry.mealPlanId, entry.id);
    slotOpen.value = false;
  } catch {
    slotError.value = planStore.error;
    planStore.clearError();
  } finally {
    slotSaving.value = false;
  }
}

function openGenerate() {
  generateOpen.value = true;
}

function confirmGenerate() {
  generateOpen.value = false;
  void handleGeneratePlan();
}

async function handleGeneratePlan() {
  if (!authStore.user?.id) return;
  try {
    await planStore.generatePlan({
      userId: authStore.user.id,
      startDate: format(weekDays.value[0], 'yyyy-MM-dd'),
      endDate: format(weekDays.value[6], 'yyyy-MM-dd'),
      // Kept between attempts on purpose: "Try again" after a provider outage should repeat
      // the request that was made, not quietly drop what was asked for.
      guidance: guidance.value.trim() || undefined,
    });
  } catch {
    // The store has already put the reason in `error`, which the banner above renders.
    // Without this catch the rethrow escaped as an unhandled rejection and the button
    // just silently stopped spinning.
  }
}

watch(weekStart, fetchLoggedWeek);

onMounted(() => {
  if (authStore.user?.id) planStore.fetchPlans(authStore.user.id);
  fetchLoggedWeek();
});
</script>
