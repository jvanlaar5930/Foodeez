<script setup lang="ts">
import { ref, watch } from 'vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import { useDebouncedSearch } from '@/composables/useDebouncedSearch';
import { recipeService, type RecipeSuggestion } from '@/services/recipeService';

interface Props {
  modelValue?: string;
  placeholder?: string;
  size?: 'md' | 'lg';
  autofocus?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: '',
  placeholder: 'Search any recipe — "chicken curry", "vegan tacos", "high protein breakfast"…',
  size: 'md',
  autofocus: false,
});

const emit = defineEmits<{
  'update:modelValue': [value: string];
  /** Fired when the user commits a search, either by submitting or picking a suggestion. */
  search: [query: string, suggestion?: RecipeSuggestion];
}>();

const query = ref(props.modelValue);
const isOpen = ref(false);
const highlighted = ref(-1);
const inputEl = ref<HTMLInputElement | null>(null);

// A dropdown should feel quicker than a page of results, hence the shorter delay. The
// signal is passed on, so typing past a suggestion request actually cancels it.
const {
  results: suggestions,
  isSearching: isLoading,
  onInput: suggest,
  reset: clearSuggestions,
} = useDebouncedSearch<RecipeSuggestion>(
  (text, signal) => recipeService.autocomplete(text, signal),
  { delayMs: 220 },
);

// The list opens itself as results arrive and closes when there are none left to show.
watch(suggestions, (found) => {
  isOpen.value = found.length > 0;
});

watch(
  () => props.modelValue,
  (v) => {
    if (v !== query.value) query.value = v;
  }
);

watch(query, (value) => {
  emit('update:modelValue', value);
  highlighted.value = -1;
  suggest(value);
});

function submit() {
  const q = query.value.trim();
  if (!q) {
    inputEl.value?.focus();
    return;
  }
  isOpen.value = false;
  clearSuggestions();
  emit('search', q);
}

function pick(suggestion: RecipeSuggestion) {
  query.value = suggestion.name;
  isOpen.value = false;
  emit('search', suggestion.name, suggestion);
}

/** Delay the close so a click on a suggestion still registers before blur hides it. */
function closeSoon() {
  setTimeout(() => {
    isOpen.value = false;
    highlighted.value = -1;
  }, 150);
}

function onKeydown(e: KeyboardEvent) {
  if (!isOpen.value || suggestions.value.length === 0) return;

  if (e.key === 'ArrowDown') {
    e.preventDefault();
    highlighted.value = (highlighted.value + 1) % suggestions.value.length;
  } else if (e.key === 'ArrowUp') {
    e.preventDefault();
    highlighted.value =
      highlighted.value <= 0 ? suggestions.value.length - 1 : highlighted.value - 1;
  } else if (e.key === 'Enter' && highlighted.value >= 0) {
    e.preventDefault();
    pick(suggestions.value[highlighted.value]);
  } else if (e.key === 'Escape') {
    isOpen.value = false;
    highlighted.value = -1;
  }
}

/** Bold the typed substring inside a suggestion so the match is obvious. */
function highlightMatch(name: string): string {
  const q = query.value.trim();
  const escaped = q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const html = name.replace(/[<>&]/g, (c) => ({ '<': '&lt;', '>': '&gt;', '&': '&amp;' })[c]!);
  if (!escaped) return html;
  return html.replace(new RegExp(`(${escaped})`, 'i'), '<strong class="font-bold">$1</strong>');
}


defineExpose({ focus: () => inputEl.value?.focus() });
</script>

<template>
  <form class="relative w-full" role="search" @submit.prevent="submit">
    <div
      :class="[
        'relative flex items-center rounded-2xl bg-white dark:bg-gray-900 shadow-lg shadow-green-900/5 ring-1 ring-gray-200 dark:ring-gray-700 transition-shadow focus-within:ring-2 focus-within:ring-green-500 dark:focus-within:ring-green-400',
        size === 'lg' ? 'p-1.5' : 'p-1',
      ]"
    >
      <svg
        class="ml-3 h-5 w-5 shrink-0 text-gray-400 dark:text-gray-500"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M21 21l-4.35-4.35M11 19a8 8 0 100-16 8 8 0 000 16z"
        />
      </svg>

      <input
        ref="inputEl"
        v-model="query"
        type="search"
        :placeholder="placeholder"
        :autofocus="autofocus"
        autocomplete="off"
        role="combobox"
        aria-label="Search recipes"
        :aria-expanded="isOpen"
        aria-controls="recipe-suggestions"
        :class="[
          'w-full min-w-0 bg-transparent px-3 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none [&::-webkit-search-cancel-button]:hidden',
          size === 'lg' ? 'py-3 text-base' : 'py-2.5 text-sm',
        ]"
        @keydown="onKeydown"
        @focus="isOpen = suggestions.length > 0"
        @blur="closeSoon"
      />

      <LoadingSpinner v-if="isLoading" size="sm" class="mr-2 shrink-0" />

      <button
        type="submit"
        :class="[
          'shrink-0 rounded-xl bg-green-600 font-semibold text-white transition-colors hover:bg-green-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-green-500 focus-visible:ring-offset-2 dark:focus-visible:ring-offset-gray-900',
          size === 'lg' ? 'px-6 py-3 text-base' : 'px-4 py-2 text-sm',
        ]"
      >
        <span class="hidden sm:inline">Find Recipes</span>
        <span class="sm:hidden">Go</span>
      </button>
    </div>

    <!-- Autocomplete dropdown -->
    <ul
      v-if="isOpen && suggestions.length"
      id="recipe-suggestions"
      role="listbox"
      class="absolute left-0 right-0 top-full z-50 mt-2 max-h-80 overflow-y-auto rounded-2xl border border-gray-200 bg-white py-2 text-left shadow-xl dark:border-gray-700 dark:bg-gray-900"
    >
      <li v-for="(s, i) in suggestions" :key="s.name" role="option" :aria-selected="i === highlighted">
        <button
          type="button"
          :class="[
            'flex w-full items-center gap-3 px-4 py-2.5 text-left text-sm transition-colors',
            i === highlighted
              ? 'bg-green-50 text-green-800 dark:bg-green-900/30 dark:text-green-300'
              : 'text-gray-700 hover:bg-gray-50 dark:text-gray-300 dark:hover:bg-gray-800',
          ]"
          @mouseenter="highlighted = i"
          @mousedown.prevent="pick(s)"
        >
          <img
            v-if="s.imageUrl"
            :src="s.imageUrl"
            alt=""
            class="h-8 w-8 shrink-0 rounded-lg object-cover"
          />
          <span
            v-else
            class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-green-100 text-sm dark:bg-green-900/40"
            >🍽️</span
          >
          <span class="truncate" v-html="highlightMatch(s.name)" />
          <span
            v-if="s.recipeId"
            class="ml-auto shrink-0 rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-500 dark:bg-gray-800 dark:text-gray-400"
            >Saved</span
          >
        </button>
      </li>
    </ul>
  </form>
</template>
