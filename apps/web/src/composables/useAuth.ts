import { storeToRefs } from 'pinia';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import type { LoginRequest, RegisterRequest } from '@foodeez/shared';

/**
 * Signing in and out, with the routing that goes with each.
 *
 * The state comes back through storeToRefs. Reading `authStore.user` here and returning it
 * handed the caller the value as it was at setup time - a plain object, not a ref - so a
 * component that destructured `user` or `isAuthenticated` was looking at a snapshot that
 * never updated. `handleLogout` would clear the store and the header would go on showing the
 * name of whoever had just left.
 */
export function useAuth() {
  const authStore = useAuthStore();
  const { user, token, isAuthenticated, isLoading, error } = storeToRefs(authStore);
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
    user,
    token,
    isAuthenticated,
    isLoading,
    error,
    handleLogin,
    handleRegister,
    handleLogout,
  };
}
