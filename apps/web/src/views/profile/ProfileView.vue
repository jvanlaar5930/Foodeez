<template>
  <AppLayout>
    <div class="p-6 max-w-4xl mx-auto">
      <h1 class="text-2xl font-bold text-gray-900 mb-6">Profile</h1>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Left: user card -->
        <div class="lg:col-span-1 space-y-4">
          <div class="bg-white rounded-2xl shadow-sm p-6 text-center">
            <div class="w-20 h-20 rounded-full bg-green-600 flex items-center justify-center text-white text-2xl font-bold mx-auto mb-3">
              {{ initials }}
            </div>
            <p class="text-lg font-bold text-gray-900">{{ authStore.user?.firstName }} {{ authStore.user?.lastName }}</p>
            <p class="text-gray-500 text-sm">{{ authStore.user?.email }}</p>
            <button class="mt-4 w-full py-2 border-2 border-green-500 text-green-600 rounded-xl text-sm font-semibold hover:bg-green-50 transition-colors">
              Edit Profile
            </button>
          </div>

          <!-- Stats -->
          <div v-if="profile" class="bg-white rounded-2xl shadow-sm p-5 space-y-3">
            <h3 class="font-bold text-gray-900">Quick Stats</h3>
            <div v-for="stat in stats" :key="stat.label" class="flex justify-between text-sm">
              <span class="text-gray-500">{{ stat.label }}</span>
              <span class="font-semibold text-gray-900">{{ stat.value }}</span>
            </div>
          </div>
        </div>

        <!-- Right: details -->
        <div class="lg:col-span-2 space-y-5">
          <!-- Goals -->
          <div v-if="profile" class="bg-white rounded-2xl shadow-sm p-6">
            <h3 class="font-bold text-gray-900 mb-4">Goals & Activity</h3>
            <div class="grid grid-cols-2 gap-4">
              <div class="bg-green-50 rounded-xl p-4">
                <p class="text-xs font-semibold text-green-700 uppercase tracking-wide mb-1">Dietary Goal</p>
                <p class="font-bold text-gray-900">{{ GOAL_LABELS[profile.dietaryGoal] }}</p>
              </div>
              <div class="bg-orange-50 rounded-xl p-4">
                <p class="text-xs font-semibold text-orange-700 uppercase tracking-wide mb-1">Activity Level</p>
                <p class="font-bold text-gray-900">{{ ACTIVITY_LABELS[profile.activityLevel] }}</p>
              </div>
            </div>
          </div>

          <!-- Daily targets -->
          <div v-if="profile" class="bg-white rounded-2xl shadow-sm p-6">
            <h3 class="font-bold text-gray-900 mb-4">Daily Nutrition Targets</h3>
            <div class="space-y-3">
              <div class="flex items-center gap-3">
                <div class="w-24 text-sm text-gray-500">Calories</div>
                <div class="flex-1 h-3 bg-gray-100 rounded-full overflow-hidden">
                  <div class="h-full bg-green-500 rounded-full" style="width: 100%"></div>
                </div>
                <div class="text-sm font-semibold text-gray-900 w-20 text-right">{{ profile.dailyCalorieTarget }} kcal</div>
              </div>
              <div class="flex items-center gap-3">
                <div class="w-24 text-sm text-gray-500">Protein</div>
                <div class="flex-1 h-3 bg-gray-100 rounded-full overflow-hidden">
                  <div class="h-full bg-blue-500 rounded-full" style="width: 100%"></div>
                </div>
                <div class="text-sm font-semibold text-gray-900 w-20 text-right">{{ Math.round(profile.dailyProteinTargetG) }}g</div>
              </div>
              <div class="flex items-center gap-3">
                <div class="w-24 text-sm text-gray-500">Carbs</div>
                <div class="flex-1 h-3 bg-gray-100 rounded-full overflow-hidden">
                  <div class="h-full bg-orange-400 rounded-full" style="width: 100%"></div>
                </div>
                <div class="text-sm font-semibold text-gray-900 w-20 text-right">{{ Math.round(profile.dailyCarbTargetG) }}g</div>
              </div>
              <div class="flex items-center gap-3">
                <div class="w-24 text-sm text-gray-500">Fat</div>
                <div class="flex-1 h-3 bg-gray-100 rounded-full overflow-hidden">
                  <div class="h-full bg-yellow-400 rounded-full" style="width: 100%"></div>
                </div>
                <div class="text-sm font-semibold text-gray-900 w-20 text-right">{{ Math.round(profile.dailyFatTargetG) }}g</div>
              </div>
            </div>
          </div>

          <!-- Notifications -->
          <div class="bg-white rounded-2xl shadow-sm p-6">
            <h3 class="font-bold text-gray-900 mb-4">Notifications</h3>
            <div class="space-y-3">
              <div class="flex items-center justify-between py-2">
                <div>
                  <p class="text-sm font-medium text-gray-900">Weekly Weight Check-in</p>
                  <p class="text-xs text-gray-500 mt-0.5">Remind me to log my weight</p>
                </div>
                <button @click="weightReminder = !weightReminder"
                  class="relative w-11 h-6 rounded-full transition-colors"
                  :class="weightReminder ? 'bg-green-500' : 'bg-gray-200'">
                  <span class="absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform"
                    :class="weightReminder ? 'translate-x-5' : 'translate-x-0.5'" />
                </button>
              </div>
              <div class="border-t" />
              <div class="flex items-center justify-between py-2">
                <div>
                  <p class="text-sm font-medium text-gray-900">Meal Tracking Reminders</p>
                  <p class="text-xs text-gray-500 mt-0.5">Alert me if I haven't logged meals</p>
                </div>
                <button @click="trackingReminder = !trackingReminder"
                  class="relative w-11 h-6 rounded-full transition-colors"
                  :class="trackingReminder ? 'bg-green-500' : 'bg-gray-200'">
                  <span class="absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform"
                    :class="trackingReminder ? 'translate-x-5' : 'translate-x-0.5'" />
                </button>
              </div>
            </div>
          </div>

          <!-- Sign out -->
          <button @click="handleLogout"
            class="w-full py-3 border-2 border-red-300 text-red-500 hover:bg-red-50 rounded-2xl font-semibold text-sm transition-colors">
            Sign Out
          </button>
        </div>
      </div>
    </div>
  </AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import AppLayout from '@/components/layout/AppLayout.vue';
