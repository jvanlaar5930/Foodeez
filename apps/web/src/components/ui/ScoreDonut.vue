<script setup lang="ts">
import { computed } from 'vue';
import { scoreStroke, scoreTextClass } from '@/utils/analysisScore';

interface Props {
  /** 0-100. Anything outside that is clamped rather than drawn as an over-full ring. */
  score: number;
  size?: 'sm' | 'md';
}

const props = withDefaults(defineProps<Props>(), { size: 'sm' });

/**
 * Geometry per size. The radius drives the circumference, which drives the dash array - three
 * numbers that have to agree. Written out three times before this, each with its own
 * hand-computed circumference (150.8 for r=24, 175.9 for r=28), which is the kind of constant
 * that quietly stops matching when someone adjusts a radius.
 */
const GEOMETRY = {
  sm: { box: 56, center: 28, radius: 24, wrapper: 'h-14 w-14', label: 'text-sm' },
  md: { box: 64, center: 32, radius: 28, wrapper: 'h-16 w-16', label: 'text-base' },
} as const;

const geometry = computed(() => GEOMETRY[props.size]);
const clamped = computed(() => Math.min(100, Math.max(0, props.score)));
const circumference = computed(() => 2 * Math.PI * geometry.value.radius);
const arc = computed(() => `${(clamped.value / 100) * circumference.value} ${circumference.value}`);
</script>

<template>
  <div class="relative shrink-0" :class="geometry.wrapper">
    <svg
      class="-rotate-90"
      :class="geometry.wrapper"
      :viewBox="`0 0 ${geometry.box} ${geometry.box}`"
      role="img"
      :aria-label="`Score ${clamped} out of 100`"
    >
      <!-- The track behind the arc. A theme token rather than a hex literal, so it follows
           the purple the AI panels are drawn in. -->
      <circle
        :cx="geometry.center"
        :cy="geometry.center"
        :r="geometry.radius"
        fill="none"
        stroke="var(--color-purple-200)"
        stroke-width="5"
      />
      <circle
        :cx="geometry.center"
        :cy="geometry.center"
        :r="geometry.radius"
        fill="none"
        :stroke="scoreStroke(clamped)"
        stroke-width="5"
        stroke-linecap="round"
        :stroke-dasharray="arc"
      />
    </svg>
    <span
      class="absolute inset-0 flex items-center justify-center font-bold"
      :class="[geometry.label, scoreTextClass(clamped)]"
      aria-hidden="true"
    >
      {{ clamped }}
    </span>
  </div>
</template>
