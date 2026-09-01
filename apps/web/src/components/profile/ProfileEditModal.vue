<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import AppModal from '@/components/ui/AppModal.vue';
import AppInput from '@/components/ui/AppInput.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppAlert from '@/components/ui/AppAlert.vue';
import { useAuthStore } from '@/stores/auth';
import { useProfileStore } from '@/stores/profile';
import {
  ActivityLevel,
  DietaryGoal,
  Gender,
  UnitSystem,
  cmToFeetInches,
  feetInchesToCm,
  kgToLb,
  lbToKg,
} from '@foodeez/shared';

const props = defineProps<{ modelValue: boolean }>();
const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>();

const authStore = useAuthStore();
const profileStore = useProfileStore();

const GENDERS = [
  { value: Gender.Male, label: 'Male' },
  { value: Gender.Female, label: 'Female' },
  { value: Gender.Other, label: 'Other' },
  { value: Gender.PreferNotToSay, label: 'Prefer not to say' },
];

const ACTIVITY = [
  { value: ActivityLevel.Sedentary, label: 'Sedentary' },
  { value: ActivityLevel.LightlyActive, label: 'Lightly active' },
  { value: ActivityLevel.ModeratelyActive, label: 'Moderately active' },
  { value: ActivityLevel.VeryActive, label: 'Very active' },
  { value: ActivityLevel.ExtraActive, label: 'Extra active' },
];

const GOALS = [
  { value: DietaryGoal.WeightLoss, label: 'Lose weight' },
  { value: DietaryGoal.WeightMaintenance, label: 'Maintain weight' },
  { value: DietaryGoal.WeightGain, label: 'Gain weight' },
  { value: DietaryGoal.MuscleGain, label: 'Build muscle' },
  { value: DietaryGoal.GeneralHealth, label: 'General health' },
];

// The profile stores kg and cm. These fields hold whatever the user's unit system displays,
// and convert back on save, so a US user never types pounds into a field that means kg.
const units = ref<UnitSystem>(UnitSystem.Metric);
const weight = ref('');
const targetWeight = ref('');
const heightCm = ref('');
const heightFeet = ref('');
const heightInches = ref('');
const age = ref('');
const gender = ref<Gender>(Gender.PreferNotToSay);
const activityLevel = ref<ActivityLevel>(ActivityLevel.ModeratelyActive);
const dietaryGoal = ref<DietaryGoal>(DietaryGoal.GeneralHealth);
const notes = ref('');
const error = ref<string | null>(null);
const errors = ref<Record<string, string>>({});

const isMetric = computed(() => units.value === UnitSystem.Metric);
const weightLabel = computed(() => (isMetric.value ? 'Weight (kg)' : 'Weight (lb)'));
const targetLabel = computed(() => (isMetric.value ? 'Target weight (kg)' : 'Target weight (lb)'));

function loadFromProfile() {
  const p = profileStore.profile;
  if (!p) return;

  units.value = p.unitSystem ?? UnitSystem.Metric;
  const metric = units.value === UnitSystem.Metric;

  weight.value = String(round(metric ? p.weightKg : kgToLb(p.weightKg)));
  targetWeight.value =
    p.targetWeightKg != null
      ? String(round(metric ? p.targetWeightKg : kgToLb(p.targetWeightKg)))
      : '';

  heightCm.value = String(Math.round(p.heightCm));
  const { feet, inches } = cmToFeetInches(p.heightCm);
  heightFeet.value = String(feet);
  heightInches.value = String(inches);

  age.value = String(p.age);
  gender.value = p.gender;
  activityLevel.value = p.activityLevel;
  dietaryGoal.value = p.dietaryGoal;
  notes.value = p.notes ?? '';
  errors.value = {};
  error.value = null;
}

function round(n: number): number {
  return Math.round(n * 10) / 10;
}

/** Re-express the entered values when the unit toggle flips, so nothing silently changes meaning. */
function switchUnits(next: UnitSystem) {
  if (next === units.value) return;
  const toMetric = next === UnitSystem.Metric;

  const w = parseFloat(weight.value);
  if (!isNaN(w)) weight.value = String(round(toMetric ? lbToKg(w) : kgToLb(w)));

  const t = parseFloat(targetWeight.value);
  if (!isNaN(t)) targetWeight.value = String(round(toMetric ? lbToKg(t) : kgToLb(t)));

  if (toMetric) {
    const cm = feetInchesToCm(parseInt(heightFeet.value, 10) || 0, parseInt(heightInches.value, 10) || 0);
    if (cm > 0) heightCm.value = String(Math.round(cm));
  } else {
    const { feet, inches } = cmToFeetInches(parseFloat(heightCm.value) || 0);
    heightFeet.value = String(feet);
    heightInches.value = String(inches);
  }

  units.value = next;
}

/** Everything the API stores, back in canonical kg/cm. */
const canonical = computed(() => {
  const w = parseFloat(weight.value);
  const t = targetWeight.value.trim() ? parseFloat(targetWeight.value) : null;
  const weightKg = isNaN(w) ? NaN : isMetric.value ? w : lbToKg(w);
  const targetKg = t === null || isNaN(t) ? null : isMetric.value ? t : lbToKg(t);
  const cm = isMetric.value
    ? parseFloat(heightCm.value)
    : feetInchesToCm(parseInt(heightFeet.value, 10) || 0, parseInt(heightInches.value, 10) || 0);
  return { weightKg, targetKg, heightCm: cm, age: parseInt(age.value, 10) };
});

