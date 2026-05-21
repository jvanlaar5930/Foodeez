import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import type { LoginRequest, RegisterRequest } from '@foodeez/shared';

export function useAuth() {
  const authStore = useAuthStore();
  const router = useRouter();

  async function handleLogin(data: LoginRequest): Promise<void> {
    await authStore.login(data);
    if (!authStore.user?.profileCompleted) {
      await router.push('/auth/setup');
    } else {
      await router.push('/dashboard');
    }
  }

  async function handleRegister(data: RegisterRequest): Promise<void> {
    await authStore.register(data);
    await router.push('/auth/setup');
  }

  async function handleLogout(): Promise<void> {
    authStore.logout();
    await router.push('/auth/login');
  }

  return {
    user: authStore.user,
    token: authStore.token,
    isAuthenticated: authStore.isAuthenticated,
    isLoading: authStore.isLoading,
    error: authStore.error,
    handleLogin,
    handleRegister,
    handleLogout,
  };
}
