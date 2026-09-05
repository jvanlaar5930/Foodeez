import { useEffect, useRef, useState } from 'react';
import { searchFoodItems } from '@/services/foodItemService';
import type { FoodItemDto } from '@/types';

export interface UseFoodSearch {
  query: string;
  setQuery: (text: string) => void;
  results: FoodItemDto[];
  isSearching: boolean;
  /** Drops the query and its results - after picking a food, or on reset. */
  clear: () => void;
}

/**
 * Search-as-you-type over the food database.
 *
 * The token guard is the part worth having: without it a slow search for "chick" lands after
 * a fast one for "chicken breast" and replaces the right results with stale ones. The screen
 * this came out of had the debounce but not the guard.
 */
export function useFoodSearch(delayMs = 300): UseFoodSearch {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<FoodItemDto[]>([]);
  const [isSearching, setIsSearching] = useState(false);

  const latest = useRef(0);

  useEffect(() => {
    const trimmed = query.trim();
    if (!trimmed) {
      // Retires anything in flight, so its results are discarded rather than landing on a
      // list the reader has already cleared.
      latest.current += 1;
      setResults([]);
      setIsSearching(false);
      return;
    }

    setIsSearching(true);
    const timer = setTimeout(async () => {
      const token = (latest.current += 1);
      try {
        const found = await searchFoodItems(trimmed);
        if (token === latest.current) setResults(found);
      } catch {
        // Search is a convenience; a failure leaves the field usable and the custom-food
        // route still open.
        if (token === latest.current) setResults([]);
      } finally {
        if (token === latest.current) setIsSearching(false);
      }
    }, delayMs);

    return () => clearTimeout(timer);
  }, [query, delayMs]);

  function clear(): void {
    latest.current += 1;
    setQuery('');
    setResults([]);
    setIsSearching(false);
  }

  return { query, setQuery, results, isSearching, clear };
}
