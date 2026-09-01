<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref } from 'vue';
import FoodSpriteSheet from './FoodSpriteSheet.vue';

interface Decoration {
  /** Symbol id from FoodSpriteSheet. */
  shape: string;
  /** Position as a percentage of the backdrop box. */
  top: string;
  left: string;
  /** Rendered edge length in px at the `lg` breakpoint. */
  size: number;
  /** Fraction of scroll distance this element lags behind by. Higher = nearer the viewer. */
  speed: number;
  rotate: number;
  /** Hue, as text-colour classes so the shape tints itself through `currentColor`. */
  tone: string;
  /** Kept low: this sits behind real content and must never compete with it. */
  opacity: string;
  /** Hidden on small screens where there is no room for it to read as decoration. */
  hideOnMobile?: boolean;
}

/*
 * Depth reads from three cues at once - size, opacity and speed. Distant items are large,
 * faint and slow; near items are small, crisper and fast. Keeping those correlated is what
 * makes the layers feel like space rather than a set of independently sliding stickers.
 */
const DECORATIONS: Decoration[] = [
  // ── Hero ──
  { shape: 'pf-leaf', top: '1%', left: '-4%', size: 260, speed: 0.16, rotate: -18, tone: 'text-emerald-500 dark:text-emerald-400', opacity: 'opacity-[0.10] dark:opacity-[0.13]' },
  { shape: 'pf-citrus', top: '3%', left: '84%', size: 220, speed: 0.24, rotate: 12, tone: 'text-amber-500 dark:text-amber-400', opacity: 'opacity-[0.13] dark:opacity-[0.16]' },
  { shape: 'pf-avocado', top: '10%', left: '8%', size: 130, speed: 0.42, rotate: 14, tone: 'text-lime-600 dark:text-lime-400', opacity: 'opacity-[0.18] dark:opacity-[0.20]', hideOnMobile: true },
  { shape: 'pf-tomato', top: '7%', left: '91%', size: 110, speed: 0.5, rotate: -10, tone: 'text-rose-500 dark:text-rose-400', opacity: 'opacity-[0.18] dark:opacity-[0.20]', hideOnMobile: true },
  { shape: 'pf-ramen', top: '17%', left: '78%', size: 190, speed: 0.3, rotate: -6, tone: 'text-orange-500 dark:text-orange-400', opacity: 'opacity-[0.13] dark:opacity-[0.16]' },
  { shape: 'pf-wheat', top: '19%', left: '3%', size: 150, speed: 0.36, rotate: 20, tone: 'text-amber-600 dark:text-amber-400', opacity: 'opacity-[0.14] dark:opacity-[0.17]', hideOnMobile: true },

  // ── How it works ──
  { shape: 'pf-pizza', top: '28%', left: '89%', size: 165, speed: 0.2, rotate: 16, tone: 'text-orange-500 dark:text-orange-400', opacity: 'opacity-[0.11] dark:opacity-[0.14]' },
  { shape: 'pf-chilli', top: '33%', left: '-2%', size: 175, speed: 0.28, rotate: -12, tone: 'text-red-500 dark:text-red-400', opacity: 'opacity-[0.11] dark:opacity-[0.14]' },
  { shape: 'pf-egg', top: '38%', left: '92%', size: 120, speed: 0.46, rotate: -20, tone: 'text-yellow-500 dark:text-yellow-400', opacity: 'opacity-[0.16] dark:opacity-[0.19]', hideOnMobile: true },

  // ── Features ──
  { shape: 'pf-bowl', top: '46%', left: '4%', size: 200, speed: 0.22, rotate: 8, tone: 'text-emerald-500 dark:text-emerald-400', opacity: 'opacity-[0.11] dark:opacity-[0.14]' },
  { shape: 'pf-sushi', top: '52%', left: '87%', size: 150, speed: 0.34, rotate: -14, tone: 'text-rose-500 dark:text-rose-400', opacity: 'opacity-[0.13] dark:opacity-[0.16]', hideOnMobile: true },
  { shape: 'pf-croissant', top: '58%', left: '-3%', size: 185, speed: 0.26, rotate: 10, tone: 'text-amber-500 dark:text-amber-400', opacity: 'opacity-[0.12] dark:opacity-[0.15]' },

  // ── FAQ ──
  { shape: 'pf-burger', top: '68%', left: '90%', size: 175, speed: 0.24, rotate: -8, tone: 'text-orange-500 dark:text-orange-400', opacity: 'opacity-[0.11] dark:opacity-[0.14]' },
  { shape: 'pf-pancakes', top: '76%', left: '2%', size: 165, speed: 0.32, rotate: 12, tone: 'text-amber-600 dark:text-amber-400', opacity: 'opacity-[0.12] dark:opacity-[0.15]', hideOnMobile: true },
  { shape: 'pf-leaf', top: '84%', left: '86%', size: 210, speed: 0.18, rotate: 150, tone: 'text-green-600 dark:text-green-400', opacity: 'opacity-[0.10] dark:opacity-[0.13]' },
];

