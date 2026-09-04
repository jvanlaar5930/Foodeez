<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-800 flex items-center justify-center p-4">
    <div class="w-full max-w-md">
      <div class="text-center mb-8">
        <h1 class="text-3xl font-bold text-green-600 dark:text-green-400">Foodeez</h1>
        <p class="text-gray-500 dark:text-gray-400 mt-1">Create your account</p>
      </div>

      <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-sm p-8">
        <form @submit.prevent="handleSubmit" class="space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">First Name</label>
              <input
                v-model="form.firstName"
                type="text"
                autocomplete="given-name"
                class="w-full border rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                :class="errors.firstName ? 'border-red-400' : 'border-gray-300 dark:border-gray-600'"
                placeholder="Jane"
              />
              <p v-if="errors.firstName" class="text-red-500 dark:text-red-400 text-xs mt-1">{{ errors.firstName }}</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Last Name</label>
              <input
                v-model="form.lastName"
                type="text"
                autocomplete="family-name"
                class="w-full border rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                :class="errors.lastName ? 'border-red-400' : 'border-gray-300 dark:border-gray-600'"
                placeholder="Doe"
              />
              <p v-if="errors.lastName" class="text-red-500 dark:text-red-400 text-xs mt-1">{{ errors.lastName }}</p>
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Email</label>
            <input
              v-model="form.email"
              type="email"
              autocomplete="email"
              class="w-full border rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
              :class="errors.email ? 'border-red-400' : 'border-gray-300 dark:border-gray-600'"
              placeholder="jane@example.com"
            />
            <p v-if="errors.email" class="text-red-500 dark:text-red-400 text-xs mt-1">{{ errors.email }}</p>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Password</label>
            <div class="relative">
              <input
                v-model="form.password"
                :type="showPassword ? 'text' : 'password'"
                autocomplete="new-password"
                class="w-full border rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 pr-10"
                :class="errors.password ? 'border-red-400' : 'border-gray-300 dark:border-gray-600'"
                placeholder="Min. 8 characters"
              />
              <button type="button" class="absolute right-3 top-2.5 text-gray-400" @click="showPassword = !showPassword">
                <span class="text-sm">{{ showPassword ? '🙈' : '👁' }}</span>
              </button>
            </div>
            <div v-if="form.password" class="mt-1">
              <div class="flex gap-1">
                <div v-for="i in 4" :key="i" class="h-1 flex-1 rounded" :class="passwordStrength >= i ? strengthColor : 'bg-gray-200 dark:bg-gray-700'" />
              </div>
              <p class="text-xs mt-1" :class="strengthTextColor">{{ strengthLabel }}</p>
            </div>
            <p v-if="errors.password" class="text-red-500 dark:text-red-400 text-xs mt-1">{{ errors.password }}</p>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Confirm Password</label>
            <input
              v-model="form.confirmPassword"
              :type="showPassword ? 'text' : 'password'"
              autocomplete="new-password"
              class="w-full border rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
              :class="errors.confirmPassword ? 'border-red-400' : 'border-gray-300 dark:border-gray-600'"
              placeholder="Repeat password"
            />
            <p v-if="errors.confirmPassword" class="text-red-500 dark:text-red-400 text-xs mt-1">{{ errors.confirmPassword }}</p>
          </div>

          <AppAlert v-if="apiError" variant="error" :message="apiError" />

          <button
            type="submit"
            :disabled="isLoading"
            class="w-full bg-green-600 hover:bg-green-700 disabled:bg-green-300 text-white font-semibold py-2.5 rounded-lg transition-colors flex items-center justify-center gap-2"
          >
            <span v-if="isLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
            {{ isLoading ? 'Creating account...' : 'Create Account' }}
          </button>
        </form>

        <p class="text-center text-sm text-gray-500 dark:text-gray-400 mt-6">
          Already have an account?
          <RouterLink to="/auth/login" class="text-green-600 dark:text-green-400 font-semibold ml-1">Sign in</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { extractErrorMessage } from '@/utils/apiError';
import { ref, computed } from 'vue';
import { RouterLink, useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import AppAlert from '@/components/ui/AppAlert.vue';

const router = useRouter();
const authStore = useAuthStore();

const form = ref({ firstName: '', lastName: '', email: '', password: '', confirmPassword: '' });
const errors = ref<Record<string, string>>({});
const isLoading = ref(false);
const apiError = ref('');
const showPassword = ref(false);

const passwordStrength = computed(() => {
  const p = form.value.password;
  let score = 0;
  if (p.length >= 8) score++;
  if (/[A-Z]/.test(p)) score++;
  if (/[0-9]/.test(p)) score++;
  if (/[^a-zA-Z0-9]/.test(p)) score++;
  return score;
});

const strengthColor = computed(() => {
  if (passwordStrength.value <= 1) return 'bg-red-400';
  if (passwordStrength.value === 2) return 'bg-yellow-400';
  if (passwordStrength.value === 3) return 'bg-blue-400';
  return 'bg-green-500';
});
const strengthTextColor = computed(() => {
  if (passwordStrength.value <= 1) return 'text-red-500 dark:text-red-400';
  if (passwordStrength.value === 2) return 'text-yellow-600 dark:text-yellow-400';
  if (passwordStrength.value === 3) return 'text-blue-500';
  return 'text-green-600 dark:text-green-400';
});
const strengthLabel = computed(() => ['', 'Weak', 'Fair', 'Good', 'Strong'][passwordStrength.value]);

function validate() {
  const e: Record<string, string> = {};
  if (!form.value.firstName.trim()) e.firstName = 'First name is required';
  if (!form.value.lastName.trim()) e.lastName = 'Last name is required';
  if (!form.value.email.match(/^[^\s@]+@[^\s@]+\.[^\s@]+$/)) e.email = 'Valid email required';
  if (form.value.password.length < 8) e.password = 'Password must be at least 8 characters';
  if (form.value.password !== form.value.confirmPassword) e.confirmPassword = 'Passwords do not match';
  errors.value = e;
  return Object.keys(e).length === 0;
}

async function handleSubmit() {
  if (!validate()) return;
  isLoading.value = true;
  apiError.value = '';
  try {
    await authStore.register({
      firstName: form.value.firstName,
      lastName: form.value.lastName,
      email: form.value.email,
      password: form.value.password,
    });
    router.push('/auth/setup');
  } catch (err: unknown) {
    apiError.value = extractErrorMessage(err, 'Registration failed. Please try again.');
  } finally {
    isLoading.value = false;
  }
}
</script>
