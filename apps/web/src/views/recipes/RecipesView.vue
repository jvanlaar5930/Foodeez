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
        <button v-for="tag in filterTags" :key="tag" type="button"
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
      <EmptyState v-else-if="recipes.length === 0"
        title="No recipes found"
        description="Try adjusting your search or filters"
      />

      <!-- Recipe grid -->
      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
        <RecipeCard
          v-for="recipe in recipes"
          :key="recipe.id"
          :recipe="recipe"
          :can-save="isAuthenticated"
          :is-saved="savedStore.savedIds.has(recipe.id)"
          :is-save-pending="savedStore.pending.has(recipe.id)"
          @click="openRecipe(recipe)"
          @toggle-save="toggleSaved(recipe)"
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
      :can-enhance="isAuthenticated"
      @close="closeDetail"
      @retry="retryDetail"
    >
      <!-- `shown` is whichever version the reader is looking at, so planning an enhanced
           recipe puts the enhanced one in the calendar rather than the original. -->
      <template #actions="{ recipe: shown }">
        <button
          type="button"
          class="block w-full rounded-xl bg-green-600 py-3 text-center font-semibold text-white transition-colors hover:bg-green-700"
          @click="planningRecipe = shown"
        >
          Add to Meal Plan
        </button>
      </template>
    </RecipeDetailModal>

    <!-- Outside the detail dialog so it is not clipped by the panel it was opened from. -->
    <AddToMealPlanModal
      v-if="planningRecipe"
      :model-value="true"
      :recipe="planningRecipe"
      @update:model-value="planningRecipe = null"
    />
  </AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AppLayout from '@/components/layout/AppLayout.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import EmptyState from '@/components/ui/EmptyState.vue';
import AppAlert from '@/components/ui/AppAlert.vue';
import RecipeSearchBar from '@/components/recipe/RecipeSearchBar.vue';
import RecipeCard from '@/components/recipe/RecipeCard.vue';
import RecipeDetailModal from '@/components/recipe/RecipeDetailModal.vue';
import AddToMealPlanModal from '@/components/mealplan/AddToMealPlanModal.vue';
import { usePagedRecipes } from '@/composables/usePagedRecipes';
import { useRecipeDetail } from '@/composables/useRecipeDetail';
import { useRecipeFilterTags } from '@/composables/useRecipeFilterTags';
import { useSavedRecipesStore } from '@/stores/savedRecipes';
import { useAuthStore } from '@/stores/auth';
import { useToastStore } from '@/stores/toast';
import { toRecipeQueryFilters } from '@foodeez/shared';
import type { RecipeSuggestion } from '@/services/recipeService';
import type { Recipe } from '@foodeez/shared';

const route = useRoute();
const router = useRouter();

const searchQuery = ref((route.query.q as string) ?? '');
/** The recipe being placed in the calendar, which is what opens the day picker. */
const planningRecipe = ref<Recipe | null>(null);
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

// Administrator-managed, so this is a ref rather than a constant.
const { filterTags } = useRecipeFilterTags();

const savedStore = useSavedRecipesStore();
const auth = useAuthStore();
const toast = useToastStore();

/**
 * Pressing a pill refetches from page one.
 *
 * The filtering happens in the database now. It used to be done here, over the pages the
 * infinite scroll had already fetched, which meant a pill hid recipes it should have shown
 * purely because nobody had scrolled far enough to load them - the behaviour that made the
 * filters look broken.
 */
function toggleTag(tag: string) {
  const next = new Set(activeTags.value);
  if (next.has(tag)) next.delete(tag); else next.add(tag);
  activeTags.value = next;

  void loadPage(searchQuery.value, toRecipeQueryFilters(next));
}

/**
 * Marking is optimistic, so the card flips before the request lands. The message is not: it
 * says what actually happened, and says nothing at all when the flip was undone.
 */
async function toggleSaved(recipe: Recipe) {
  const nowSaved = await savedStore.toggle(recipe);

  if (nowSaved === null) {
    toast.error(savedStore.error ?? 'Your favorites could not be updated.');
    return;
  }

  // Both are successes - the removal worked - so both announce politely and stay the same
  // length. Only the glyph differs, because the two read almost identically at a glance and
  // the colour is what says which way the change went.
  if (nowSaved) {
    toast.success(`You've added ${recipe.name} to your favorites.`, { icon: '❤️' });
  } else {
    toast.success(`You've removed ${recipe.name} from your favorites.`, { icon: '💔' });
  }
}

// Committing a search rewrites the URL; the route watcher below does the fetching.
async function runSearch(q: string, suggestion?: RecipeSuggestion) {
  if (suggestion?.recipeId) {
    // Opens the picked recipe straight away; the text search below still runs behind it.
    await openById(suggestion.recipeId);
  }
  router.push({ path: '/recipes', query: { q } });
}

const isAuthenticated = computed(() => auth.isAuthenticated);

// Re-fetch when route query changes (navigated from dashboard)
watch(() => route.query.q, (q) => {
  searchQuery.value = (q as string) ?? '';
  loadPage(searchQuery.value, toRecipeQueryFilters(activeTags.value));
});

onMounted(() => {
  loadPage(searchQuery.value, toRecipeQueryFilters(activeTags.value));
  // Only what this user kept, so the hearts are drawn correctly from the first paint.
  if (auth.isAuthenticated) void savedStore.ensureLoaded();
});
</script>