function validate(): boolean {
  const next: Record<string, string> = {};
  const c = canonical.value;

  if (isNaN(c.weightKg) || c.weightKg < 20 || c.weightKg > 500) {
    next.weight = 'Enter a realistic weight';
  }
  if (c.targetKg !== null && (c.targetKg < 20 || c.targetKg > 500)) {
    next.targetWeight = 'Enter a realistic target, or leave it blank';
  }
  if (isNaN(c.heightCm) || c.heightCm < 50 || c.heightCm > 260) {
    next.height = 'Enter a realistic height';
  }
  if (isNaN(c.age) || c.age < 13 || c.age > 120) {
    next.age = 'Enter an age between 13 and 120';
  }

  errors.value = next;
  return Object.keys(next).length === 0;
}

async function save() {
  const userId = authStore.user?.id;
  if (!userId || !validate()) return;

  error.value = null;
  const c = canonical.value;

  try {
    await profileStore.updateProfile(userId, {
      heightCm: Math.round(c.heightCm * 10) / 10,
      weightKg: Math.round(c.weightKg * 10) / 10,
      targetWeightKg: c.targetKg === null ? undefined : Math.round(c.targetKg * 10) / 10,
      age: c.age,
      gender: gender.value,
      activityLevel: activityLevel.value,
      dietaryGoal: dietaryGoal.value,
      notes: notes.value.trim() || undefined,
      // Carry the current appearance choice through; this form does not own it.
      darkMode: profileStore.profile?.darkMode,
      unitSystem: units.value,
    });
    emit('update:modelValue', false);
  } catch {
    error.value = profileStore.error ?? 'Could not save your profile. Please try again.';
  }
}

// Reload each time the modal opens, so a cancelled edit never leaks into the next one.
watch(
  () => props.modelValue,
  (open) => {
    if (open) loadFromProfile();
  },
  { immediate: true },
);

const selectClass =
  'w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100';
</script>

<template>
  <AppModal
    :model-value="modelValue"
    size="lg"
    title="Edit Profile"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div class="space-y-5">
      <AppAlert v-if="error" variant="error" :message="error" />

      <!-- Units -->
      <div>
        <p class="mb-2 text-sm font-medium text-gray-700 dark:text-gray-200">Measurement units</p>
        <div class="inline-flex rounded-xl border border-gray-200 p-1 dark:border-gray-700">
          <button
            v-for="opt in [
              { value: UnitSystem.Metric, label: 'Metric (kg, cm)' },
              { value: UnitSystem.US, label: 'US (lb, ft/in)' },
            ]"
            :key="opt.value"
            type="button"
            class="rounded-lg px-3 py-1.5 text-sm font-medium transition-colors"
            :class="
              units === opt.value
                ? 'bg-green-600 text-white'
                : 'text-gray-600 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800'
            "
            @click="switchUnits(opt.value)"
          >
            {{ opt.label }}
          </button>
        </div>
        <p class="mt-1.5 text-xs text-gray-500 dark:text-gray-400">
          Applies everywhere, including the mobile app. Nutrition stays in grams and kcal.
        </p>
      </div>

      <!-- Body -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <AppInput
          v-model="weight"
          :label="weightLabel"
          :error="errors.weight"
          type="number"
          step="0.1"
          inputmode="decimal"
        />
        <AppInput
          v-model="targetWeight"
          :label="targetLabel"
          :error="errors.targetWeight"
          type="number"
          step="0.1"
          inputmode="decimal"
          hint="Optional"
        />

        <div v-if="isMetric">
          <AppInput
            v-model="heightCm"
            label="Height (cm)"
            :error="errors.height"
            type="number"
            inputmode="numeric"
          />
        </div>
        <div v-else class="grid grid-cols-2 gap-2">
          <AppInput v-model="heightFeet" label="Height (ft)" type="number" inputmode="numeric" />
          <AppInput
            v-model="heightInches"
            label="(in)"
            :error="errors.height"
            type="number"
            inputmode="numeric"
          />
        </div>

        <AppInput v-model="age" label="Age" :error="errors.age" type="number" inputmode="numeric" />
      </div>

      <!-- Selects -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium text-gray-700 dark:text-gray-200">Gender</label>
          <select v-model="gender" :class="selectClass">
            <option v-for="o in GENDERS" :key="o.value" :value="o.value">{{ o.label }}</option>
          </select>
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium text-gray-700 dark:text-gray-200">Activity level</label>
          <select v-model="activityLevel" :class="selectClass">
            <option v-for="o in ACTIVITY" :key="o.value" :value="o.value">{{ o.label }}</option>
          </select>
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium text-gray-700 dark:text-gray-200">Dietary goal</label>
          <select v-model="dietaryGoal" :class="selectClass">
            <option v-for="o in GOALS" :key="o.value" :value="o.value">{{ o.label }}</option>
          </select>
        </div>
      </div>

      <div class="flex flex-col gap-1">
        <label class="text-sm font-medium text-gray-700 dark:text-gray-200">Notes</label>
        <textarea
          v-model="notes"
          rows="3"
          placeholder="Allergies, dislikes, anything the meal planner should know"
          :class="selectClass"
        />
      </div>

      <p class="text-xs text-gray-500 dark:text-gray-400">
        Changing your body stats or goal recalculates your daily calorie and macro targets.
      </p>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2">
        <AppButton variant="outline" @click="emit('update:modelValue', false)">Cancel</AppButton>
        <AppButton :loading="profileStore.isLoading" @click="save">Save changes</AppButton>
      </div>
    </template>
  </AppModal>
</template>
