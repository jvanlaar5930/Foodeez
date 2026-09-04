<script setup lang="ts">
interface Props {
  /**
   * Matches what the app already renders: `md` is the p-6 most panels use, `lg` the p-8 of
   * the auth forms, `sm` the p-5 of the denser lists, and `none` for a card whose content
   * runs to its edges - a table, or anything with its own header band.
   */
  padding?: 'none' | 'sm' | 'md' | 'lg';
  /**
   * A hairline border. Off by default because none of the fourteen hand-rolled cards this
   * replaces had one, and turning it on everywhere would be a visual change dressed up as a
   * refactor.
   */
  bordered?: boolean;
  /** Lifts on hover. For a card that is itself a link or a button. */
  interactive?: boolean;
}

withDefaults(defineProps<Props>(), {
  padding: 'md',
  bordered: false,
  interactive: false,
});

const PADDING: Record<string, string> = {
  none: '',
  sm: 'p-5',
  md: 'p-6',
  lg: 'p-8',
};
</script>

<template>
  <div
    :class="[
      'rounded-2xl bg-white shadow-sm dark:bg-gray-900',
      PADDING[padding],
      bordered ? 'border border-gray-100 dark:border-gray-800' : '',
      interactive ? 'transition-shadow hover:shadow-md' : '',
      padding === 'none' ? 'overflow-hidden' : '',
    ]"
  >
    <div v-if="$slots.header" class="mb-4 border-b border-gray-100 pb-4 dark:border-gray-800">
      <slot name="header" />
    </div>

    <slot />

    <div v-if="$slots.footer" class="mt-4 border-t border-gray-100 pt-4 dark:border-gray-800">
      <slot name="footer" />
    </div>
  </div>
</template>
