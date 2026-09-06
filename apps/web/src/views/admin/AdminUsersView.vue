<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { adminService, type AdminUser } from '@/services/adminService';
import { useAuthStore } from '@/stores/auth';
import { useToastStore } from '@/stores/toast';
import { extractErrorMessage } from '@/utils/apiError';
import AppLayout from '@/components/layout/AppLayout.vue';

const authStore = useAuthStore();
const toastStore = useToastStore();
const users = ref<AdminUser[]>([]);
const isLoading = ref(false);
const togglingId = ref<string | null>(null);

async function fetchUsers() {
  isLoading.value = true;
  try {
    users.value = await adminService.getUsers();
  } finally {
    isLoading.value = false;
  }
}

// A toggled row changes one word in a table of them, and a refused change used to look
// exactly like a successful one - so both outcomes say so.
async function toggleAdmin(user: AdminUser) {
  if (user.id === authStore.user?.id) return;
  togglingId.value = user.id;
  try {
    const result = await adminService.toggleAdmin(user.id);
    user.isAdmin = result.isAdmin;
    toastStore.success(
      `${user.email} is ${result.isAdmin ? 'now an administrator' : 'no longer an administrator'}.`,
    );
  } catch (err: unknown) {
    toastStore.error(extractErrorMessage(err, 'That change could not be saved.'));
  } finally {
    togglingId.value = null;
  }
}

async function toggleActive(user: AdminUser) {
  if (user.id === authStore.user?.id) return;
  togglingId.value = user.id;
  try {
    const result = await adminService.toggleActive(user.id);
    user.isActive = result.isActive;
    toastStore.success(`${user.email} is ${result.isActive ? 'active again' : 'now deactivated'}.`);
  } catch (err: unknown) {
    toastStore.error(extractErrorMessage(err, 'That change could not be saved.'));
  } finally {
    togglingId.value = null;
  }
}

onMounted(fetchUsers);
</script>

<template>
  <AppLayout>
    <div class="p-6 space-y-4">
      <!-- Admin sub-nav -->
      <div class="flex items-center gap-1 mb-2">
        <RouterLink to="/admin/logs" class="px-3 py-1.5 text-sm rounded-lg font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800">Logs</RouterLink>
        <RouterLink to="/admin/users" class="px-3 py-1.5 text-sm rounded-lg font-medium bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400">Users</RouterLink>
        <RouterLink to="/admin/settings" class="px-3 py-1.5 text-sm rounded-lg font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800">Settings</RouterLink>
      </div>

      <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">User Management</h1>

      <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-400">Loading...</div>
        <div v-else-if="users.length === 0" class="p-8 text-center text-gray-400">No users found.</div>
        <table v-else class="w-full text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">User</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Status</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Profile</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
            <tr
              v-for="user in users"
              :key="user.id"
              class="hover:bg-gray-50 dark:hover:bg-gray-800/50"
              :class="{ 'opacity-60': !user.isActive }"
            >
              <td class="px-4 py-3">
                <div class="font-medium text-gray-900 dark:text-gray-100">{{ user.firstName }} {{ user.lastName }}</div>
                <div class="text-xs text-gray-500 dark:text-gray-400">{{ user.email }}</div>
              </td>
              <td class="px-4 py-3 space-x-1.5">
                <span v-if="user.isAdmin" class="inline-flex px-2 py-0.5 rounded text-xs font-medium bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400">Admin</span>
                <span :class="['inline-flex px-2 py-0.5 rounded text-xs font-medium', user.isActive ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' : 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400']">
                  {{ user.isActive ? 'Active' : 'Disabled' }}
                </span>
              </td>
              <td class="px-4 py-3 text-xs text-gray-500 dark:text-gray-400">
                {{ user.profileCompleted ? 'Complete' : 'Incomplete' }}
              </td>
              <td class="px-4 py-3">
                <div v-if="user.id !== authStore.user?.id" class="flex gap-2">
                  <button
                    :disabled="togglingId === user.id"
                    :class="['px-2.5 py-1 text-xs rounded-lg border transition-colors disabled:opacity-50', user.isAdmin ? 'border-purple-200 text-purple-700 hover:bg-purple-50 dark:border-purple-700 dark:text-purple-400 dark:hover:bg-purple-900/20' : 'border-gray-200 text-gray-600 hover:bg-gray-50 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-800']"
                    @click="toggleAdmin(user)"
                  >{{ user.isAdmin ? 'Remove Admin' : 'Make Admin' }}</button>
                  <button
                    :disabled="togglingId === user.id"
                    :class="['px-2.5 py-1 text-xs rounded-lg border transition-colors disabled:opacity-50', user.isActive ? 'border-red-200 text-red-600 hover:bg-red-50 dark:border-red-700 dark:text-red-400 dark:hover:bg-red-900/20' : 'border-green-200 text-green-600 hover:bg-green-50 dark:border-green-700 dark:text-green-400 dark:hover:bg-green-900/20']"
                    @click="toggleActive(user)"
                  >{{ user.isActive ? 'Disable' : 'Enable' }}</button>
                </div>
                <span v-else class="text-xs text-gray-400 dark:text-gray-500 italic">You</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </AppLayout>
</template>
