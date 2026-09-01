<script setup lang="ts">
import { useThemeStore } from '@/stores/theme';

const themeStore = useThemeStore();
</script>

<template>
  <button
    type="button"
    class="rounded-xl p-2 text-gray-500 transition-colors hover:bg-gray-100 hover:text-gray-900 focus:outline-none focus-visible:ring-2 focus-visible:ring-green-500 dark:text-gray-400 dark:hover:bg-gray-800 dark:hover:text-gray-100"
    :aria-label="themeStore.isDark ? 'Switch to light mode' : 'Switch to dark mode'"
    :title="themeStore.isDark ? 'Light mode' : 'Dark mode'"
    @click="themeStore.toggleDarkMode()"
  >
    <svg v-if="themeStore.isDark" class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path
        stroke-linecap="round"
        stroke-linejoin="round"
        stroke-width="2"
        d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"
      />
    </svg>
    <svg v-else class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path
        stroke-linecap="round"
        stroke-linejoin="round"
        stroke-width="2"
        d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"
      />
    </svg>
  </button>
</template>
