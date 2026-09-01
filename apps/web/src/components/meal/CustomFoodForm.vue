<script setup lang="ts">
import { ref } from 'vue';
import AppInput from '@/components/ui/AppInput.vue';
import AppButton from '@/components/ui/AppButton.vue';
import { foodItemService } from '@/services/foodItemService';
import { estimateNutrition } from '@/services/aiService';
import type { FoodItem } from '@foodeez/shared';

const emit = defineEmits<{
  /** The saved item, ready to drop straight into the meal being logged. */
  created: [item: FoodItem];
  cancel: [];
}>();

const name = ref('');
const brand = ref('');
const servingSize = ref('100');
const servingUnit = ref('g');
const calories = ref('');
const protein = ref('');
const carbohydrates = ref('');
const fat = ref('');
const fiber = ref('');
const sugar = ref('');
const sodium = ref('');

const isSaving = ref(false);
const error = ref<string | null>(null);
const errors = ref<Record<string, string>>({});

const UNITS = ['g', 'ml', 'piece', 'slice', 'cup', 'tbsp', 'tsp', 'serving'];

// Nobody knows the macros of their own cooking, so the numbers are estimated from a plain
// description of what went in. Everything stays editable afterwards.
const ingredients = ref('');
const servings = ref('1');
const isEstimating = ref(false);
const estimateNote = ref<string | null>(null);
const estimateConfidence = ref<'low' | 'medium' | 'high' | null>(null);

const confidenceCopy: Record<string, string> = {
  high: 'Confident — amounts were specific.',
  medium: 'Reasonable — some amounts were assumed.',
  low: 'Rough — add ingredients and amounts for a closer estimate.',
};

async function runEstimate() {
  if (!name.value.trim()) {
    errors.value = { ...errors.value, name: 'Name the dish first' };
    return;
  }

  isEstimating.value = true;
  estimateNote.value = null;
  error.value = null;

  try {
    const result = await estimateNutrition({
      name: name.value.trim(),
      ingredients: ingredients.value.trim() || undefined,
      servings: Math.max(1, parseInt(servings.value, 10) || 1),
      servingDescription: `${servingSize.value} ${servingUnit.value}`.trim(),
    });

    if (!result.succeeded) {
      estimateNote.value =
        'Could not estimate this one automatically — enter whatever values you know.';
      return;
    }

    const n = result.perServing;
    calories.value = String(Math.round(n.calories));
    protein.value = String(Math.round(n.protein));
    carbohydrates.value = String(Math.round(n.carbohydrates));
    fat.value = String(Math.round(n.fat));
    fiber.value = String(Math.round(n.fiber));
    sugar.value = String(Math.round(n.sugar));
    sodium.value = String(Math.round(n.sodium));

    if (result.servingSize > 0) {
      servingSize.value = String(Math.round(result.servingSize));
      if (result.servingUnit) servingUnit.value = result.servingUnit;
    }

    estimateConfidence.value = result.confidence;
    estimateNote.value = result.assumptions ?? null;
    errors.value = {};
  } catch {
    estimateNote.value = 'The estimate service is unavailable right now. Enter values manually.';
  } finally {
    isEstimating.value = false;
  }
}

function num(value: string): number {
  const n = parseFloat(value);
  return isNaN(n) ? 0 : n;
}

function validate(): boolean {
  const next: Record<string, string> = {};
  if (!name.value.trim()) next.name = 'Give it a name';
  if (num(servingSize.value) <= 0) next.servingSize = 'Must be greater than zero';
  if (!servingUnit.value.trim()) next.servingUnit = 'Pick a unit';
  if (calories.value.trim() === '' || num(calories.value) < 0) {
    next.calories = 'Enter the calories per serving';
  }
  errors.value = next;
  return Object.keys(next).length === 0;
}

async function save() {
  if (!validate()) return;
  error.value = null;
  isSaving.value = true;

  try {
    const created = await foodItemService.createFoodItem({
      name: name.value.trim(),
      brand: brand.value.trim() || undefined,
      servingSize: num(servingSize.value),
      servingUnit: servingUnit.value.trim(),
      category: 'Homemade',
      calories: num(calories.value),
      protein: num(protein.value),
      carbohydrates: num(carbohydrates.value),
      fat: num(fat.value),
      fiber: num(fiber.value),
      sugar: num(sugar.value),
      sodium: num(sodium.value),
    });
    emit('created', created);
  } catch (err: unknown) {
    const e = err as { response?: { data?: { detail?: string; title?: string } } };
    error.value = e.response?.data?.detail ?? e.response?.data?.title ?? 'Could not save this food.';
  } finally {
    isSaving.value = false;
  }
}
</script>

