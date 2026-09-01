<script setup lang="ts">
import { watch } from 'vue';
import { RouterView } from 'vue-router';
import { useProfileStore } from '@/stores/profile';
import { useThemeStore } from '@/stores/theme';

const profileStore = useProfileStore();
const themeStore = useThemeStore();

watch(
  () => profileStore.profile?.darkMode,
  (darkMode) => {
    if (darkMode !== undefined) {
      themeStore.initFromProfile(darkMode);
    }
  },
  { immediate: true }
);
</script>

<template>
  <RouterView />
</template>
