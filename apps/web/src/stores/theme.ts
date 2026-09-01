import { ref } from 'vue';
import { defineStore } from 'pinia';
import { profileService } from '@/services/profileService';
import { useAuthStore } from '@/stores/auth';
import { useProfileStore } from '@/stores/profile';

const STORAGE_KEY = 'foodeez_dark_mode';

function applyTheme(dark: boolean): void {
  document.documentElement.classList.toggle('dark', dark);
  document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
}

function readStoredPreference(): boolean | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw === null ? null : raw === 'true';
  } catch {
    return null;
  }
}

function storePreference(dark: boolean): void {
  try {
    localStorage.setItem(STORAGE_KEY, String(dark));
  } catch {
    // Private browsing / blocked storage: the in-memory preference still applies.
  }
}

function prefersDark(): boolean {
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
}

export const useThemeStore = defineStore('theme', () => {
  const isDark = ref(false);

  /**
   * Resolve and apply the theme before the app renders. The stored preference wins;
   * otherwise fall back to the OS setting. Runs for signed-out visitors too, so the
   * public pages honour dark mode without an account.
   */
  function init(): void {
    isDark.value = readStoredPreference() ?? prefersDark();
    applyTheme(isDark.value);
  }

  /** Adopt the signed-in user's saved preference once their profile arrives. */
  function initFromProfile(darkMode: boolean): void {
    isDark.value = darkMode;
    storePreference(darkMode);
    applyTheme(darkMode);
  }

  function setDark(dark: boolean): void {
    isDark.value = dark;
    storePreference(dark);
    applyTheme(dark);
  }

  /**
   * Flip the theme immediately, then mirror it onto the signed-in user's profile as a
   * best-effort background write. A failed sync (or no account at all) never undoes the
   * local change: the preference lives in localStorage either way.
   */
  async function toggleDarkMode(): Promise<void> {
    setDark(!isDark.value);
    await syncToProfile();
  }

  async function syncToProfile(): Promise<void> {
    const authStore = useAuthStore();
    const profileStore = useProfileStore();
    const userId = authStore.user?.id;
    if (!userId) return;

    try {
      if (!profileStore.profile) {
        await profileStore.fetchProfile(userId);
      }
      const profile = profileStore.profile;
      if (!profile) return;

      profile.darkMode = isDark.value;
      await profileService.updateProfile(userId, {
        heightCm: profile.heightCm,
        weightKg: profile.weightKg,
        targetWeightKg: profile.targetWeightKg,
        age: profile.age,
        gender: profile.gender,
        activityLevel: profile.activityLevel,
        dietaryGoal: profile.dietaryGoal,
        notes: profile.notes,
        darkMode: isDark.value,
      });
    } catch {
      // Preference is already stored locally; the server copy can catch up later.
    }
  }

  return { isDark, init, initFromProfile, setDark, toggleDarkMode };
});