<template>
  <div
    class="space-y-4 rounded-xl border border-green-200 bg-green-50/50 p-4 dark:border-green-900/50 dark:bg-green-950/20"
  >
    <div>
      <p class="text-sm font-semibold text-gray-900 dark:text-gray-100">Add a homemade food</p>
      <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">
        Saved to your account, so it turns up in search next time you cook it.
      </p>
    </div>

    <p v-if="error" class="text-sm text-red-600 dark:text-red-400">{{ error }}</p>

    <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
      <AppInput v-model="name" label="Name" :error="errors.name" placeholder="Nan's lasagne" />
      <AppInput v-model="brand" label="Brand or source" placeholder="Optional" />
    </div>

    <div class="grid grid-cols-2 gap-3">
      <AppInput
        v-model="servingSize"
        label="Serving size"
        :error="errors.servingSize"
        type="number"
        inputmode="decimal"
      />
      <div class="flex flex-col gap-1">
        <label class="text-sm font-medium text-gray-700 dark:text-gray-200">Unit</label>
        <select
          v-model="servingUnit"
          class="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
        >
          <option v-for="u in UNITS" :key="u" :value="u">{{ u }}</option>
        </select>
      </div>
    </div>

    <!-- Estimate step: describe the cooking, let the model do the arithmetic. -->
    <div class="rounded-lg border border-gray-200 bg-white p-3 dark:border-gray-700 dark:bg-gray-900">
      <label class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-200">
        What went into it?
      </label>
      <textarea
        v-model="ingredients"
        rows="4"
        placeholder="500g beef mince&#10;2 jars passata&#10;250g lasagne sheets&#10;200g cheddar"
        class="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
      />
      <div class="mt-2 flex flex-wrap items-end gap-3">
        <div class="w-32">
          <AppInput
            v-model="servings"
            label="Servings in the batch"
            type="number"
            inputmode="numeric"
          />
        </div>
        <AppButton size="sm" :loading="isEstimating" @click="runEstimate">
          Estimate nutrition
        </AppButton>
      </div>
      <p class="mt-2 text-xs text-gray-500 dark:text-gray-400">
        Rough amounts are fine. You can skip this and type the numbers in yourself.
      </p>

      <div
        v-if="estimateNote"
        class="mt-3 rounded-lg bg-gray-50 p-2.5 text-xs text-gray-600 dark:bg-gray-800 dark:text-gray-300"
      >
        <p v-if="estimateConfidence" class="font-semibold text-gray-700 dark:text-gray-200">
          {{ confidenceCopy[estimateConfidence] }}
        </p>
        <p class="mt-0.5">{{ estimateNote }}</p>
      </div>
    </div>

    <div>
      <p class="mb-2 text-sm font-medium text-gray-700 dark:text-gray-200">
        Nutrition per serving
      </p>
      <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <AppInput
          v-model="calories"
          label="Calories"
          :error="errors.calories"
          type="number"
          inputmode="decimal"
        />
        <AppInput v-model="protein" label="Protein (g)" type="number" inputmode="decimal" />
        <AppInput v-model="carbohydrates" label="Carbs (g)" type="number" inputmode="decimal" />
        <AppInput v-model="fat" label="Fat (g)" type="number" inputmode="decimal" />
        <AppInput v-model="fiber" label="Fiber (g)" type="number" inputmode="decimal" />
        <AppInput v-model="sugar" label="Sugar (g)" type="number" inputmode="decimal" />
        <AppInput v-model="sodium" label="Sodium (mg)" type="number" inputmode="decimal" />
      </div>
      <p class="mt-2 text-xs text-gray-500 dark:text-gray-400">
        Estimates are a starting point, not a lab result — correct anything that looks off.
        Only calories are required; blanks count as zero.
      </p>
    </div>

    <div class="flex justify-end gap-2">
      <AppButton variant="ghost" size="sm" @click="emit('cancel')">Cancel</AppButton>
      <AppButton size="sm" :loading="isSaving" @click="save">Save &amp; add</AppButton>
    </div>
  </div>
</template>
