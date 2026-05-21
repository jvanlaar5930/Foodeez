<script setup lang="ts">
import { ref, computed } from 'vue';
import { RouterLink } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { useRouter } from 'vue-router';
import AppButton from '@/components/ui/AppButton.vue';
import AppInput from '@/components/ui/AppInput.vue';

const authStore = useAuthStore();
const router = useRouter();

const email = ref('');
const password = ref('');
const submitted = ref(false);

const emailError = computed(() => {
  if (!submitted.value) return '';
  if (!email.value) return 'Email is required';
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value)) return 'Enter a valid email';
  return '';
});

const passwordError = computed(() => {
  if (!submitted.value) return '';
  if (!password.value) return 'Password is required';
  return '';
});

const isFormValid = computed(() => !emailError.value && !passwordError.value && !!email.value && !!password.value);

async function handleSubmit() {
  submitted.value = true;
  if (!isFormValid.value) return;

  try {
    await authStore.login({ email: email.value, password: password.value });
    if (!authStore.user?.profileCompleted) {
      await router.push('/auth/setup');
    } else {
      await router.push('/dashboard');
    }
  } catch {
    // error displayed from store
  }
}
</script>

<template>
  <div class="min-h-screen bg-gradient-to-br from-green-50 to-emerald-100 flex items-center justify-center p-4">
    <div class="w-full max-w-sm">
      <!-- Logo -->
      <div class="text-center mb-8">
        <div class="inline-flex items-center gap-2 mb-2">
          <span class="text-4xl">🥦</span>
          <span class="text-3xl font-bold text-green-700">Foodeez</span>
        </div>
        <p class="text-gray-500 text-sm">Track your nutrition, reach your goals</p>
      </div>

      <!-- Card -->
      <div class="bg-white rounded-2xl shadow-lg p-8">
        <h1 class="text-2xl font-bold text-gray-900 mb-1">Welcome back</h1>
        <p class="text-sm text-gray-500 mb-6">Sign in to your account</p>

        <!-- Error alert -->
        <div
          v-if="authStore.error"
          class="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700"
        >
          {{ authStore.error }}
        </div>

        <form @submit.prevent="handleSubmit" class="space-y-4">
          <AppInput
            v-model="email"
            label="Email address"
            type="email"
            placeholder="you@example.com"
            autocomplete="email"
            :error="emailError"
          />
          <AppInput
            v-model="password"
            label="Password"
            type="password"
            placeholder="••••••••"
            autocomplete="current-password"
            :error="passwordError"
          />

          <AppButton
            type="submit"
            class="w-full justify-center mt-2"
            :loading="authStore.isLoading"
          >
            Sign in
          </AppButton>
        </form>
      </div>

      <p class="text-center text-sm text-gray-500 mt-4">
        Don't have an account?
        <RouterLink to="/auth/register" class="text-green-600 font-medium hover:underline">
          Create one
        </RouterLink>
      </p>
    </div>
  </div>
</template>
