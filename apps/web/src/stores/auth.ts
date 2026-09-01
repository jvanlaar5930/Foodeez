import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { authService } from '@/services/authService';
import { setAuthStore } from '@/services/api';
import type { LoginRequest, RegisterRequest, User } from '@foodeez/shared';

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null);
  const token = ref<string | null>(localStorage.getItem('foodeez_token'));
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const isAuthenticated = computed(() => !!token.value && !!user.value);
  const isAdmin = computed(() => user.value?.isAdmin ?? false);

  // Wire up the api instance so it can read our token
  setAuthStore({
    get token() {
      return token.value;
    },
    logout,
  });

  async function login(data: LoginRequest): Promise<void> {
    isLoading.value = true;
    error.value = null;
    try {
      const response = await authService.login(data);
      token.value = response.token;
      user.value = response.user;
      localStorage.setItem('foodeez_token', response.token);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  async function register(data: RegisterRequest): Promise<void> {
    isLoading.value = true;
    error.value = null;
    try {
      const response = await authService.register(data);
      token.value = response.token;
      user.value = response.user;
      localStorage.setItem('foodeez_token', response.token);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  function logout(): void {
    user.value = null;
    token.value = null;
    localStorage.removeItem('foodeez_token');
  }

  async function fetchCurrentUser(): Promise<void> {
    if (!token.value) return;
    try {
      const u = await authService.getCurrentUser();
      user.value = u;
    } catch {
      logout();
      throw new Error('Session expired');
    }
  }

  function extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'response' in err) {
      const e = err as { response?: { data?: { detail?: string; title?: string } } };
      return e.response?.data?.detail ?? e.response?.data?.title ?? 'An error occurred';
    }
    return 'An error occurred';
  }

  return {
    user,
    token,
    isAuthenticated,
    isAdmin,
    isLoading,
    error,
    login,
    register,
    logout,
    fetchCurrentUser,
  };
});