const root = ref<HTMLElement | null>(null);
const items = ref<HTMLElement[]>([]);

let frame = 0;
let motionQuery: MediaQueryList | null = null;

function apply(scrollY: number) {
  for (const el of items.value) {
    const speed = Number(el.dataset.speed ?? 0);
    // translate3d keeps the work on the compositor; `top`/`background-position` would
    // force layout or paint on every frame.
    el.style.transform = `translate3d(0, ${(scrollY * speed).toFixed(2)}px, 0)`;
  }
}

function onScroll() {
  if (frame) return;
  frame = requestAnimationFrame(() => {
    frame = 0;
    apply(window.scrollY);
  });
}

function reset() {
  for (const el of items.value) el.style.transform = '';
}

function syncMotionPreference() {
  if (motionQuery?.matches) {
    window.removeEventListener('scroll', onScroll);
    if (frame) {
      cancelAnimationFrame(frame);
      frame = 0;
    }
    reset();
  } else {
    window.addEventListener('scroll', onScroll, { passive: true });
    apply(window.scrollY);
  }
}

onMounted(() => {
  items.value = Array.from(root.value?.querySelectorAll<HTMLElement>('[data-speed]') ?? []);
  motionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
  motionQuery.addEventListener('change', syncMotionPreference);
  syncMotionPreference();
});

onBeforeUnmount(() => {
  window.removeEventListener('scroll', onScroll);
  motionQuery?.removeEventListener('change', syncMotionPreference);
  if (frame) cancelAnimationFrame(frame);
});
</script>

<template>
  <!--
    `-z-10` puts this behind the page sections but still above the wrapper's own background,
    and `pointer-events-none` keeps it clear of the search field and its autocomplete popup.
  -->
  <div
    ref="root"
    class="pointer-events-none absolute inset-0 -z-10 overflow-hidden"
    aria-hidden="true"
  >
    <FoodSpriteSheet />

    <!-- Soft colour wash: gives the shapes something to sit in rather than bare white -->
    <div class="absolute -left-32 -top-24 h-96 w-96 rounded-full bg-green-200/40 blur-3xl dark:bg-green-800/20" />
    <div class="absolute -right-24 top-40 h-[26rem] w-[26rem] rounded-full bg-emerald-200/30 blur-3xl dark:bg-emerald-800/20" />
    <div class="absolute left-1/4 top-[45%] h-96 w-96 rounded-full bg-amber-200/25 blur-3xl dark:bg-amber-900/15" />
    <div class="absolute -left-20 top-[72%] h-96 w-96 rounded-full bg-lime-200/25 blur-3xl dark:bg-lime-900/15" />

    <svg
      v-for="(deco, i) in DECORATIONS"
      :key="`${deco.shape}-${i}`"
      class="pf-float absolute will-change-transform"
      :class="[deco.tone, deco.opacity, deco.hideOnMobile ? 'hidden md:block' : '']"
      :style="{
        top: deco.top,
        left: deco.left,
        width: `${deco.size}px`,
        height: `${deco.size}px`,
        '--pf-rotate': `${deco.rotate}deg`,
        '--pf-delay': `${(i % 5) * -1.7}s`,
      }"
      :data-speed="deco.speed"
      viewBox="0 0 64 64"
    >
      <use :href="`#${deco.shape}`" />
    </svg>
  </div>
</template>

<style scoped>
/*
 * Two transforms have to coexist: the scroll offset (set inline on the element by the
 * script) and the resting tilt plus drift. Applying the tilt/drift to an inner layer via
 * the animation would fight the inline transform, so the element carries the scroll offset
 * and this animation runs on the same element's `rotate`/`translate` longhands, which
 * compose with `transform` instead of replacing it.
 */
.pf-float {
  rotate: var(--pf-rotate, 0deg);
  animation: pf-drift 13s ease-in-out infinite;
  animation-delay: var(--pf-delay, 0s);
}

@keyframes pf-drift {
  0%,
  100% {
    translate: 0 0;
  }
  50% {
    translate: 0 -14px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .pf-float {
    animation: none;
    translate: none;
  }
}
</style>
