<template>
  <AppLayout>
    <div class="p-6 max-w-6xl mx-auto">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-6">Recipes</h1>

      <!-- Search -->
      <div class="mb-5">
        <RecipeSearchBar v-model="searchQuery" @search="runSearch" />
      </div>

      <!-- Tag filters -->
      <div class="flex flex-wrap gap-2 mb-6">
        <button v-for="tag in FILTER_TAGS" :key="tag" type="button"
          class="px-3 py-1.5 rounded-full border text-sm font-medium transition-colors"
          :class="activeTags.has(tag)
            ? 'bg-green-600 border-green-600 text-white'
            : 'border-gray-300 text-gray-600 hover:border-green-400 dark:border-gray-700 dark:text-gray-400 dark:hover:border-green-600'"
          @click="toggleTag(tag)">
          {{ tag }}
        </button>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="flex justify-center py-16">
        <LoadingSpinner size="lg" />
      </div>

      <!-- Empty -->
      <EmptyState v-else-if="filteredRecipes.length === 0"
        title="No recipes found"
        description="Try adjusting your search or filters"
      />

      <!-- Recipe grid -->
      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
        <RecipeCard
          v-for="recipe in filteredRecipes"
          :key="recipe.id"
          :recipe="recipe"
          @click="openRecipe(recipe)"
        />
      </div>

      <!-- Lazy-load trigger -->
      <div v-if="!isLoading && hasMore" ref="sentinel" class="py-8 text-center">
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
    </div>

    <!-- Recipe detail panel -->
    <RecipeDetailModal
      v-if="selectedRecipe"
      :recipe="selectedRecipe"
      :loading="isDetailLoading"
      @close="selectedRecipe = null"
      @retry="retryDetail"
    >
      <template #actions>
        <RouterLink
          to="/meal-plan"
          class="block w-full rounded-xl bg-green-600 py-3 text-center font-semibold text-white transition-colors hover:bg-green-700"
        >
          Add to Meal Plan
        </RouterLink>
      </template>
    </RecipeDetailModal>
  </AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, nextTick, watch } from 'vue';
import { useRoute, useRouter, RouterLink } from 'vue-router';
import AppLayout from '@/components/layout/AppLayout.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import EmptyState from '@/components/ui/EmptyState.vue';
import RecipeSearchBar from '@/components/recipe/RecipeSearchBar.vue';
import RecipeCard from '@/components/recipe/RecipeCard.vue';
import RecipeDetailModal from '@/components/recipe/RecipeDetailModal.vue';
import { recipeService, type RecipeSuggestion } from '@/services/recipeService';
import type { Recipe } from '@foodeez/shared';

const route = useRoute();
const router = useRouter();
const recipes = ref<Recipe[]>([]);
const selectedRecipe = ref<Recipe | null>(null);
const isDetailLoading = ref(false);
const searchQuery = ref((route.query.q as string) ?? '');
const activeTags = ref(new Set<string>());
const isLoading = ref(true);
const isLoadingMore = ref(false);
const hasMore = ref(false);

const sentinel = ref<HTMLElement | null>(null);
let observer: IntersectionObserver | null = null;
let currentPage = 1;

const FILTER_TAGS = ['Vegetarian', 'Vegan', 'High-Protein', 'Low-Carb', 'Quick', 'Gluten-Free', 'Dairy-Free'];

// Tag filter applied client-side on top of server results
const filteredRecipes = computed(() => {
  const tags = Array.from(activeTags.value);
  if (tags.length === 0) return recipes.value;
  return recipes.value.filter(r =>
    tags.every(t => r.tags?.toLowerCase().includes(t.toLowerCase()))
  );
});

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
  if (next.has(tag)) next.delete(tag); else next.add(tag);
  activeTags.value = next;
}

async function fetchRecipes(query?: string) {
  isLoading.value = true;
  currentPage = 1;
  try {
    const q = (query ?? searchQuery.value).trim();
    const result = q
      ? await recipeService.searchRecipes(q, 1)
      : await recipeService.getRecipes(1);
    recipes.value = result.items;
    hasMore.value = result.hasMore;
  } finally {
    isLoading.value = false;
    await nextTick();
    observeSentinel();
  }
}

/** Appends the next page; guarded against overlapping scroll events. */
async function loadMore() {
  if (isLoadingMore.value || isLoading.value || !hasMore.value) return;

  isLoadingMore.value = true;
  const next = currentPage + 1;

  try {
    const q = searchQuery.value.trim();
    const result = q
      ? await recipeService.searchRecipes(q, next)
      : await recipeService.getRecipes(next);

    const seen = new Set(recipes.value.map((r) => r.id));
    recipes.value = [...recipes.value, ...result.items.filter((r) => !seen.has(r.id))];
    currentPage = next;
    hasMore.value = result.hasMore && result.items.length > 0;
  } catch {
    hasMore.value = false;
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
    { rootMargin: '400px 0px' },
  );
  observer.observe(sentinel.value);
}

// Committing a search rewrites the URL; the route watcher below does the fetching.
async function runSearch(q: string, suggestion?: RecipeSuggestion) {
  if (suggestion?.recipeId) {
    try {
      selectedRecipe.value = await recipeService.getRecipeById(suggestion.recipeId);
    } catch {
      // Fall through to the plain text search below.
    }
  }
  router.push({ path: '/recipes', query: { q } });
}

// Re-fetch when route query changes (navigated from dashboard)
watch(() => route.query.q, (q) => {
  searchQuery.value = (q as string) ?? '';
  fetchRecipes(searchQuery.value);
});

onMounted(() => fetchRecipes());
onBeforeUnmount(() => observer?.disconnect());
</script>
