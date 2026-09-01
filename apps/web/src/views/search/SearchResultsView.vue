<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount, nextTick } from 'vue';
import { useRoute, useRouter, RouterLink } from 'vue-router';
import PublicLayout from '@/components/layout/PublicLayout.vue';
import RecipeSearchBar from '@/components/recipe/RecipeSearchBar.vue';
import RecipeCard from '@/components/recipe/RecipeCard.vue';
import RecipeDetailModal from '@/components/recipe/RecipeDetailModal.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import { useAuthStore } from '@/stores/auth';
import { recipeService, type RecipeSuggestion } from '@/services/recipeService';
import type { Recipe } from '@foodeez/shared';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const FILTER_TAGS = [
  'Vegetarian',
  'Vegan',
  'High-Protein',
  'Low-Carb',
  'Quick',
  'Gluten-Free',
  'Dairy-Free',
];

const SORTS = [
  { value: 'relevance', label: 'Best match' },
  { value: 'calories-asc', label: 'Fewest calories' },
  { value: 'protein-desc', label: 'Most protein' },
  { value: 'time-asc', label: 'Quickest' },
] as const;

type Sort = (typeof SORTS)[number]['value'];

const searchInput = ref('');
const recipes = ref<Recipe[]>([]);
const selectedRecipe = ref<Recipe | null>(null);
const isDetailLoading = ref(false);
const activeTags = ref(new Set<string>());
const sort = ref<Sort>('relevance');
const isLoading = ref(false);
const isLoadingMore = ref(false);
const hasMore = ref(false);
const totalAvailable = ref<number | null>(null);
const error = ref<string | null>(null);

// Sentinel below the grid; when it scrolls into view the next page is requested.
const sentinel = ref<HTMLElement | null>(null);
let observer: IntersectionObserver | null = null;
let currentPage = 1;

const activeQuery = computed(() => ((route.query.q as string) ?? '').trim());

const filteredRecipes = computed(() => {
  const tags = Array.from(activeTags.value);
  const list = tags.length
    ? recipes.value.filter((r) =>
        tags.every((t) => r.tags?.toLowerCase().includes(t.toLowerCase()))
      )
    : [...recipes.value];

  switch (sort.value) {
    case 'calories-asc':
      return list.sort(
        (a, b) => a.nutritionalInfoPerServing.calories - b.nutritionalInfoPerServing.calories
      );
    case 'protein-desc':
      return list.sort(
        (a, b) => b.nutritionalInfoPerServing.protein - a.nutritionalInfoPerServing.protein
      );
    case 'time-asc':
      return list.sort(
        (a, b) =>
          a.prepTimeMinutes + a.cookTimeMinutes - (b.prepTimeMinutes + b.cookTimeMinutes)
      );
    default:
      return list;
  }
});

/** Tags that at least one result carries — no point offering a filter that empties the page. */
const availableTags = computed(() =>
  FILTER_TAGS.filter(
    (tag) =>
      activeTags.value.has(tag) ||
      recipes.value.some((r) => r.tags?.toLowerCase().includes(tag.toLowerCase()))
  )
);

/**
 * Re-requests the recipe after the method failed to load. Worth a button of its own: the
 * server retries upstream on every read, so a second attempt genuinely can succeed where
 * the first did not.
 */
async function retryDetail() {
  const current = selectedRecipe.value;
  if (!current) return;

  isDetailLoading.value = true;
  try {
    const full = await recipeService.getRecipeById(current.id);
    if (selectedRecipe.value?.id === current.id) selectedRecipe.value = full;
  } catch {
    // Keep what is on screen; the panel still offers another try.
  } finally {
    isDetailLoading.value = false;
  }
}

function toggleTag(tag: string) {
  const next = new Set(activeTags.value);
  if (next.has(tag)) next.delete(tag);
  else next.add(tag);
  activeTags.value = next;
}

/** Submitting from this page just rewrites the URL; the route watcher does the fetching. */
function runSearch(q: string, suggestion?: RecipeSuggestion) {
  router.push({
    path: '/search',
    query: suggestion?.recipeId ? { q, recipe: suggestion.recipeId } : { q },
  });
}

async function load() {
  const q = activeQuery.value;
  searchInput.value = q;
  activeTags.value = new Set();
  currentPage = 1;
  isLoading.value = true;
  error.value = null;

  try {
    const result = q
      ? await recipeService.searchRecipes(q, 1)
      : await recipeService.getRecipes(1);
    recipes.value = result.items;
    hasMore.value = result.hasMore;
    totalAvailable.value = result.totalAvailable;
    await openRequestedRecipe();
  } catch {
    recipes.value = [];
    hasMore.value = false;
    error.value = 'We could not reach the recipe service. Please try again in a moment.';
  } finally {
    isLoading.value = false;
    // Re-arm after the grid re-renders, since the old sentinel node is gone.
    await nextTick();
    observeSentinel();
  }
}

