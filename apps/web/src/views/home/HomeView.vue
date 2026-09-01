<script setup lang="ts">
import { ref } from 'vue';
import { useRouter, RouterLink } from 'vue-router';
import PublicLayout from '@/components/layout/PublicLayout.vue';
import ParallaxBackdrop from '@/components/home/ParallaxBackdrop.vue';
import RecipeSearchBar from '@/components/recipe/RecipeSearchBar.vue';
import type { RecipeSuggestion } from '@/services/recipeService';

const router = useRouter();
const query = ref('');

function goToSearch(q: string, suggestion?: RecipeSuggestion) {
  router.push({
    path: '/search',
    query: suggestion?.recipeId ? { q, recipe: suggestion.recipeId } : { q },
  });
}

const POPULAR_SEARCHES = [
  'Chicken curry',
  'High protein breakfast',
  'Vegan tacos',
  'Overnight oats',
  '30-minute dinner',
  'Salmon',
];

const STEPS = [
  {
    emoji: '🔍',
    title: 'Search anything',
    body: 'Type a dish, an ingredient, or a goal. We pull from a library of thousands of recipes — no account, no paywall.',
  },
  {
    emoji: '📊',
    title: 'See the real numbers',
    body: 'Every result carries full per-serving nutrition: calories, protein, carbs, fat, fiber and sugar. No guessing.',
  },
  {
    emoji: '🗓️',
    title: 'Build your week',
    body: 'Create a free account to save recipes into a weekly meal plan and track what you actually ate.',
  },
];

const FEATURES = [
  {
    emoji: '🥗',
    title: 'Nutrition on every recipe',
    body: 'Macro and micronutrient breakdowns per serving, so a recipe is a decision you can make in seconds.',
  },
  {
    emoji: '🏷️',
    title: 'Filters that mean something',
    body: 'Vegetarian, vegan, gluten-free, keto, high-protein, quick — applied to the actual recipe data, not tags someone typed.',
  },
  {
    emoji: '📸',
    title: 'Log a meal from a photo',
    body: 'Snap what you are eating and let the AI identify the items and estimate the portions for you.',
  },
  {
    emoji: '🤖',
    title: 'AI meal plans',
    body: 'Generate a week of meals around your calorie and macro targets, then adjust anything you do not like.',
  },
  {
    emoji: '📈',
    title: 'Progress that adds up',
    body: 'Daily totals against your targets, so you can see the trend instead of a single bad Tuesday.',
  },
  {
    emoji: '🛒',
    title: 'Plan to plate',
    body: 'Turn a week of planned meals into an ingredient list, grouped the way a grocery store is laid out.',
  },
];

const FAQ = [
  {
    q: 'Do I need an account to search recipes?',
    a: 'No. Searching, browsing and reading full recipes — ingredients, instructions and nutrition — is completely open. An account only comes in when you want to save things: meal plans, logged meals and personal targets.',
  },
  {
    q: 'Where do the recipes come from?',
    a: 'Foodeez searches its own recipe library first and falls back to a large third-party recipe database, caching what it finds. Nutrition is taken from the source data rather than estimated.',
  },
  {
    q: 'Is it free?',
    a: 'Search and recipe browsing are free and always will be. Creating an account for meal planning and tracking is also free.',
  },
  {
    q: 'How accurate is the nutrition data?',
    a: 'Per-serving figures come from the recipe source. They are a good planning estimate, not a lab measurement — portion sizes and ingredient brands move the numbers in real kitchens.',
  },
];

const openFaq = ref<number | null>(0);
</script>


