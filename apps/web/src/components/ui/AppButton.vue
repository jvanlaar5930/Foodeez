<script setup lang="ts">
import LoadingSpinner from './LoadingSpinner.vue';

interface Props {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  loading?: boolean;
  disabled?: boolean;
  type?: 'button' | 'submit' | 'reset';
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'primary',
  size: 'md',
  loading: false,
  disabled: false,
  type: 'button',
});

const variantClasses: Record<string, string> = {
  primary:
    'bg-green-600 text-white hover:bg-green-700 focus:ring-green-500 disabled:bg-green-300',
  secondary:
    'bg-gray-600 text-white hover:bg-gray-700 focus:ring-gray-500 disabled:bg-gray-300',
  outline:
    'border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 focus:ring-gray-400 disabled:opacity-50',
  ghost:
    'bg-transparent text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 focus:ring-gray-400 disabled:opacity-50',
  danger:
    'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500 disabled:bg-red-300',
};

const sizeClasses: Record<string, string> = {
  sm: 'px-3 py-1.5 text-sm',
  md: 'px-4 py-2 text-sm',
  lg: 'px-6 py-3 text-base',
};
</script>

<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    :class="[
      'inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 cursor-pointer',
      variantClasses[variant],
      sizeClasses[size],
      (disabled || loading) ? 'cursor-not-allowed opacity-70' : '',
    ]"
  >
    <LoadingSpinner v-if="loading" size="sm" color="currentColor" />
    <slot />
  </button>
</template>
