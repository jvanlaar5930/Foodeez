<template>
  <div class="min-h-screen bg-gray-50 flex items-center justify-center p-4">
    <div class="w-full max-w-lg">
      <!-- Progress -->
      <div class="mb-6">
        <div class="flex items-center justify-between mb-2">
          <span class="text-sm text-gray-500">Step {{ currentStep }} of {{ TOTAL_STEPS }}</span>
          <span class="text-sm font-medium text-green-600">{{ Math.round((currentStep / TOTAL_STEPS) * 100) }}%</span>
        </div>
        <div class="h-2 bg-gray-200 rounded-full">
          <div class="h-2 bg-green-500 rounded-full transition-all duration-300" :style="{ width: `${(currentStep / TOTAL_STEPS) * 100}%` }" />
        </div>
      </div>

      <div class="bg-white rounded-2xl shadow-sm p-8">
        <!-- Step 1: Basic info -->
        <div v-show="currentStep === 1">
          <h2 class="text-2xl font-bold text-gray-900 mb-1">Tell us about yourself</h2>
          <p class="text-gray-500 mb-6">This helps us calculate your personalized nutrition targets.</p>
          <div class="space-y-5">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Height (cm)</label>
                <input v-model.number="profile.heightCm" type="number" min="100" max="250" class="input" placeholder="175" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Current Weight (kg)</label>
                <input v-model.number="profile.weightKg" type="number" min="30" max="300" class="input" placeholder="70" />
              </div>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Target Weight (kg)</label>
                <input v-model.number="profile.targetWeightKg" type="number" min="30" max="300" class="input" placeholder="65 (optional)" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Age</label>
                <input v-model.number="profile.age" type="number" min="13" max="120" class="input" placeholder="28" />
              </div>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">Gender</label>
              <div class="grid grid-cols-2 gap-2">
                <button v-for="g in GENDERS" :key="g.value" type="button"
                  class="py-2.5 px-4 rounded-lg border-2 text-sm font-medium transition-colors"
                  :class="profile.gender === g.value ? 'border-green-500 bg-green-50 text-green-700' : 'border-gray-200 text-gray-600 hover:border-gray-300'"
                  @click="profile.gender = g.value">
                  {{ g.label }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Step 2: Dietary goal -->
        <div v-show="currentStep === 2">
          <h2 class="text-2xl font-bold text-gray-900 mb-1">What's your goal?</h2>
          <p class="text-gray-500 mb-6">Choose what best describes what you want to achieve.</p>
          <div class="space-y-3">
            <button v-for="goal in GOALS" :key="goal.value" type="button"
              class="w-full text-left p-4 rounded-xl border-2 transition-colors flex items-start gap-3"
              :class="profile.dietaryGoal === goal.value ? 'border-green-500 bg-green-50' : 'border-gray-200 hover:border-gray-300'"
              @click="profile.dietaryGoal = goal.value">
              <span class="text-2xl">{{ goal.icon }}</span>
              <div>
                <p class="font-semibold text-gray-900">{{ goal.label }}</p>
                <p class="text-sm text-gray-500 mt-0.5">{{ goal.description }}</p>
              </div>
              <div class="ml-auto mt-1">
                <div class="w-5 h-5 rounded-full border-2 flex items-center justify-center"
                  :class="profile.dietaryGoal === goal.value ? 'border-green-500 bg-green-500' : 'border-gray-300'">
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
          <h2 class="text-2xl font-bold text-gray-900 mb-1">How active are you?</h2>
          <p class="text-gray-500 mb-6">This helps us estimate your daily calorie burn.</p>
          <div class="space-y-3">
            <button v-for="level in ACTIVITY_LEVELS" :key="level.value" type="button"
              class="w-full text-left p-4 rounded-xl border-2 transition-colors"
              :class="profile.activityLevel === level.value ? 'border-green-500 bg-green-50' : 'border-gray-200 hover:border-gray-300'"
              @click="profile.activityLevel = level.value">
              <p class="font-semibold text-gray-900">{{ level.label }}</p>
              <p class="text-sm text-gray-500 mt-0.5">{{ level.description }}</p>
            </button>
          </div>
        </div>

        <!-- Step 4: Summary -->
        <div v-show="currentStep === 4">
          <h2 class="text-2xl font-bold text-gray-900 mb-1">Your Daily Targets</h2>
          <p class="text-gray-500 mb-6">Based on your profile, here are your recommended daily nutrition targets.</p>

          <div class="bg-green-50 rounded-xl p-6 text-center mb-6">
            <p class="text-sm text-green-700 font-medium mb-1">Daily Calorie Target</p>
            <p class="text-5xl font-bold text-green-600">{{ estimatedCalories }}</p>
            <p class="text-green-600 text-sm mt-1">calories per day</p>
          </div>

          <div class="grid grid-cols-3 gap-3">
            <div class="bg-blue-50 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-blue-600">{{ estimatedProtein }}g</p>
              <p class="text-xs text-blue-600 font-medium mt-1">Protein</p>
            </div>
            <div class="bg-orange-50 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-orange-500">{{ estimatedCarbs }}g</p>
              <p class="text-xs text-orange-500 font-medium mt-1">Carbohydrates</p>
            </div>
            <div class="bg-yellow-50 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-yellow-600">{{ estimatedFat }}g</p>
              <p class="text-xs text-yellow-600 font-medium mt-1">Fat</p>
            </div>
          </div>

          <p class="text-xs text-gray-400 text-center mt-4">
            These targets can be adjusted anytime from your profile settings.
          </p>
        </div>

        <!-- Navigation -->
        <div class="flex gap-3 mt-8">
          <button v-if="currentStep > 1" type="button"
            class="flex-1 py-2.5 border-2 border-gray-200 rounded-lg font-semibold text-gray-600 hover:border-gray-300 transition-colors"
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
            <span v-if="isLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
            {{ isLoading ? 'Saving...' : 'Complete Setup' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { useProfileStore } from '@/stores/profile';
import { Gender, ActivityLevel, DietaryGoal } from '@foodeez/shared';

const router = useRouter();
const authStore = useAuthStore();
const profileStore = useProfileStore();

const TOTAL_STEPS = 4;
const currentStep = ref(1);
const isLoading = ref(false);

const profile = ref({
  heightCm: 175,
  weightKg: 70,
  targetWeightKg: undefined as number | undefined,
  age: 25,
  gender: Gender.Male as Gender,
  dietaryGoal: DietaryGoal.GeneralHealth as DietaryGoal,
  activityLevel: ActivityLevel.ModeratelyActive as ActivityLevel,
  notes: '',
});

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

const ACTIVITY_MULTIPLIERS: Record<ActivityLevel, number> = {
  [ActivityLevel.Sedentary]: 1.2,
  [ActivityLevel.LightlyActive]: 1.375,
  [ActivityLevel.ModeratelyActive]: 1.55,
  [ActivityLevel.VeryActive]: 1.725,
  [ActivityLevel.ExtraActive]: 1.9,
};

const GOAL_ADJUSTMENTS: Record<DietaryGoal, number> = {
  [DietaryGoal.WeightLoss]: -500,
  [DietaryGoal.WeightMaintenance]: 0,
  [DietaryGoal.WeightGain]: 300,
  [DietaryGoal.MuscleGain]: 250,
  [DietaryGoal.GeneralHealth]: 0,
};

const estimatedCalories = computed(() => {
  const { heightCm, weightKg, age, gender, activityLevel, dietaryGoal } = profile.value;
  const bmr = gender === Gender.Female
    ? 10 * weightKg + 6.25 * heightCm - 5 * age - 161
    : 10 * weightKg + 6.25 * heightCm - 5 * age + 5;
  const tdee = bmr * ACTIVITY_MULTIPLIERS[activityLevel];
  return Math.round(tdee + GOAL_ADJUSTMENTS[dietaryGoal]);
});

const estimatedProtein = computed(() => Math.round(profile.value.weightKg * 1.6));
const estimatedCarbs = computed(() => Math.round((estimatedCalories.value * 0.45) / 4));
const estimatedFat = computed(() => Math.round((estimatedCalories.value * 0.25) / 9));

function nextStep() {
  currentStep.value++;
}

async function handleComplete() {
  if (!authStore.user?.id) return;
  isLoading.value = true;
  try {
    await profileStore.updateProfile(authStore.user.id, profile.value);
    router.push('/dashboard');
  } catch {
    // Profile update failed — still navigate to dashboard
    router.push('/dashboard');
  } finally {
    isLoading.value = false;
  }
}
</script>

<style scoped>
.input {
  @apply w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500;
}
</style>
