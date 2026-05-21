<script setup lang="ts">
import type { NutritionalInfo } from '@foodeez/shared';

interface Props {
  nutrition: NutritionalInfo;
  servingSize?: string;
}

defineProps<Props>();
</script>

<template>
  <div class="border-2 border-gray-900 p-3 max-w-xs font-sans text-gray-900">
    <h2 class="text-2xl font-black leading-tight">Nutrition Facts</h2>
    <div v-if="servingSize" class="text-sm border-b border-gray-900 pb-1 mb-1">
      Serving size <span class="font-bold">{{ servingSize }}</span>
    </div>

    <div class="border-t-8 border-gray-900 pt-1">
      <div class="flex justify-between items-baseline">
        <span class="text-sm font-bold">Calories</span>
        <span class="text-4xl font-black">{{ Math.round(nutrition.calories) }}</span>
      </div>
    </div>

    <div class="border-t-4 border-gray-900 pt-1 text-xs text-right text-gray-600 mb-1">
      % Daily Value*
    </div>

    <div class="space-y-0.5 text-sm">
      <div class="flex justify-between border-b border-gray-300 py-0.5">
        <span><strong>Total Fat</strong> {{ nutrition.fat.toFixed(1) }}g</span>
        <span class="font-bold">{{ Math.round((nutrition.fat / 78) * 100) }}%</span>
      </div>
      <div class="flex justify-between border-b border-gray-300 py-0.5 pl-4">
        <span>Saturated Fat 0g</span>
        <span class="font-bold">0%</span>
      </div>
      <div class="flex justify-between border-b border-gray-300 py-0.5 pl-4">
        <span><em>Trans</em> Fat 0g</span>
      </div>
      <div class="flex justify-between border-b border-gray-300 py-0.5">
        <span><strong>Total Carbohydrate</strong> {{ nutrition.carbohydrates.toFixed(1) }}g</span>
        <span class="font-bold">{{ Math.round((nutrition.carbohydrates / 275) * 100) }}%</span>
      </div>
      <div class="flex justify-between border-b border-gray-300 py-0.5 pl-4">
        <span>Dietary Fiber {{ nutrition.fiber.toFixed(1) }}g</span>
        <span class="font-bold">{{ Math.round((nutrition.fiber / 28) * 100) }}%</span>
      </div>
      <div class="flex justify-between border-b border-gray-300 py-0.5 pl-4">
        <span>Total Sugars {{ nutrition.sugar.toFixed(1) }}g</span>
      </div>
      <div class="flex justify-between border-b border-gray-300 py-0.5">
        <span><strong>Protein</strong> {{ nutrition.protein.toFixed(1) }}g</span>
        <span class="font-bold">{{ Math.round((nutrition.protein / 50) * 100) }}%</span>
      </div>
      <div class="flex justify-between border-b border-gray-300 py-0.5">
        <span>Sodium {{ nutrition.sodium.toFixed(0) }}mg</span>
        <span class="font-bold">{{ Math.round((nutrition.sodium / 2300) * 100) }}%</span>
      </div>
    </div>

    <div class="border-t-4 border-gray-900 mt-2 text-xs text-gray-600">
      *The % Daily Value tells you how much a nutrient in a serving contributes to a daily diet.
      2,000 calories a day is used for general nutrition advice.
    </div>
  </div>
</template>