<template>
  <PublicLayout transparent-header>
    <!--
      Everything above the closing CTA shares one scrolling stage so a single parallax
      backdrop can run the length of the page. `isolate` keeps the backdrop's `-z-10`
      contained here: behind the sections, but still above this wrapper's own gradient.
    -->
    <div
      class="relative isolate overflow-hidden bg-gradient-to-b from-green-50 via-white to-white dark:from-green-950/40 dark:via-gray-950 dark:to-gray-950"
    >
      <ParallaxBackdrop />

      <!-- ── Hero ────────────────────────────────────────────────────────── -->
      <section class="relative">
        <div class="mx-auto max-w-3xl px-4 pb-24 pt-20 text-center sm:px-6 sm:pb-32 sm:pt-28">
          <span
            class="mb-6 inline-flex items-center gap-2 rounded-full border border-green-200 bg-white/70 px-4 py-1.5 text-xs font-semibold text-green-700 shadow-sm backdrop-blur-sm dark:border-green-800 dark:bg-gray-900/70 dark:text-green-400"
          >
            <span class="h-1.5 w-1.5 rounded-full bg-green-500" />
            Free to search &middot; no account needed
          </span>

          <h1
            class="text-4xl font-extrabold leading-[1.1] tracking-tight text-gray-900 sm:text-6xl dark:text-white"
          >
            Find a recipe.<br />
            <span class="bg-gradient-to-r from-green-600 to-emerald-500 bg-clip-text text-transparent">
              Know exactly what's in it.
            </span>
          </h1>

          <p class="mx-auto mt-6 max-w-xl text-lg leading-relaxed text-gray-600 dark:text-gray-300">
            Search thousands of recipes and get the full nutritional breakdown on every one — then
            plan your week around the food you actually want to eat.
          </p>

          <div class="mx-auto mt-10 max-w-2xl">
            <RecipeSearchBar v-model="query" size="lg" autofocus @search="goToSearch" />
          </div>

          <div class="mt-6 flex flex-wrap items-center justify-center gap-2">
            <span class="text-sm text-gray-500 dark:text-gray-400">Popular:</span>
            <button
              v-for="term in POPULAR_SEARCHES"
              :key="term"
              type="button"
              class="rounded-full border border-gray-200 bg-white/70 px-3 py-1.5 text-sm text-gray-600 backdrop-blur-sm transition-colors hover:border-green-400 hover:text-green-700 dark:border-gray-700 dark:bg-gray-900/60 dark:text-gray-300 dark:hover:border-green-600 dark:hover:text-green-400"
              @click="goToSearch(term)"
            >
              {{ term }}
            </button>
          </div>
        </div>

        <!-- Scroll cue -->
        <div class="pb-10 text-center">
          <a
            href="#how-it-works"
            class="inline-flex flex-col items-center gap-1 text-xs font-medium text-gray-400 transition-colors hover:text-green-600 dark:text-gray-500 dark:hover:text-green-400"
          >
            More about Foodeez
            <svg class="h-4 w-4 animate-bounce" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 14l-7 7m0 0l-7-7m7 7V3" />
            </svg>
          </a>
        </div>
      </section>

      <!-- ── How it works ────────────────────────────────────────────────── -->
      <section id="how-it-works" class="relative scroll-mt-20 py-20">
        <div class="mx-auto max-w-6xl px-4 sm:px-6">
          <div class="mx-auto max-w-2xl text-center">
            <h2 class="text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl dark:text-white">
              How it works
            </h2>
            <p class="mt-4 text-gray-600 dark:text-gray-300">
              Three steps from "what should I eat?" to a week that's already planned.
            </p>
          </div>

          <ol class="mt-14 grid gap-8 md:grid-cols-3">
            <li
              v-for="(step, i) in STEPS"
              :key="step.title"
              class="relative rounded-2xl border border-gray-100 bg-white/70 p-7 backdrop-blur-sm dark:border-gray-800 dark:bg-gray-900/70"
            >
              <span
                class="absolute -top-4 left-7 flex h-8 w-8 items-center justify-center rounded-full bg-green-600 text-sm font-bold text-white shadow"
                >{{ i + 1 }}</span
              >
              <span class="mb-4 block text-3xl">{{ step.emoji }}</span>
              <h3 class="mb-2 text-lg font-bold text-gray-900 dark:text-gray-100">{{ step.title }}</h3>
              <p class="text-sm leading-relaxed text-gray-600 dark:text-gray-300">{{ step.body }}</p>
            </li>
          </ol>
        </div>
      </section>

      <!-- ── Features ────────────────────────────────────────────────────── -->
      <!--
        A frosted band rather than an opaque one: the drifting shapes stay visible behind it
        but blurred, which keeps the long body copy on the cards fully legible.
      -->
      <section
        id="features"
        class="relative scroll-mt-20 border-y border-gray-100/80 bg-gray-50/60 py-20 backdrop-blur-sm dark:border-gray-800/80 dark:bg-gray-900/50"
      >
        <div class="mx-auto max-w-6xl px-4 sm:px-6">
          <div class="mx-auto max-w-2xl text-center">
            <h2 class="text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl dark:text-white">
              Everything after the search
            </h2>
            <p class="mt-4 text-gray-600 dark:text-gray-300">
              Search is open to everyone. Sign up free and the rest of Foodeez opens up too.
            </p>
          </div>

          <div class="mt-14 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            <div
              v-for="feature in FEATURES"
              :key="feature.title"
              class="rounded-2xl bg-white/85 p-6 shadow-sm ring-1 ring-gray-100 backdrop-blur-sm transition-shadow hover:shadow-md dark:bg-gray-950/80 dark:ring-gray-800"
            >
              <div
                class="mb-4 flex h-11 w-11 items-center justify-center rounded-xl bg-green-100 text-xl dark:bg-green-900/40"
              >
                {{ feature.emoji }}
              </div>
              <h3 class="mb-2 font-bold text-gray-900 dark:text-gray-100">{{ feature.title }}</h3>
              <p class="text-sm leading-relaxed text-gray-600 dark:text-gray-300">{{ feature.body }}</p>
            </div>
          </div>
        </div>
      </section>

      <!-- ── FAQ ─────────────────────────────────────────────────────────── -->
      <section id="faq" class="relative scroll-mt-20 py-20">
        <div class="mx-auto max-w-3xl px-4 sm:px-6">
          <h2
            class="text-center text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl dark:text-white"
          >
            Questions, answered
          </h2>

          <dl
            class="mt-12 divide-y divide-gray-100 rounded-2xl border border-gray-100 bg-white/70 px-6 backdrop-blur-sm dark:divide-gray-800 dark:border-gray-800 dark:bg-gray-900/70"
          >
            <div v-for="(item, i) in FAQ" :key="item.q" class="py-5">
              <dt>
                <button
                  type="button"
                  class="flex w-full items-center justify-between gap-4 text-left"
                  :aria-expanded="openFaq === i"
                  @click="openFaq = openFaq === i ? null : i"
                >
                  <span class="font-semibold text-gray-900 dark:text-gray-100">{{ item.q }}</span>
                  <svg
                    class="h-5 w-5 shrink-0 text-gray-400 transition-transform"
                    :class="openFaq === i ? 'rotate-180' : ''"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                </button>
              </dt>
              <dd
                v-if="openFaq === i"
                class="mt-3 pr-9 text-sm leading-relaxed text-gray-600 dark:text-gray-300"
              >
                {{ item.a }}
              </dd>
            </div>
          </dl>
        </div>
      </section>
    </div>

    <!-- ── Closing CTA ───────────────────────────────────────────────────── -->
    <!-- Deliberately solid: a hard colour block ends the scroll and lands the CTA. -->
    <section class="bg-gradient-to-br from-green-600 to-emerald-600 py-20">
      <div class="mx-auto max-w-3xl px-4 text-center sm:px-6">
        <h2 class="text-3xl font-bold tracking-tight text-white sm:text-4xl">
          Start with a search. Stay for the plan.
        </h2>
        <p class="mx-auto mt-4 max-w-lg text-green-50">
          Browsing costs nothing. When you're ready to track and plan, an account is free too.
        </p>
        <div class="mt-8 flex flex-wrap items-center justify-center gap-3">
          <RouterLink
            to="/search"
            class="rounded-xl bg-white px-6 py-3 font-semibold text-green-700 shadow-sm transition-transform hover:-translate-y-0.5"
          >
            Browse recipes
          </RouterLink>
          <RouterLink
            to="/auth/register"
            class="rounded-xl border border-white/40 px-6 py-3 font-semibold text-white transition-colors hover:bg-white/10"
          >
            Create a free account
          </RouterLink>
        </div>
      </div>
    </section>
  </PublicLayout>
</template>
