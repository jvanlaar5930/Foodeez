<script setup lang="ts">
interface Props {
  size?: 'sm' | 'md' | 'lg';
  color?: string;
}

const props = withDefaults(defineProps<Props>(), {
  size: 'md',
  color: '#16a34a',
});

const sizeMap: Record<string, string> = {
  sm: 'w-4 h-4',
  md: 'w-6 h-6',
  lg: 'w-10 h-10',
};
</script>

<template>
  <svg
    :class="['animate-spin', sizeMap[size]]"
    xmlns="http://www.w3.org/2000/svg"
    fill="none"
    viewBox="0 0 24 24"
    aria-label="Loading"
  >
    <circle
      class="opacity-25"
      cx="12"
      cy="12"
      r="10"
      :stroke="color"
      stroke-width="4"
    />
    <path
      class="opacity-75"
      :fill="color"
      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
    />
  </svg>
</template>