/** Appends the next page. Guarded so overlapping scroll events cannot double-fetch. */
async function loadMore() {
  if (isLoadingMore.value || isLoading.value || !hasMore.value) return;

  isLoadingMore.value = true;
  const next = currentPage + 1;

  try {
    const q = activeQuery.value;
    const result = q
      ? await recipeService.searchRecipes(q, next)
      : await recipeService.getRecipes(next);

    // Dedupe defensively: a recipe cached between pages could otherwise appear twice.
    const seen = new Set(recipes.value.map((r) => r.id));
    recipes.value = [...recipes.value, ...result.items.filter((r) => !seen.has(r.id))];

    currentPage = next;
    hasMore.value = result.hasMore && result.items.length > 0;
    if (result.totalAvailable !== null) totalAvailable.value = result.totalAvailable;
  } catch {
    // Stop auto-loading on failure; the manual button below remains available.
    hasMore.value = false;
    error.value = 'Could not load more results.';
  } finally {
    isLoadingMore.value = false;
  }
}

function observeSentinel() {
  observer?.disconnect();
  if (!sentinel.value) return;

  observer = new IntersectionObserver(
    (entries) => {
      if (entries.some((e) => e.isIntersecting)) loadMore();
    },
    // Start fetching a little before the sentinel is actually visible.
    { rootMargin: '400px 0px' },
  );
  observer.observe(sentinel.value);
}

/**
 * Show the card's data straight away so the modal opens instantly, then swap in the full
 * record. Search results carry no ingredients or steps - the API fills those in on first read.
 */
async function openRecipe(recipe: Recipe) {
  selectedRecipe.value = recipe;
  if (recipe.ingredients.length && recipe.instructions) return;

  isDetailLoading.value = true;
  try {
    const full = await recipeService.getRecipeById(recipe.id);
    // Guard against a slow response landing after the user has moved on.
    if (selectedRecipe.value?.id === recipe.id) selectedRecipe.value = full;
  } catch {
    // Leave the partial record on screen; the modal says which parts are missing.
  } finally {
    isDetailLoading.value = false;
  }
}

/** `?recipe=<id>` (set when a suggestion resolved to a saved recipe) opens straight to detail. */
async function openRequestedRecipe() {
  const id = route.query.recipe as string | undefined;
  if (!id) return;

  const fromResults = recipes.value.find((r) => r.id === id);
  if (fromResults) {
    await openRecipe(fromResults);
    return;
  }
  try {
    selectedRecipe.value = await recipeService.getRecipeById(id);
  } catch {
    // A stale id just means no modal — the result list still stands.
  }
}

function closeDetail() {
  selectedRecipe.value = null;
  if (route.query.recipe) {
    const { recipe: _drop, ...rest } = route.query;
    router.replace({ path: '/search', query: rest });
  }
}

watch(() => route.query.q, load);
onMounted(load);
onBeforeUnmount(() => observer?.disconnect());
</script>

<template>
  <PublicLayout>
    <div class="mx-auto max-w-6xl px-4 py-8 sm:px-6">
      <!-- Search bar -->
      <div class="mb-8">
        <RecipeSearchBar v-model="searchInput" @search="runSearch" />
      </div>

      <!-- Result header -->
      <div class="mb-6 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">
            <template v-if="activeQuery">
              Results for <span class="text-green-700 dark:text-green-400">"{{ activeQuery }}"</span>
            </template>
            <template v-else>Browse recipes</template>
          </h1>
