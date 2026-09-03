<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { useThemeStore } from '@/stores/theme';
import { computed } from 'vue';

const route = useRoute();
const authStore = useAuthStore();
const themeStore = useThemeStore();

const navItems = [
  {
    label: 'Dashboard',
    path: '/dashboard',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />`,
  },
  {
    label: 'Meal Log',
    path: '/meal-log',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />`,
  },
  {
    label: 'Meal Plan',
    path: '/meal-plan',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />`,
  },
  {
    label: 'Grocery List',
    path: '/grocery',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z" />`,
  },
  {
    label: 'Ask AI',
    path: '/advice',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />`,
  },
  {
    label: 'Recipes',
    path: '/recipes',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />`,
  },
  {
    label: 'Profile',
    path: '/profile',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />`,
  },
];

const userInitials = computed(() => {
  const u = authStore.user;
  if (!u) return '?';
  return `${u.firstName.charAt(0)}${u.lastName.charAt(0)}`.toUpperCase();
});

function isActive(path: string): boolean {
  return route.path.startsWith(path);
}

</script>

<template>
  <aside class="hidden lg:flex flex-col w-64 min-h-screen bg-white dark:bg-gray-900 border-r border-gray-100 dark:border-gray-800 px-4 py-6">
    <!-- Logo -->
    <div class="flex items-center gap-2 px-2 mb-8">
      <span class="text-2xl">🥦</span>
      <span class="text-xl font-bold text-green-700 dark:text-green-400 tracking-tight">Foodeez</span>
    </div>

    <!-- Navigation -->
    <nav class="flex-1 space-y-1">
      <RouterLink
        v-for="item in navItems"
        :key="item.path"
        :to="item.path"
        :class="[
          'flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors',
          isActive(item.path)
            ? 'bg-green-50 dark:bg-green-900/30 text-green-700 dark:text-green-400'
            : 'text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800 hover:text-gray-900 dark:hover:text-gray-100',
        ]"
      >
        <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"
          v-html="item.icon" />
        {{ item.label }}
      </RouterLink>

      <!-- Admin link (only visible to admins) -->
      <RouterLink
        v-if="authStore.isAdmin"
        to="/admin/logs"
        :class="[
          'flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors',
          isActive('/admin')
            ? 'bg-purple-50 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400'
            : 'text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800 hover:text-gray-900 dark:hover:text-gray-100',
        ]"
      >
        <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
        </svg>
        Admin
      </RouterLink>
    </nav>

    <!-- Bottom controls -->
    <div class="border-t border-gray-100 dark:border-gray-800 pt-4 mt-4 space-y-1">
      <!-- Dark mode toggle -->
      <button
        class="w-full flex items-center justify-between px-3 py-2 text-sm text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800 rounded-xl transition-colors"
        @click="themeStore.toggleDarkMode()"
      >
        <span class="flex items-center gap-2">
          <svg v-if="!themeStore.isDark" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
          </svg>
          <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
          </svg>
          {{ themeStore.isDark ? 'Light mode' : 'Dark mode' }}
        </span>
        <div :class="['w-9 h-5 rounded-full transition-colors relative', themeStore.isDark ? 'bg-green-500' : 'bg-gray-300 dark:bg-gray-600']">
          <div :class="['absolute top-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform', themeStore.isDark ? 'translate-x-4' : 'translate-x-0.5']" />
        </div>
      </button>

      <!-- User info -->
      <div class="flex items-center gap-3 px-2 py-2 rounded-xl hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
        <div class="w-8 h-8 rounded-full bg-green-600 flex items-center justify-center text-white text-xs font-bold shrink-0">
          {{ userInitials }}
        </div>
        <div class="flex-1 min-w-0">
          <p class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
            {{ authStore.user?.firstName }} {{ authStore.user?.lastName }}
          </p>
          <p class="text-xs text-gray-400 dark:text-gray-500 truncate">{{ authStore.user?.email }}</p>
        </div>
      </div>

      <button
        class="w-full flex items-center gap-2 px-3 py-2 text-sm text-gray-500 dark:text-gray-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-xl transition-colors"
        @click="$router.push('/auth/login'); authStore.logout()"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
        </svg>
        Sign out
      </button>
    </div>
  </aside>
</template>
