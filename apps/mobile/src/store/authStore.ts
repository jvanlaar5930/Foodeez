import { create } from 'zustand';
import { useSavedRecipeStore } from './savedRecipeStore';
import { persist, createJSONStorage } from 'zustand/middleware';
import * as SecureStore from 'expo-secure-store';
import * as authService from '@/services/authService';
import type { LoginRequest, RegisterRequest, UserDto } from '@/types';

interface AuthState {
  user: UserDto | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
  loadStoredAuth: () => Promise<void>;
  clearError: () => void;
}

const secureStorage = {
  getItem: async (name: string): Promise<string | null> => {
    return SecureStore.getItemAsync(name);
  },
  setItem: async (name: string, value: string): Promise<void> => {
    await SecureStore.setItemAsync(name, value);
  },
  removeItem: async (name: string): Promise<void> => {
    await SecureStore.deleteItemAsync(name);
  },
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set, _get) => ({
      user: null,
      token: null,
      isAuthenticated: false,
      isLoading: true,
      error: null,

      login: async (data: LoginRequest) => {
        set({ isLoading: true, error: null });
        try {
          const response = await authService.login(data);
          set({
            user: response.user,
            token: response.token,
            isAuthenticated: true,
            isLoading: false,
            error: null,
          });
        } catch (err: unknown) {
          const message =
            err instanceof Error ? err.message : 'Login failed. Please check your credentials.';
          set({ isLoading: false, error: message, isAuthenticated: false });
          throw err;
        }
      },

      register: async (data: RegisterRequest) => {
        set({ isLoading: true, error: null });
        try {
          const response = await authService.register(data);
          set({
            user: response.user,
            token: response.token,
            isAuthenticated: true,
            isLoading: false,
            error: null,
          });
        } catch (err: unknown) {
          const message =
            err instanceof Error ? err.message : 'Registration failed. Please try again.';
          set({ isLoading: false, error: message, isAuthenticated: false });
          throw err;
        }
      },

      logout: () => {
        set({
          user: null,
          token: null,
          isAuthenticated: false,
          isLoading: false,
          error: null,
        });
        // Saved recipes belong to the account that just left; the next sign-in must not
        // inherit them.
        useSavedRecipeStore.getState().clear();
      },

      loadStoredAuth: async () => {
        // The persist middleware handles rehydration automatically.
        // We just need to set isLoading to false after hydration check.
        set({ isLoading: false });
      },

      clearError: () => {
        set({ error: null });
      },
    }),
    {
      name: 'foodeez-auth',
      storage: createJSONStorage(() => secureStorage),
      partialize: (state) => ({
        user: state.user,
        token: state.token,
        isAuthenticated: state.isAuthenticated,
      }),
      onRehydrateStorage: () => (state) => {
        if (state) {
          state.isLoading = false;
        }
      },
    },
  ),
);
