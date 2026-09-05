import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { authService } from '@/services/authService';
import { useAsyncState } from '@/stores/asyncState';
import { setAuthStore } from '@/services/api';
import type { LoginRequest, RegisterRequest, User } from '@foodeez/shared';

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null);
  const token = ref<string | null>(localStorage.getItem('foodeez_token'));
  const { isLoading, error, runOrThrow } = useAsyncState();

  const isAuthenticated = computed(() => !!token.value && !!user.value);
  const isAdmin = computed(() => user.value?.isAdmin ?? false);

  // Wire up the api instance so it can read our token
  setAuthStore({
    get token() {
      return token.value;
    },
    logout,
  });

  // Both rethrow: the form stays put and keeps what was typed when a sign-in fails.

  async function login(data: LoginRequest): Promise<void> {
    await runOrThrow('Could not sign you in.', async () =>
      accept(await authService.login(data)),
    );
  }

  async function register(data: RegisterRequest): Promise<void> {
    await runOrThrow('Could not create your account.', async () =>
      accept(await authService.register(data)),
    );
  }

  function accept(response: { token: string; user: User }): void {
    token.value = response.token;
    user.value = response.user;
    localStorage.setItem('foodeez_token', response.token);
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
