<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-800 flex items-center justify-center p-4">
    <div class="w-full max-w-md">
      <div class="text-center mb-8">
        <h1 class="text-3xl font-bold text-green-600 dark:text-green-400">Foodeez</h1>
        <p class="text-gray-500 dark:text-gray-400 mt-1">Create your account</p>
      </div>

      <AppCard padding="lg">
        <form @submit.prevent="handleSubmit" class="space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <AppInput
              v-model="form.firstName"
              label="First Name"
              autocomplete="given-name"
              placeholder="Jane"
              :error="errors.firstName"
            />
            <AppInput
              v-model="form.lastName"
              label="Last Name"
              autocomplete="family-name"
              placeholder="Doe"
              :error="errors.lastName"
            />
          </div>

          <AppInput
            v-model="form.email"
            label="Email"
            type="email"
            autocomplete="email"
            placeholder="jane@example.com"
            :error="errors.email"
          />

          <AppInput
            v-model="form.password"
            label="Password"
            :type="showPassword ? 'text' : 'password'"
            autocomplete="new-password"
            placeholder="Min. 8 characters"
            :error="errors.password"
          >
            <template #suffix>
              <button
                type="button"
                class="text-gray-400"
                :aria-label="showPassword ? 'Hide password' : 'Show password'"
                @click="showPassword = !showPassword"
              >
                <span class="text-sm" aria-hidden="true">{{ showPassword ? '🙈' : '👁' }}</span>
              </button>
            </template>

            <template #below>
              <div v-if="form.password">
                <div class="flex gap-1">
                  <div
                    v-for="i in 4"
                    :key="i"
                    class="h-1 flex-1 rounded"
                    :class="passwordStrength >= i ? strengthColor : 'bg-gray-200 dark:bg-gray-700'"
                  />
                </div>
                <p class="mt-1 text-xs" :class="strengthTextColor">{{ strengthLabel }}</p>
              </div>
            </template>
          </AppInput>

          <AppInput
            v-model="form.confirmPassword"
            label="Confirm Password"
            :type="showPassword ? 'text' : 'password'"
            autocomplete="new-password"
            placeholder="Repeat password"
            :error="errors.confirmPassword"
          />

          <AppAlert v-if="apiError" variant="error" :message="apiError" />

          <AppButton type="submit" size="lg" :loading="isLoading" class="w-full">
            {{ isLoading ? 'Creating account...' : 'Create Account' }}
          </AppButton>
        </form>

        <p class="text-center text-sm text-gray-500 dark:text-gray-400 mt-6">
          Already have an account?
          <RouterLink to="/auth/login" class="text-green-600 dark:text-green-400 font-semibold ml-1">Sign in</RouterLink>
        </p>
      </AppCard>
    </div>
  </div>
</template>

<script setup lang="ts">
import AppButton from '@/components/ui/AppButton.vue';
import AppCard from '@/components/ui/AppCard.vue';
import AppInput from '@/components/ui/AppInput.vue';
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
