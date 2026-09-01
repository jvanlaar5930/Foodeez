<script setup lang="ts">
import { ref } from 'vue';
import { RouterLink } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import ThemeToggleButton from '@/components/ui/ThemeToggleButton.vue';

defineProps<{
  /** Let the hero sit flush under a transparent header. */
  transparentHeader?: boolean;
}>();

const authStore = useAuthStore();
const mobileOpen = ref(false);

const sectionLinks = [
  { label: 'How it works', href: '/#how-it-works' },
  { label: 'Features', href: '/#features' },
  { label: 'FAQ', href: '/#faq' },
];
</script>

<template>
  <div class="flex min-h-screen flex-col bg-white dark:bg-gray-950">
    <header
      :class="[
        'sticky top-0 z-40 w-full backdrop-blur',
        transparentHeader
          ? 'bg-white/80 dark:bg-gray-950/80'
          : 'border-b border-gray-100 bg-white/90 dark:border-gray-800 dark:bg-gray-950/90',
      ]"
    >
      <div class="mx-auto flex max-w-6xl items-center gap-4 px-4 py-3 sm:px-6">
        <RouterLink to="/" class="flex shrink-0 items-center gap-2">
          <span class="text-2xl">🥦</span>
          <span class="text-xl font-bold tracking-tight text-green-700 dark:text-green-400"
            >Foodeez</span
          >
        </RouterLink>

        <nav class="ml-6 hidden items-center gap-6 md:flex">
          <a
            v-for="link in sectionLinks"
            :key="link.href"
            :href="link.href"
            class="text-sm font-medium text-gray-600 transition-colors hover:text-green-700 dark:text-gray-400 dark:hover:text-green-400"
          >
            {{ link.label }}
          </a>
        </nav>

        <div class="ml-auto flex items-center gap-2">
          <ThemeToggleButton />

          <template v-if="authStore.isAuthenticated">
            <RouterLink
              to="/dashboard"
              class="rounded-xl bg-green-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-green-700"
            >
              Dashboard
            </RouterLink>
          </template>
          <template v-else>
            <RouterLink
              to="/auth/login"
              class="hidden rounded-xl px-3 py-2 text-sm font-medium text-gray-600 transition-colors hover:bg-gray-100 hover:text-gray-900 sm:block dark:text-gray-400 dark:hover:bg-gray-800 dark:hover:text-gray-100"
            >
              Sign in
            </RouterLink>
            <RouterLink
              to="/auth/register"
              class="rounded-xl bg-green-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-green-700"
            >
              Get started
            </RouterLink>
          </template>

          <button
            type="button"
            class="rounded-lg p-2 text-gray-500 hover:bg-gray-100 md:hidden dark:text-gray-400 dark:hover:bg-gray-800"
            :aria-expanded="mobileOpen"
            aria-label="Toggle navigation"
            @click="mobileOpen = !mobileOpen"
          >
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                :d="mobileOpen ? 'M6 18L18 6M6 6l12 12' : 'M4 6h16M4 12h16M4 18h16'"
              />
            </svg>
          </button>
        </div>
      </div>

      <nav
        v-if="mobileOpen"
        class="border-t border-gray-100 px-4 py-3 md:hidden dark:border-gray-800"
      >
        <a
          v-for="link in sectionLinks"
          :key="link.href"
          :href="link.href"
          class="block rounded-lg px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50 dark:text-gray-400 dark:hover:bg-gray-800"
          @click="mobileOpen = false"
        >
          {{ link.label }}
        </a>
      </nav>
    </header>

    <main class="flex-1">
      <slot />
    </main>

    <footer class="border-t border-gray-100 bg-gray-50 dark:border-gray-800 dark:bg-gray-900">
      <div
        class="mx-auto flex max-w-6xl flex-col gap-4 px-4 py-8 sm:flex-row sm:items-center sm:justify-between sm:px-6"
      >
        <div class="flex items-center gap-2">
          <span class="text-xl">🥦</span>
          <span class="font-bold text-green-700 dark:text-green-400">Foodeez</span>
          <span class="text-sm text-gray-400 dark:text-gray-500"
            >&middot; Eat well, without the guesswork.</span
          >
        </div>
        <div class="flex flex-wrap items-center gap-5 text-sm text-gray-500 dark:text-gray-400">
          <RouterLink to="/search" class="transition-colors hover:text-green-700 dark:hover:text-green-400"
            >Browse recipes</RouterLink
          >
          <RouterLink
            to="/auth/register"
            class="transition-colors hover:text-green-700 dark:hover:text-green-400"
            >Create account</RouterLink
          >
          <span>&copy; {{ new Date().getFullYear() }} Foodeez</span>
        </div>
      </div>
    </footer>
  </div>
</template>
