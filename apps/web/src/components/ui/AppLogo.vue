<script setup lang="ts">
import { computed } from 'vue';
import markUrl from '@foodeez/brand/assets/Foodeez_mark.png';
import wordmarkUrl from '@foodeez/brand/assets/Foodeez_image.svg';

/**
 * The horizontal Foodeez lockup: icon mark beside the wordmark.
 *
 * Composed from the two assets rather than shipped as one image, so the wordmark stays true
 * vector at any size. Both files are trimmed to their artwork, so a single height positions
 * them - the widths follow from the aspect ratios below.
 *
 * The wordmark is set a little shorter than the mark. It carries its own tagline underneath
 * the name, so matching the heights exactly leaves a wide block of text overpowering the icon.
 */
const props = withDefaults(defineProps<{ height?: number }>(), { height: 32 });

const WORDMARK_SCALE = 0.8;

const wordmarkHeight = computed(() => Math.round(props.height * WORDMARK_SCALE));
const gap = computed(() => Math.round(props.height * 0.28));
</script>

<template>
  <span class="inline-flex items-center" :style="{ gap: `${gap}px` }">
    <!-- Decorative: the wordmark beside it already carries the name. -->
    <img :src="markUrl" alt="" class="w-auto" :style="{ height: `${height}px` }" />
    <img :src="wordmarkUrl" alt="Foodeez" class="w-auto" :style="{ height: `${wordmarkHeight}px` }" />
  </span>
</template>