import { useAuthStore } from '@/stores/auth';
import { useProfileStore } from '@/stores/profile';
import { useAuth } from '@/composables/useAuth';
import { ActivityLevel, DietaryGoal } from '@foodeez/shared';

const authStore = useAuthStore();
const profileStore = useProfileStore();
const { handleLogout } = useAuth();

const weightReminder = ref(true);
const trackingReminder = ref(true);

const profile = computed(() => profileStore.profile);
const initials = computed(() => {
  const u = authStore.user;
  return u ? `${u.firstName[0]}${u.lastName[0]}`.toUpperCase() : '?';
});

const GOAL_LABELS: Record<DietaryGoal, string> = {
  [DietaryGoal.WeightLoss]: 'Weight Loss',
  [DietaryGoal.WeightMaintenance]: 'Maintain Weight',
  [DietaryGoal.WeightGain]: 'Weight Gain',
  [DietaryGoal.MuscleGain]: 'Muscle Gain',
  [DietaryGoal.GeneralHealth]: 'General Health',
};

const ACTIVITY_LABELS: Record<ActivityLevel, string> = {
  [ActivityLevel.Sedentary]: 'Sedentary',
  [ActivityLevel.LightlyActive]: 'Lightly Active',
  [ActivityLevel.ModeratelyActive]: 'Moderately Active',
  [ActivityLevel.VeryActive]: 'Very Active',
  [ActivityLevel.ExtraActive]: 'Extra Active',
};

const bmi = computed(() => {
  const p = profile.value;
  if (!p) return null;
  return (p.weightKg / Math.pow(p.heightCm / 100, 2)).toFixed(1);
});

const stats = computed(() => {
  const p = profile.value;
  if (!p) return [];
  return [
    { label: 'Height', value: `${p.heightCm} cm` },
    { label: 'Current Weight', value: `${p.weightKg} kg` },
    { label: 'Target Weight', value: p.targetWeightKg ? `${p.targetWeightKg} kg` : '—' },
    { label: 'Age', value: `${p.age} years` },
    { label: 'BMI', value: bmi.value ?? '—' },
    { label: 'Daily Calories', value: `${p.dailyCalorieTarget} kcal` },
  ];
});

onMounted(() => {
  if (authStore.user?.id) profileStore.fetchProfile(authStore.user.id);
});
</script>
