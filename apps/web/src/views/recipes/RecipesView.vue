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

      <AppAlert v-if="error" variant="error" :message="error" class="mb-6" />

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
      @close="closeDetail"
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
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter, RouterLink } from 'vue-router';
import AppLayout from '@/components/layout/AppLayout.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import EmptyState from '@/components/ui/EmptyState.vue';
import AppAlert from '@/components/ui/AppAlert.vue';
import RecipeSearchBar from '@/components/recipe/RecipeSearchBar.vue';
import RecipeCard from '@/components/recipe/RecipeCard.vue';
import RecipeDetailModal from '@/components/recipe/RecipeDetailModal.vue';
import { usePagedRecipes } from '@/composables/usePagedRecipes';
import { useRecipeDetail } from '@/composables/useRecipeDetail';
import type { RecipeSuggestion } from '@/services/recipeService';

const route = useRoute();
const router = useRouter();

const searchQuery = ref((route.query.q as string) ?? '');
const activeTags = ref(new Set<string>());

const {
  recipes,
  isLoading,
  isLoadingMore,
  hasMore,
  error,
  sentinel,
  load: loadPage,
  loadMore,
} = usePagedRecipes();

const {
  selected: selectedRecipe,
  isLoading: isDetailLoading,
  open: openRecipe,
  openById,
  retry: retryDetail,
  close: closeDetail,
} = useRecipeDetail();

const FILTER_TAGS = ['Vegetarian', 'Vegan', 'High-Protein', 'Low-Carb', 'Quick', 'Gluten-Free', 'Dairy-Free'];

// Tag filter applied client-side on top of server results
const filteredRecipes = computed(() => {
  const tags = Array.from(activeTags.value);
  if (tags.length === 0) return recipes.value;
  return recipes.value.filter(r =>
    tags.every(t => r.tags?.toLowerCase().includes(t.toLowerCase()))
  );
});

function toggleTag(tag: string) {
  const next = new Set(activeTags.value);
  if (next.has(tag)) next.delete(tag); else next.add(tag);
  activeTags.value = next;
}

// Committing a search rewrites the URL; the route watcher below does the fetching.
async function runSearch(q: string, suggestion?: RecipeSuggestion) {
  if (suggestion?.recipeId) {
    // Opens the picked recipe straight away; the text search below still runs behind it.
    await openById(suggestion.recipeId);
  }
  router.push({ path: '/recipes', query: { q } });
}

// Re-fetch when route query changes (navigated from dashboard)
watch(() => route.query.q, (q) => {
  searchQuery.value = (q as string) ?? '';
  loadPage(searchQuery.value);
});

onMounted(() => loadPage(searchQuery.value));
</script>
