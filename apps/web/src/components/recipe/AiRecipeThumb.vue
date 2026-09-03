<script setup lang="ts">
/**
 * The stand-in picture for a recipe that came out of the advice tab.
 *
 * Drawn here rather than loaded as a file: it is the same speech-bubble mark and "Ask AI"
 * wording the tab itself carries, so a reader recognises where the recipe came from, and
 * drawing it keeps it sharp at every size with nothing to fetch.
 */
withDefaults(defineProps<{ compact?: boolean }>(), { compact: false });
</script>

<template>
  <div
    class="flex h-full w-full flex-col items-center justify-center gap-1 bg-gradient-to-br from-green-500 to-emerald-600 text-white"
  >
    <svg
      :class="compact ? 'h-8 w-8' : 'h-12 w-12'"
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      aria-hidden="true"
    >
      <path
        stroke-linecap="round"
        stroke-linejoin="round"
        stroke-width="2"
        d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
      />
    </svg>
    <span
      :class="[
        'font-semibold uppercase tracking-wide',
        compact ? 'text-[10px]' : 'text-xs',
      ]"
    >
      Ask AI
    </span>
  </div>
</template>
