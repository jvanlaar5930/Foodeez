<script setup lang="ts">
import EmptyState from '@/components/ui/EmptyState.vue';
import { ref, computed, watch, onMounted } from 'vue';
import { useRoute, useRouter, RouterLink } from 'vue-router';
import PublicLayout from '@/components/layout/PublicLayout.vue';
import RecipeSearchBar from '@/components/recipe/RecipeSearchBar.vue';
import RecipeCard from '@/components/recipe/RecipeCard.vue';
import RecipeDetailModal from '@/components/recipe/RecipeDetailModal.vue';
import AddToMealPlanModal from '@/components/mealplan/AddToMealPlanModal.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import { useAuthStore } from '@/stores/auth';
import { usePagedRecipes } from '@/composables/usePagedRecipes';
import { useRecipeDetail } from '@/composables/useRecipeDetail';
import { useRecipeFilterTags } from '@/composables/useRecipeFilterTags';
import type { RecipeSuggestion } from '@/services/recipeService';
import type { Recipe } from '@foodeez/shared';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

// Administrator-managed, so this is a ref rather than a constant.
const { filterTags } = useRecipeFilterTags();

const SORTS = [
  { value: 'relevance', label: 'Best match' },
  { value: 'calories-asc', label: 'Fewest calories' },
  { value: 'protein-desc', label: 'Most protein' },
  { value: 'time-asc', label: 'Quickest' },
] as const;

type Sort = (typeof SORTS)[number]['value'];

const searchInput = ref('');
/** The recipe being placed in the calendar, which is what opens the day picker. */
const planningRecipe = ref<Recipe | null>(null);
const activeTags = ref(new Set<string>());
const sort = ref<Sort>('relevance');

// The grid and its paging. `sentinel` is bound below the results; scrolling it into view
// requests the next page.
const {
  recipes,
  isLoading,
  isLoadingMore,
  hasMore,
  totalAvailable,
  error,
  sentinel,
  load: loadPage,
  loadMore,
} = usePagedRecipes();

// The detail panel over it.
const {
  selected: selectedRecipe,
  isLoading: isDetailLoading,
  open: openRecipe,
  openById,
  retry: retryDetail,
  close: closeSelected,
} = useRecipeDetail();

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
  filterTags.value.filter(
    (tag) =>
      activeTags.value.has(tag) ||
      recipes.value.some((r) => r.tags?.toLowerCase().includes(tag.toLowerCase()))
  )
);

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
  searchInput.value = activeQuery.value;
  activeTags.value = new Set();

  await loadPage(activeQuery.value);
  await openRequestedRecipe();
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

  await openById(id);
}

function closeDetail() {
  closeSelected();
  if (route.query.recipe) {
    const { recipe: _drop, ...rest } = route.query;
    router.replace({ path: '/search', query: rest });
  }
}

watch(() => route.query.q, load);
onMounted(load);
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
      <EmptyState v-else-if="filteredRecipes.length === 0" emoji="🔍" title="No recipes found">
        <template #description>
          <template v-if="activeTags.size">
            Nothing matches every filter at once. Try clearing a filter or two.
          </template>
          <template v-else>
            Try a broader term — an ingredient like "salmon" or a dish like "curry" usually
            works better than a full sentence.
          </template>
        </template>

        <template v-if="activeTags.size" #actions>
          <button
            type="button"
            class="rounded-xl bg-green-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-green-700"
            @click="activeTags = new Set()"
          >
            Clear filters
          </button>
        </template>
      </EmptyState>

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
        <button
          v-if="authStore.isAuthenticated"
          type="button"
          class="block w-full rounded-xl bg-green-600 py-3 text-center font-semibold text-white transition-colors hover:bg-green-700"
          @click="planningRecipe = selectedRecipe"
        >
          Add to Meal Plan
        </button>
        <RouterLink
          v-else
          to="/auth/register"
          class="block w-full rounded-xl bg-green-600 py-3 text-center font-semibold text-white transition-colors hover:bg-green-700"
        >
          Sign up free to save this recipe
        </RouterLink>
      </template>
    </RecipeDetailModal>

    <!-- Outside the detail dialog so it is not clipped by the panel it was opened from. -->
    <AddToMealPlanModal
      v-if="planningRecipe"
      :model-value="true"
      :recipe="planningRecipe"
      @update:model-value="planningRecipe = null"
    />
  </PublicLayout>
</template>
