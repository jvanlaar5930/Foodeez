<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-800 flex items-center justify-center p-4">
    <div class="w-full max-w-lg">
      <!-- Progress -->
      <div class="mb-6">
        <div class="flex items-center justify-between mb-2">
          <span class="text-sm text-gray-500 dark:text-gray-400">Step {{ currentStep }} of {{ TOTAL_STEPS }}</span>
          <span class="text-sm font-medium text-green-600 dark:text-green-400">{{ Math.round((currentStep / TOTAL_STEPS) * 100) }}%</span>
        </div>
        <div class="h-2 bg-gray-200 dark:bg-gray-700 rounded-full">
          <div class="h-2 bg-green-500 rounded-full transition-all duration-300" :style="{ width: `${(currentStep / TOTAL_STEPS) * 100}%` }" />
        </div>
      </div>

      <AppCard padding="lg">
        <!-- Step 1: Basic info -->
        <div v-show="currentStep === 1">
          <div class="flex items-start justify-between mb-1">
            <h2 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Tell us about yourself</h2>
            <!-- Unit toggle -->
            <div class="flex items-center bg-gray-100 dark:bg-gray-800 rounded-lg p-0.5 gap-0.5 flex-shrink-0 ml-4">
              <button type="button"
                class="px-3 py-1.5 rounded-md text-sm font-semibold transition-colors"
                :class="useMetric ? 'bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 shadow-sm' : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'"
                @click="setUnitSystem(true)">
                Metric
              </button>
              <button type="button"
                class="px-3 py-1.5 rounded-md text-sm font-semibold transition-colors"
                :class="!useMetric ? 'bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 shadow-sm' : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'"
                @click="setUnitSystem(false)">
                US
              </button>
            </div>
          </div>
          <p class="text-gray-500 dark:text-gray-400 mb-6">This helps us calculate your personalized nutrition targets.</p>

          <div class="space-y-5">
            <!-- Height -->
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Height</label>
              <!-- Metric -->
              <div v-if="useMetric" class="flex items-center gap-2">
                <input v-model.number="displayHeightCm" type="number" min="100" max="250" class="input" placeholder="175" @change="onHeightCmChange" />
                <span class="text-sm text-gray-500 dark:text-gray-400 flex-shrink-0">cm</span>
              </div>
              <!-- Imperial -->
              <div v-else class="flex items-center gap-2">
                <input v-model.number="displayFeet" type="number" min="3" max="8" class="input" placeholder="5" @change="onImperialHeightChange" />
                <span class="text-sm text-gray-500 dark:text-gray-400 flex-shrink-0">ft</span>
                <input v-model.number="displayInches" type="number" min="0" max="11" class="input" placeholder="10" @change="onImperialHeightChange" />
                <span class="text-sm text-gray-500 dark:text-gray-400 flex-shrink-0">in</span>
              </div>
            </div>

            <!-- Current weight -->
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Current Weight</label>
              <div class="flex items-center gap-2">
                <input v-if="useMetric" v-model.number="displayWeightKg" type="number" min="30" max="300" step="0.1" class="input" placeholder="70" @change="onWeightKgChange" />
                <input v-else v-model.number="displayWeightLbs" type="number" min="66" max="660" step="0.5" class="input" placeholder="154" @change="onWeightLbsChange" />
                <span class="text-sm text-gray-500 dark:text-gray-400 flex-shrink-0">{{ useMetric ? 'kg' : 'lbs' }}</span>
              </div>
            </div>

            <!-- Target weight -->
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Target Weight <span class="text-gray-400 font-normal">(optional)</span></label>
              <div class="flex items-center gap-2">
                <input v-if="useMetric" v-model.number="displayTargetKg" type="number" min="30" max="300" step="0.1" class="input" :placeholder="useMetric ? 'e.g. 65' : 'e.g. 143'" @change="onTargetKgChange" />
                <input v-else v-model.number="displayTargetLbs" type="number" min="66" max="660" step="0.5" class="input" placeholder="e.g. 143" @change="onTargetLbsChange" />
                <span class="text-sm text-gray-500 dark:text-gray-400 flex-shrink-0">{{ useMetric ? 'kg' : 'lbs' }}</span>
              </div>
            </div>

            <!-- Age -->
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Age</label>
              <div class="flex items-center gap-2">
                <input v-model.number="profile.age" type="number" min="13" max="120" class="input" placeholder="28" />
                <span class="text-sm text-gray-500 dark:text-gray-400 flex-shrink-0">years</span>
              </div>
            </div>

            <!-- Gender -->
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-2">Gender</label>
              <div class="grid grid-cols-2 gap-2">
                <button v-for="g in GENDERS" :key="g.value" type="button"
                  class="py-2.5 px-4 rounded-lg border-2 text-sm font-medium transition-colors"
                  :class="profile.gender === g.value ? 'border-green-500 bg-green-50 dark:bg-green-950/40 text-green-700 dark:text-green-400' : 'border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'"
                  @click="profile.gender = g.value">
                  {{ g.label }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Step 2: Dietary goal -->
        <div v-show="currentStep === 2">
          <h2 class="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-1">What's your goal?</h2>
          <p class="text-gray-500 dark:text-gray-400 mb-6">Choose what best describes what you want to achieve.</p>
          <div class="space-y-3">
            <button v-for="goal in GOALS" :key="goal.value" type="button"
              class="w-full text-left p-4 rounded-xl border-2 transition-colors flex items-start gap-3"
              :class="profile.dietaryGoal === goal.value ? 'border-green-500 bg-green-50 dark:bg-green-950/40' : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'"
              @click="profile.dietaryGoal = goal.value">
              <span class="text-2xl">{{ goal.icon }}</span>
              <div>
                <p class="font-semibold text-gray-900 dark:text-gray-100">{{ goal.label }}</p>
                <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{{ goal.description }}</p>
              </div>
              <div class="ml-auto mt-1">
                <div class="w-5 h-5 rounded-full border-2 flex items-center justify-center"
                  :class="profile.dietaryGoal === goal.value ? 'border-green-500 bg-green-500' : 'border-gray-300 dark:border-gray-600'">
                  <svg v-if="profile.dietaryGoal === goal.value" class="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 12 12">
                    <path d="M10 3L5 8.5 2 5.5" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round"/>
                  </svg>
                </div>
              </div>
            </button>
          </div>
        </div>

        <!-- Step 3: Activity level -->
        <div v-show="currentStep === 3">
          <h2 class="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-1">How active are you?</h2>
          <p class="text-gray-500 dark:text-gray-400 mb-6">This helps us estimate your daily calorie burn.</p>
          <div class="space-y-3">
            <button v-for="level in ACTIVITY_LEVELS" :key="level.value" type="button"
              class="w-full text-left p-4 rounded-xl border-2 transition-colors"
              :class="profile.activityLevel === level.value ? 'border-green-500 bg-green-50 dark:bg-green-950/40' : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'"
              @click="profile.activityLevel = level.value">
              <p class="font-semibold text-gray-900 dark:text-gray-100">{{ level.label }}</p>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{{ level.description }}</p>
            </button>
          </div>
        </div>

        <!-- Step 4: Foods to avoid -->
        <div v-show="currentStep === 4">
          <h2 class="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-1">Anything you can't eat?</h2>
          <p class="text-gray-500 dark:text-gray-400 mb-6">
            Allergies, intolerances, or foods you simply do not want. Nothing we suggest - meal plans,
            meal ideas, daily advice - will include them. You can change this any time.
          </p>

          <FoodExclusionsInput v-model="profile.excludedFoods" />

          <p class="mt-4 text-xs text-gray-400">
            Leave this empty if there is nothing to avoid. This is not a substitute for medical advice:
            always check labels yourself if an allergy is severe.
          </p>
        </div>

        <!-- Step 5: Summary -->
        <div v-show="currentStep === 5">
          <h2 class="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-1">Your Daily Targets</h2>
          <p class="text-gray-500 dark:text-gray-400 mb-6">Based on your profile, here are your recommended daily nutrition targets.</p>

          <div class="bg-green-50 dark:bg-green-950/40 rounded-xl p-6 text-center mb-6">
            <p class="text-sm text-green-700 dark:text-green-400 font-medium mb-1">Daily Calorie Target</p>
            <p class="text-5xl font-bold text-green-600 dark:text-green-400">{{ estimatedCalories }}</p>
            <p class="text-green-600 dark:text-green-400 text-sm mt-1">calories per day</p>
          </div>

          <div class="grid grid-cols-3 gap-3">
            <div class="bg-blue-50 dark:bg-blue-950/40 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-blue-600 dark:text-blue-400">{{ estimatedProtein }}g</p>
              <p class="text-xs text-blue-600 dark:text-blue-400 font-medium mt-1">Protein</p>
            </div>
            <div class="bg-orange-50 dark:bg-orange-950/40 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-orange-500 dark:text-orange-400">{{ estimatedCarbs }}g</p>
              <p class="text-xs text-orange-500 dark:text-orange-400 font-medium mt-1">Carbohydrates</p>
            </div>
            <div class="bg-yellow-50 dark:bg-yellow-950/40 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-yellow-600 dark:text-yellow-400">{{ estimatedFat }}g</p>
              <p class="text-xs text-yellow-600 dark:text-yellow-400 font-medium mt-1">Fat</p>
            </div>
          </div>

          <div class="mt-5">
            <button type="button"
              class="text-xs text-gray-400 underline underline-offset-2 hover:text-gray-500 dark:hover:text-gray-400 transition-colors"
              @click="showCalculationInfo = !showCalculationInfo">
              {{ showCalculationInfo ? 'Hide calculation details ▲' : 'How are these calculated? ▼' }}
            </button>
            <div v-if="showCalculationInfo" class="mt-2 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4 text-xs text-gray-500 dark:text-gray-400 space-y-1">
              <p>
                Calorie target is derived using the
                <span class="font-medium text-gray-700 dark:text-gray-200">Mifflin-St Jeor equation</span> for resting metabolic rate,
                adjusted by your activity level using
                <span class="font-medium text-gray-700 dark:text-gray-200">Harris-Benedict activity multipliers</span>,
                and further modified for your goal.
              </p>
              <p>
                Macro targets follow
                <span class="font-medium text-gray-700 dark:text-gray-200">Dietary Reference Intakes (DRI)</span>
                guidelines from the
                <span class="font-medium text-gray-700 dark:text-gray-200">National Academies of Medicine</span>.
              </p>
              <p class="text-gray-400 pt-1">
                These are general estimates and not a substitute for advice from a registered dietitian or physician.
                Targets can be adjusted anytime from your profile settings.
              </p>
            </div>
          </div>
        </div>

        <!-- Legal disclaimer — shown only on final step -->
        <div v-if="currentStep === TOTAL_STEPS" class="mt-5">
          <button type="button"
            class="text-xs text-gray-400 underline underline-offset-2 hover:text-gray-500 dark:hover:text-gray-400 transition-colors"
            @click="showDisclaimer = !showDisclaimer">
            {{ showDisclaimer ? 'Hide disclaimer ▲' : 'View disclaimer ▼' }}
          </button>
          <div v-if="showDisclaimer" class="mt-2 text-xs text-gray-400 leading-relaxed border border-gray-100 dark:border-gray-800 rounded-lg p-3 bg-gray-50 dark:bg-gray-800">
            By clicking <span class="font-medium text-gray-500 dark:text-gray-400">Complete Setup</span> you acknowledge that the
            nutrition targets and recommendations provided by Foodeez are generated algorithmically for
            informational purposes only and do not constitute medical advice, diagnosis, or treatment.
            Foodeez and its affiliates make no warranties, express or implied, regarding the accuracy,
            completeness, or suitability of these targets for your individual health circumstances.
            You assume full responsibility for any dietary changes you make based on this information.
            Always consult a qualified healthcare provider or registered dietitian before making significant
            changes to your diet, especially if you have a medical condition, are pregnant, or are under
            the age of 18.
          </div>
        </div>

        <AppAlert v-if="setupError" variant="error" :message="setupError" class="mt-4" />

        <!-- Navigation -->
        <div class="flex gap-3 mt-4">
          <button v-if="currentStep > 1" type="button"
            class="flex-1 py-2.5 border-2 border-gray-200 dark:border-gray-700 rounded-lg font-semibold text-gray-600 dark:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600 transition-colors"
            @click="currentStep--">
            Back
          </button>
          <button v-if="currentStep < TOTAL_STEPS" type="button"
            class="flex-1 py-2.5 bg-green-600 hover:bg-green-700 text-white rounded-lg font-semibold transition-colors"
            @click="nextStep">
            Continue
          </button>
          <button v-else type="button"
            :disabled="isLoading"
            class="flex-1 py-2.5 bg-green-600 hover:bg-green-700 disabled:bg-green-300 text-white rounded-lg font-semibold transition-colors flex items-center justify-center gap-2"
            @click="handleComplete">
            <LoadingSpinner v-if="isLoading" size="sm" color="currentColor" />
            {{ isLoading ? 'Saving...' : 'Complete Setup' }}
          </button>
        </div>
      </AppCard>
    </div>
  </div>
</template>

<script setup lang="ts">
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import AppCard from '@/components/ui/AppCard.vue';
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import FoodExclusionsInput from '@/components/profile/FoodExclusionsInput.vue';
import { useAuthStore } from '@/stores/auth';
import { useProfileStore } from '@/stores/profile';
import { Gender, ActivityLevel, DietaryGoal, calculateTargets } from '@foodeez/shared';
import AppAlert from '@/components/ui/AppAlert.vue';

const router = useRouter();
const authStore = useAuthStore();
const profileStore = useProfileStore();

const TOTAL_STEPS = 5;
const currentStep = ref(1);
const isLoading = ref(false);
const setupError = ref('');
const useMetric = ref(true);
const showDisclaimer = ref(false);
const showCalculationInfo = ref(false);

// Profile always stored in metric (cm / kg) internally
const profile = ref({
  heightCm: 175,
  weightKg: 70,
  targetWeightKg: undefined as number | undefined,
  age: 25,
  gender: Gender.Male as Gender,
  dietaryGoal: DietaryGoal.GeneralHealth as DietaryGoal,
  activityLevel: ActivityLevel.ModeratelyActive as ActivityLevel,
  notes: '',
  excludedFoods: [] as string[],
});

// --- Conversion helpers ---
const cmToFeetInches = (cm: number) => {
  const totalInches = cm / 2.54;
  return { feet: Math.floor(totalInches / 12), inches: Math.round(totalInches % 12) };
};
const feetInchesToCm = (feet: number, inches: number) => Math.round((feet * 12 + inches) * 2.54);
const kgToLbs = (kg: number) => Math.round(kg * 2.20462 * 10) / 10;
const lbsToKg = (lbs: number) => Math.round(lbs / 2.20462 * 10) / 10;

// --- Imperial display refs (only used when useMetric === false) ---
const displayFeet = ref(5);
const displayInches = ref(9);
const displayWeightLbs = ref(kgToLbs(70));
const displayTargetLbs = ref<number | undefined>(undefined);

// Metric display refs (two-way bound to profile except during unit switching)
const displayHeightCm = ref(175);
const displayWeightKg = ref(70);
const displayTargetKg = ref<number | undefined>(undefined);

function setUnitSystem(metric: boolean) {
  if (metric === useMetric.value) return;
  if (metric) {
    // Imperial → Metric
    profile.value.heightCm = feetInchesToCm(displayFeet.value, displayInches.value);
    profile.value.weightKg = lbsToKg(displayWeightLbs.value);
    profile.value.targetWeightKg = displayTargetLbs.value != null ? lbsToKg(displayTargetLbs.value) : undefined;
    displayHeightCm.value = profile.value.heightCm;
    displayWeightKg.value = profile.value.weightKg;
    displayTargetKg.value = profile.value.targetWeightKg;
  } else {
    // Metric → Imperial
    const { feet, inches } = cmToFeetInches(profile.value.heightCm);
    displayFeet.value = feet;
    displayInches.value = inches;
    displayWeightLbs.value = kgToLbs(profile.value.weightKg);
    displayTargetLbs.value = profile.value.targetWeightKg != null ? kgToLbs(profile.value.targetWeightKg) : undefined;
  }
  useMetric.value = metric;
}

// Metric input handlers
function onHeightCmChange() { profile.value.heightCm = displayHeightCm.value; }
function onWeightKgChange() { profile.value.weightKg = displayWeightKg.value; }
function onTargetKgChange() { profile.value.targetWeightKg = displayTargetKg.value || undefined; }

// Imperial input handlers
function onImperialHeightChange() { profile.value.heightCm = feetInchesToCm(displayFeet.value, displayInches.value); }
function onWeightLbsChange() { profile.value.weightKg = lbsToKg(displayWeightLbs.value); }
function onTargetLbsChange() { profile.value.targetWeightKg = displayTargetLbs.value != null ? lbsToKg(displayTargetLbs.value) : undefined; }

const GENDERS = [
  { value: Gender.Male, label: 'Male' },
  { value: Gender.Female, label: 'Female' },
  { value: Gender.Other, label: 'Non-binary / Other' },
  { value: Gender.PreferNotToSay, label: 'Prefer not to say' },
];

const GOALS = [
  { value: DietaryGoal.WeightLoss, icon: '🔥', label: 'Lose Weight', description: 'Reduce body fat with a calorie deficit and balanced nutrition' },
  { value: DietaryGoal.WeightMaintenance, icon: '⚖️', label: 'Maintain Weight', description: 'Keep your current weight stable with balanced eating' },
  { value: DietaryGoal.WeightGain, icon: '📈', label: 'Gain Weight', description: 'Build mass with a healthy calorie surplus' },
  { value: DietaryGoal.MuscleGain, icon: '💪', label: 'Build Muscle', description: 'Maximize muscle growth with high-protein meal plans' },
  { value: DietaryGoal.GeneralHealth, icon: '🥗', label: 'Eat Healthier', description: 'Improve overall nutrition and wellness habits' },
];

const ACTIVITY_LEVELS = [
  { value: ActivityLevel.Sedentary, label: 'Sedentary', description: 'Desk job, little or no exercise' },
  { value: ActivityLevel.LightlyActive, label: 'Lightly Active', description: 'Light exercise 1–3 days/week' },
  { value: ActivityLevel.ModeratelyActive, label: 'Moderately Active', description: 'Moderate exercise 3–5 days/week' },
  { value: ActivityLevel.VeryActive, label: 'Very Active', description: 'Hard exercise 6–7 days/week' },
  { value: ActivityLevel.ExtraActive, label: 'Extra Active', description: 'Very hard exercise + physical job' },
];

// The same calculation the server runs on save, so this preview is what actually gets
// stored. Previously this screen had its own formula and showed numbers the profile page
// then contradicted.
const estimatedTargets = computed(() => calculateTargets(profile.value));

const estimatedCalories = computed(() => estimatedTargets.value.calories);
const estimatedProtein = computed(() => Math.round(estimatedTargets.value.proteinG));
const estimatedCarbs = computed(() => Math.round(estimatedTargets.value.carbsG));
const estimatedFat = computed(() => Math.round(estimatedTargets.value.fatG));

function nextStep() {
  currentStep.value++;
}

async function handleComplete() {
  setupError.value = '';

  if (!authStore.user?.id) {
    setupError.value = 'Your session has expired. Please sign in again.';
    setTimeout(() => router.push('/auth/login'), 2000);
    return;
  }

  isLoading.value = true;
  try {
    await profileStore.updateProfile(authStore.user.id, profile.value);
    router.push('/dashboard');
  } catch {
    setupError.value = 'Failed to save your profile. Please try again.';
  } finally {
    isLoading.value = false;
  }
}
</script>

<style scoped>
@reference "tailwindcss";

.input {
  @apply w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500;
}
</style>
