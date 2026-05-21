<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { computed } from 'vue';

const route = useRoute();
const authStore = useAuthStore();

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
  <aside class="hidden lg:flex flex-col w-64 min-h-screen bg-white border-r border-gray-100 px-4 py-6">
    <!-- Logo -->
    <div class="flex items-center gap-2 px-2 mb-8">
      <span class="text-2xl">🥦</span>
      <span class="text-xl font-bold text-green-700 tracking-tight">Foodeez</span>
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
            ? 'bg-green-50 text-green-700'
            : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900',
        ]"
      >
        <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"
          v-html="item.icon" />
        {{ item.label }}
      </RouterLink>
    </nav>

    <!-- User section -->
    <div class="border-t border-gray-100 pt-4 mt-4">
      <div class="flex items-center gap-3 px-2 py-2 rounded-xl hover:bg-gray-50 transition-colors">
        <div class="w-8 h-8 rounded-full bg-green-600 flex items-center justify-center text-white text-xs font-bold shrink-0">
          {{ userInitials }}
        </div>
        <div class="flex-1 min-w-0">
          <p class="text-sm font-medium text-gray-900 truncate">
            {{ authStore.user?.firstName }} {{ authStore.user?.lastName }}
          </p>
          <p class="text-xs text-gray-400 truncate">{{ authStore.user?.email }}</p>
        </div>
      </div>
      <button
        class="mt-2 w-full flex items-center gap-2 px-3 py-2 text-sm text-gray-500 hover:text-red-600 hover:bg-red-50 rounded-xl transition-colors"
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