<p v-if="!isLoading" class="mt-1 text-sm text-gray-500 dark:text-gray-400">
            Showing {{ filteredRecipes.length }}
            {{ filteredRecipes.length === 1 ? 'recipe' : 'recipes' }}<template
              v-if="totalAvailable"
            >
              of {{ totalAvailable.toLocaleString() }}</template
            ><template v-else-if="hasMore">, more as you scroll</template>
            <template v-if="activeTags.size"> &middot; filters apply to loaded results</template>
          </p>
        </div>

        <label class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
          Sort by
          <select
            v-model="sort"
            class="rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm font-medium text-gray-700 focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-700 dark:bg-gray-900 dark:text-gray-200"
          >
            <option v-for="option in SORTS" :key="option.value" :value="option.value">
              {{ option.label }}
            </option>
          </select>
        </label>
      </div>

      <!-- Tag filters -->
      <div v-if="availableTags.length" class="mb-8 flex flex-wrap gap-2">
        <button
          v-for="tag in availableTags"
          :key="tag"
          type="button"
          class="rounded-full border px-3 py-1.5 text-sm font-medium transition-colors"
          :class="
            activeTags.has(tag)
              ? 'border-green-600 bg-green-600 text-white'
              : 'border-gray-300 text-gray-600 hover:border-green-400 hover:text-green-700 dark:border-gray-700 dark:text-gray-400 dark:hover:border-green-600 dark:hover:text-green-400'
          "
          @click="toggleTag(tag)"
        >
          {{ tag }}
        </button>
        <button
          v-if="activeTags.size"
          type="button"
          class="rounded-full px-3 py-1.5 text-sm font-medium text-gray-400 underline-offset-2 hover:underline dark:text-gray-500"
          @click="activeTags = new Set()"
        >
          Clear filters
        </button>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="flex flex-col items-center gap-3 py-24">
        <LoadingSpinner size="lg" />
        <p class="text-sm text-gray-500 dark:text-gray-400">Searching recipes…</p>
      </div>

      <!-- Error -->
      <div
        v-else-if="error"
        class="rounded-2xl border border-red-100 bg-red-50 p-8 text-center dark:border-red-900/40 dark:bg-red-950/30"
      >
        <p class="text-3xl">😕</p>
        <p class="mt-3 font-semibold text-red-700 dark:text-red-300">{{ error }}</p>
        <button
          type="button"
          class="mt-4 rounded-xl bg-red-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-red-700"
          @click="load"
        >
          Try again
        </button>
      </div>

      <!-- Empty -->
      <div v-else-if="filteredRecipes.length === 0" class="py-20 text-center">
        <p class="text-5xl">🔍</p>
        <h2 class="mt-4 text-lg font-semibold text-gray-900 dark:text-gray-100">
          No recipes found
        </h2>
        <p class="mx-auto mt-2 max-w-sm text-sm text-gray-500 dark:text-gray-400">
          <template v-if="activeTags.size">
            Nothing matches every filter at once. Try clearing a filter or two.
          </template>
          <template v-else>
            Try a broader term — an ingredient like "salmon" or a dish like "curry" usually works
            better than a full sentence.
          </template>
        </p>
        <button
          v-if="activeTags.size"
          type="button"
          class="mt-5 rounded-xl bg-green-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-green-700"
          @click="activeTags = new Set()"
        >
          Clear filters
        </button>
      </div>

      <!-- Results -->
      <div v-else class="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
        <RecipeCard
          v-for="recipe in filteredRecipes"
          :key="recipe.id"
          :recipe="recipe"
          @click="openRecipe(recipe)"
        />
      </div>

      <!-- Lazy-load trigger: auto-fetches on approach, with a button for anyone who
           cannot trigger an intersection (keyboard navigation, reduced motion, etc). -->
      <div v-if="!isLoading && !error && hasMore" ref="sentinel" class="py-8 text-center">
        <div v-if="isLoadingMore" class="flex items-center justify-center gap-3">
          <LoadingSpinner size="sm" />
          <span class="text-sm text-gray-500 dark:text-gray-400">Loading more recipes…</span>
        </div>
        <button
          v-else
          type="button"
          class="rounded-xl border border-gray-300 px-5 py-2.5 text-sm font-semibold text-gray-700 transition-colors hover:border-green-400 hover:text-green-700 dark:border-gray-700 dark:text-gray-200 dark:hover:border-green-600 dark:hover:text-green-400"
          @click="loadMore"
        >
          Load more recipes
        </button>
      </div>

      <p
        v-else-if="!isLoading && !error && recipes.length > 0"
        class="py-8 text-center text-sm text-gray-400 dark:text-gray-500"
      >
        That's everything for this search.
      </p>

      <!-- Sign-up nudge for anonymous visitors -->
      <div
        v-if="!isLoading && !authStore.isAuthenticated && filteredRecipes.length > 0"
        class="mt-12 flex flex-col items-center gap-4 rounded-2xl bg-gradient-to-br from-green-600 to-emerald-600 p-8 text-center sm:flex-row sm:justify-between sm:text-left"
      >
        <div>
          <h2 class="text-lg font-bold text-white">Want to keep these?</h2>
          <p class="mt-1 text-sm text-green-50">
            A free account lets you save recipes into a weekly meal plan and track what you eat.
          </p>
        </div>
        <RouterLink
          to="/auth/register"
          class="shrink-0 rounded-xl bg-white px-5 py-2.5 font-semibold text-green-700 transition-transform hover:-translate-y-0.5"
        >
          Create free account
        </RouterLink>
      </div>
    </div>

    <RecipeDetailModal
      v-if="selectedRecipe"
      :recipe="selectedRecipe"
      :loading="isDetailLoading"
      @close="closeDetail"
      @retry="retryDetail"
    >
      <template #actions>
        <RouterLink
          v-if="authStore.isAuthenticated"
          to="/meal-plan"
          class="block w-full rounded-xl bg-green-600 py-3 text-center font-semibold text-white transition-colors hover:bg-green-700"
        >
          Add to Meal Plan
        </RouterLink>
        <RouterLink
          v-else
          to="/auth/register"
          class="block w-full rounded-xl bg-green-600 py-3 text-center font-semibold text-white transition-colors hover:bg-green-700"
        >
          Sign up free to save this recipe
        </RouterLink>
      </template>
    </RecipeDetailModal>
  </PublicLayout>
</template>
