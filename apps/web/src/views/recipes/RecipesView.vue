<template>
  <AppLayout>
    <div class="p-6 max-w-6xl mx-auto">
      <h1 class="text-2xl font-bold text-gray-900 mb-6">Recipes</h1>

      <!-- Search & filters -->
      <div class="flex flex-col sm:flex-row gap-3 mb-5">
        <div class="relative flex-1">
          <span class="absolute left-3 top-2.5 text-gray-400">🔍</span>
          <input v-model="searchQuery" type="text" placeholder="Search recipes..."
            class="w-full pl-9 pr-4 py-2.5 border border-gray-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-green-500" />
        </div>
      </div>

      <!-- Tag filters -->
      <div class="flex flex-wrap gap-2 mb-6">
        <button v-for="tag in FILTER_TAGS" :key="tag" type="button"
          class="px-3 py-1.5 rounded-full border text-sm font-medium transition-colors"
          :class="activeTags.has(tag) ? 'bg-green-600 border-green-600 text-white' : 'border-gray-300 text-gray-600 hover:border-green-400'"
          @click="toggleTag(tag)">
          {{ tag }}
        </button>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="flex justify-center py-16">
        <LoadingSpinner />
      </div>

      <!-- Empty -->
      <EmptyState v-else-if="filteredRecipes.length === 0"
        title="No recipes found"
        description="Try adjusting your search or filters"
      />

      <!-- Recipe grid -->
      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
        <div v-for="recipe in filteredRecipes" :key="recipe.id"
          class="bg-white rounded-2xl shadow-sm overflow-hidden hover:shadow-md transition-shadow cursor-pointer"
          @click="selectedRecipe = recipe">
          <!-- Recipe image -->
          <div class="h-36 bg-gradient-to-br from-green-100 to-green-200 flex items-center justify-center overflow-hidden">
            <img v-if="recipe.imageUrl" :src="recipe.imageUrl" :alt="recipe.name"
              class="w-full h-full object-cover"
              @error="hideImage" />
            <span v-else class="text-4xl">🍽️</span>
          </div>
          <div class="p-4">
            <div class="flex items-start justify-between gap-2 mb-2">
              <p class="font-bold text-gray-900">{{ recipe.name }}</p>
              <span v-if="recipe.isAIGenerated" class="text-xs bg-orange-100 text-orange-600 font-semibold px-2 py-0.5 rounded-full flex-shrink-0">✨ AI</span>
            </div>
            <p v-if="recipe.description" class="text-sm text-gray-500 mb-3 line-clamp-2">{{ recipe.description }}</p>
            <div class="flex items-center gap-3 text-xs text-gray-500">
              <span>⏱ {{ recipe.prepTimeMinutes + recipe.cookTimeMinutes }}m</span>
              <span>🔥 {{ Math.round(recipe.nutritionalInfoPerServing.calories) }} kcal</span>
              <span>👥 {{ recipe.servings }} srv</span>
            </div>
            <div v-if="recipe.tags" class="flex flex-wrap gap-1 mt-3">
              <span v-for="tag in recipe.tags.split(',').slice(0, 3)" :key="tag.trim()"
                class="text-xs bg-green-100 text-green-700 font-medium px-2 py-0.5 rounded-full">
                {{ tag.trim() }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Recipe detail panel -->
    <div v-if="selectedRecipe" class="fixed inset-0 bg-black/50 z-50 flex items-end sm:items-center justify-center p-4" @click.self="selectedRecipe = null">
      <div class="bg-white rounded-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div class="flex items-center justify-between p-5 border-b sticky top-0 bg-white z-10">
          <h2 class="text-lg font-bold text-gray-900 pr-4">{{ selectedRecipe.name }}</h2>
          <button @click="selectedRecipe = null" class="text-gray-400 hover:text-gray-600 flex-shrink-0">✕</button>
        </div>

        <!-- Hero image -->
        <div v-if="selectedRecipe.imageUrl" class="h-48 overflow-hidden">
          <img :src="selectedRecipe.imageUrl" :alt="selectedRecipe.name"
            class="w-full h-full object-cover" @error="hideImage" />
        </div>

        <div class="p-5 space-y-5">
          <!-- Time / servings stats -->
          <div class="grid grid-cols-4 gap-3 text-center">
            <div class="bg-gray-50 rounded-xl p-3">
              <p class="font-bold text-gray-900">{{ selectedRecipe.prepTimeMinutes }}m</p>
              <p class="text-xs text-gray-500">Prep</p>
            </div>
            <div class="bg-gray-50 rounded-xl p-3">
              <p class="font-bold text-gray-900">{{ selectedRecipe.cookTimeMinutes }}m</p>
              <p class="text-xs text-gray-500">Cook</p>
            </div>
            <div class="bg-gray-50 rounded-xl p-3">
              <p class="font-bold text-gray-900">{{ selectedRecipe.servings }}</p>
              <p class="text-xs text-gray-500">Servings</p>
            </div>
            <div class="bg-gray-50 rounded-xl p-3">
              <p class="font-bold text-gray-900">{{ Math.round(selectedRecipe.nutritionalInfoPerServing.calories) }}</p>
              <p class="text-xs text-gray-500">kcal/srv</p>
            </div>
          </div>

          <!-- Nutrition per serving -->
          <div>
            <h3 class="font-bold text-gray-900 mb-3">Nutrition per serving</h3>
            <div class="grid grid-cols-3 gap-2 text-center">
              <div v-for="macro in [
                { label: 'Calories', value: Math.round(selectedRecipe.nutritionalInfoPerServing.calories), unit: 'kcal', color: 'text-green-600' },
                { label: 'Protein',  value: Math.round(selectedRecipe.nutritionalInfoPerServing.protein),  unit: 'g', color: 'text-blue-600' },
                { label: 'Carbs',    value: Math.round(selectedRecipe.nutritionalInfoPerServing.carbohydrates), unit: 'g', color: 'text-orange-500' },
                { label: 'Fat',      value: Math.round(selectedRecipe.nutritionalInfoPerServing.fat),      unit: 'g', color: 'text-yellow-600' },
                { label: 'Fiber',    value: Math.round(selectedRecipe.nutritionalInfoPerServing.fiber),    unit: 'g', color: 'text-teal-600' },
                { label: 'Sugar',    value: Math.round(selectedRecipe.nutritionalInfoPerServing.sugar),    unit: 'g', color: 'text-pink-500' },
              ]" :key="macro.label" class="bg-gray-50 rounded-xl p-3">
                <p class="font-bold text-lg" :class="macro.color">{{ macro.value }}<span class="text-xs font-normal text-gray-500 ml-0.5">{{ macro.unit }}</span></p>
                <p class="text-xs text-gray-500 mt-0.5">{{ macro.label }}</p>
              </div>
            </div>
          </div>

          <!-- Ingredients -->
          <div>
            <h3 class="font-bold text-gray-900 mb-3">Ingredients</h3>
            <ul class="space-y-2">
              <li v-for="ing in selectedRecipe.ingredients" :key="ing.id" class="flex items-start gap-2 text-sm">
                <span class="text-green-500 mt-0.5">•</span>
                <span><strong>{{ ing.quantity }} {{ ing.unit }}</strong> {{ ing.foodItemName }}</span>
              </li>
            </ul>
          </div>

          <!-- Instructions -->
          <div>
            <h3 class="font-bold text-gray-900 mb-3">Instructions</h3>
            <ol class="space-y-3">
              <li v-for="(step, i) in selectedRecipe.instructions.split('\n').filter(Boolean)" :key="i"
                class="flex gap-3 text-sm">
                <span class="w-6 h-6 rounded-full bg-green-600 text-white flex-shrink-0 flex items-center justify-center text-xs font-bold">{{ i + 1 }}</span>
                <span class="text-gray-700 leading-relaxed">{{ step.replace(/^\d+\.\s*/, '') }}</span>
              </li>
            </ol>
          </div>

          <button class="w-full bg-green-600 hover:bg-green-700 text-white font-semibold py-3 rounded-xl transition-colors">
            Add to Meal Plan
          </button>
        </div>
      </div>
    </div>
  </AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import AppLayout from '@/components/layout/AppLayout.vue';
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import EmptyState from '@/components/ui/EmptyState.vue';
import { recipeService } from '@/services/recipeService';
import type { Recipe } from '@foodeez/shared';

