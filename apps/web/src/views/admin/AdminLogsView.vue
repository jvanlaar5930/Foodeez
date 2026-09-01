<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { adminService, type AppLogEntry } from '@/services/adminService';
import AppLayout from '@/components/layout/AppLayout.vue';

const logs = ref<AppLogEntry[]>([]);
const totalCount = ref(0);
const page = ref(1);
const pageSize = 20;
const isLoading = ref(false);
const levelFilter = ref('');
const searchQuery = ref('');
let debounceTimer: ReturnType<typeof setTimeout>;

const totalPages = computed(() => Math.ceil(totalCount.value / pageSize));

const levelColors: Record<string, string> = {
  Error: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
  Warning: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  Information: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  Debug: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
};

async function fetchLogs() {
  isLoading.value = true;
  try {
    const result = await adminService.getLogs(page.value, pageSize, levelFilter.value || undefined, searchQuery.value || undefined);
    logs.value = result.items;
    totalCount.value = result.totalCount;
  } finally {
    isLoading.value = false;
  }
}

function onSearchInput() {
  clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => { page.value = 1; fetchLogs(); }, 400);
}

watch(levelFilter, () => { page.value = 1; fetchLogs(); });
watch(page, fetchLogs);
onMounted(fetchLogs);

function formatTime(ts: string) {
  return new Date(ts).toLocaleString();
}
</script>

<template>
  <AppLayout>
    <div class="p-6 space-y-4 max-w-7xl">
      <!-- Admin sub-nav -->
      <div class="flex items-center gap-1 mb-2">
        <RouterLink to="/admin/logs" class="px-3 py-1.5 text-sm rounded-lg font-medium bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400">Logs</RouterLink>
        <RouterLink to="/admin/users" class="px-3 py-1.5 text-sm rounded-lg font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800">Users</RouterLink>
        <RouterLink to="/admin/settings" class="px-3 py-1.5 text-sm rounded-lg font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800">Settings</RouterLink>
      </div>

      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">App Logs</h1>
        <button
          class="px-3 py-1.5 text-sm bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
          @click="fetchLogs"
        >Refresh</button>
      </div>

      <div class="flex gap-3">
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search messages..."
          class="flex-1 px-3 py-2 text-sm border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 rounded-lg focus:outline-none focus:ring-2 focus:ring-green-500"
          @input="onSearchInput"
        />
        <select
          v-model="levelFilter"
          class="px-3 py-2 text-sm border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 rounded-lg focus:outline-none focus:ring-2 focus:ring-green-500"
        >
          <option value="">All levels</option>
          <option value="Error">Error</option>
          <option value="Warning">Warning</option>
          <option value="Information">Information</option>
          <option value="Debug">Debug</option>
        </select>
      </div>

      <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-400">Loading...</div>
        <div v-else-if="logs.length === 0" class="p-8 text-center text-gray-400">No logs found.</div>
        <table v-else class="w-full text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider w-36">Time</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider w-28">Level</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider w-32">Source</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Message</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
            <tr v-for="log in logs" :key="log.id" class="hover:bg-gray-50 dark:hover:bg-gray-800/50">
              <td class="px-4 py-3 text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">{{ formatTime(log.timestamp) }}</td>
              <td class="px-4 py-3">
                <span :class="['inline-flex px-2 py-0.5 rounded text-xs font-medium', levelColors[log.level] ?? levelColors.Debug]">
                  {{ log.level }}
                </span>
              </td>
              <td class="px-4 py-3 text-xs text-gray-500 dark:text-gray-400 truncate max-w-[8rem]">{{ log.source }}</td>
              <td class="px-4 py-3 text-gray-700 dark:text-gray-300">
                <div class="truncate max-w-[40rem]">{{ log.message }}</div>
                <div v-if="log.exception" class="mt-0.5 text-xs text-red-500 dark:text-red-400 truncate max-w-[40rem]">{{ log.exception }}</div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="totalPages > 1" class="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400">
        <span>{{ totalCount }} total entries</span>
        <div class="flex gap-2">
          <button
            :disabled="page === 1"
            class="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-gray-700 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
            @click="page--"
          >Previous</button>
          <span class="px-3 py-1.5">{{ page }} / {{ totalPages }}</span>
          <button
            :disabled="page === totalPages"
            class="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-gray-700 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
            @click="page++"
          >Next</button>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
