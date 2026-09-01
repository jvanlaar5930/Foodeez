<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router';
import AppSidebar from './AppSidebar.vue';

const route = useRoute();

const navItems = [
  {
    label: 'Home',
    path: '/dashboard',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />`,
  },
  {
    label: 'Log',
    path: '/meal-log',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />`,
  },
  {
    label: 'Plan',
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

function isActive(path: string): boolean {
  return route.path.startsWith(path);
}
</script>

<template>
  <div class="min-h-screen flex bg-gray-50 dark:bg-gray-950">
    <!-- Desktop sidebar -->
    <AppSidebar />

    <!-- Main content -->
    <div class="flex-1 flex flex-col min-w-0">
      <main class="flex-1 overflow-y-auto pb-20 lg:pb-0">
        <slot />
      </main>

      <!-- Mobile bottom navigation -->
      <nav class="lg:hidden fixed bottom-0 left-0 right-0 bg-white dark:bg-gray-900 border-t border-gray-100 dark:border-gray-800 z-40">
        <div class="flex">
          <RouterLink
            v-for="item in navItems"
            :key="item.path"
            :to="item.path"
            :class="[
              'flex-1 flex flex-col items-center gap-0.5 py-2 text-xs font-medium transition-colors',
              isActive(item.path)
                ? 'text-green-700 dark:text-green-400'
                : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-200',
            ]"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"
              v-html="item.icon" />
            {{ item.label }}
          </RouterLink>
        </div>
      </nav>
    </div>
  </div>
</template>