const route = useRoute();
const recipes = ref<Recipe[]>([]);
const selectedRecipe = ref<Recipe | null>(null);
const searchQuery = ref((route.query.q as string) ?? '');
const activeTags = ref(new Set<string>());
const isLoading = ref(true);

const FILTER_TAGS = ['Vegetarian', 'Vegan', 'High-Protein', 'Low-Carb', 'Quick', 'Gluten-Free', 'Dairy-Free'];

// Tag filter applied client-side on top of server results
const filteredRecipes = computed(() => {
  const tags = Array.from(activeTags.value);
  if (tags.length === 0) return recipes.value;
  return recipes.value.filter(r =>
    tags.every(t => r.tags?.toLowerCase().includes(t.toLowerCase()))
  );
});

function hideImage(e: Event) {
  (e.target as HTMLImageElement).style.display = 'none';
}

function toggleTag(tag: string) {
  const next = new Set(activeTags.value);
  if (next.has(tag)) next.delete(tag); else next.add(tag);
  activeTags.value = next;
}

let debounceTimer: ReturnType<typeof setTimeout> | null = null;

async function fetchRecipes(query?: string) {
  isLoading.value = true;
  try {
    const q = (query ?? searchQuery.value).trim();
    recipes.value = q
      ? await recipeService.searchRecipes(q)
      : await recipeService.getRecipes();
  } finally {
    isLoading.value = false;
  }
}

// Re-fetch when route query changes (navigated from dashboard)
watch(() => route.query.q, (q) => {
  searchQuery.value = (q as string) ?? '';
  fetchRecipes(searchQuery.value);
});

// Debounced re-fetch on manual search input
watch(searchQuery, () => {
  if (debounceTimer) clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => fetchRecipes(), 400);
});

onMounted(() => fetchRecipes());
</script>
