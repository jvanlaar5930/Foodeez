import { nextTick, onUnmounted, ref, type Ref } from 'vue';
import { recipeService } from '@/services/recipeService';
import type { Recipe } from '@foodeez/shared';

export interface PagedRecipes {
  recipes: Ref<Recipe[]>;
  /** True for the first page, which replaces the grid. Separate from `isLoadingMore`. */
  isLoading: Ref<boolean>;
  /** True while a further page is being appended below what is already shown. */
  isLoadingMore: Ref<boolean>;
  hasMore: Ref<boolean>;
  /** How many the server says exist in total, when it says. */
  totalAvailable: Ref<number | null>;
  error: Ref<string | null>;

  /** The element watched to trigger the next page. Bind it with `ref="sentinel"`. */
  sentinel: Ref<HTMLElement | null>;

  /** Loads page one for a query, replacing what is shown. An empty query browses. */
  load(query?: string): Promise<void>;
  /** Appends the next page. Safe to call repeatedly; overlapping calls are ignored. */
  loadMore(): Promise<void>;
}

const FIRST_PAGE_FAILED = 'We could not reach the recipe service. Please try again in a moment.';
const NEXT_PAGE_FAILED = 'Could not load more results.';

/**
 * An infinitely scrolling page of recipes, searched or browsed.
 *
 * Both recipe views had their own copy of this - about ninety near-identical lines each,
 * including the dedupe on append, the guard against overlapping scroll events, and the
 * observer re-arm after the grid re-renders. They had drifted in the way that matters least
 * to write and most to read: only one of them told you when a page had failed to load.
 */
export function usePagedRecipes(): PagedRecipes {
  const recipes = ref<Recipe[]>([]);
  const isLoading = ref(false);
  const isLoadingMore = ref(false);
  const hasMore = ref(false);
  const totalAvailable = ref<number | null>(null);
  const error = ref<string | null>(null);
  const sentinel = ref<HTMLElement | null>(null);

  let currentPage = 1;
  let currentQuery = '';
  let observer: IntersectionObserver | null = null;

  function fetchPage(page: number) {
    return currentQuery
      ? recipeService.searchRecipes(currentQuery, page)
      : recipeService.getRecipes(page);
  }

  async function load(query = ''): Promise<void> {
    currentQuery = query.trim();
    currentPage = 1;
    isLoading.value = true;
    error.value = null;

    try {
      const result = await fetchPage(1);
      recipes.value = result.items;
      hasMore.value = result.hasMore;
      totalAvailable.value = result.totalAvailable;
    } catch {
      recipes.value = [];
      hasMore.value = false;
      error.value = FIRST_PAGE_FAILED;
    } finally {
      isLoading.value = false;
      // Re-arm after the grid re-renders: the sentinel node it was watching is now gone.
      await nextTick();
      observeSentinel();
    }
  }

  async function loadMore(): Promise<void> {
    if (isLoadingMore.value || isLoading.value || !hasMore.value) {
      return;
    }

    isLoadingMore.value = true;
    const next = currentPage + 1;

    try {
      const result = await fetchPage(next);

      // Dedupe: a recipe cached between pages could otherwise appear twice.
      const seen = new Set(recipes.value.map((r) => r.id));
      recipes.value = [...recipes.value, ...result.items.filter((r) => !seen.has(r.id))];

      currentPage = next;
      // An empty page ends the scroll even if the server still claims there is more.
      hasMore.value = result.hasMore && result.items.length > 0;
      if (result.totalAvailable !== null) {
        totalAvailable.value = result.totalAvailable;
      }
    } catch {
      // Stop auto-loading on failure, so a broken connection does not spin against the
      // server on every scroll. Any manual "load more" button stays available.
      hasMore.value = false;
      error.value = NEXT_PAGE_FAILED;
    } finally {
      isLoadingMore.value = false;
    }
  }

  function observeSentinel(): void {
    observer?.disconnect();
    if (!sentinel.value) {
      return;
    }

    observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          loadMore();
        }
      },
      // Start fetching a little before the sentinel is actually visible.
      { rootMargin: '400px 0px' },
    );
    observer.observe(sentinel.value);
  }

  onUnmounted(() => observer?.disconnect());

  return { recipes, isLoading, isLoadingMore, hasMore, totalAvailable, error, sentinel, load, loadMore };
}
