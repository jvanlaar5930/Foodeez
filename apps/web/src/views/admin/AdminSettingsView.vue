<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { adminService } from '@/services/adminService';
import { recipeService } from '@/services/recipeService';
import AppLayout from '@/components/layout/AppLayout.vue';

interface SettingField {
  key: string;
  label: string;
  description: string;
  isSecret: boolean;
  placeholder: string;
  type?: 'text' | 'toggle';
}

const KNOWN_SETTINGS: SettingField[] = [
  { key: 'claude.apiKey', label: 'Claude API Key', description: 'Anthropic Claude API key (sk-ant-...)', isSecret: true, placeholder: 'sk-ant-api03-...' },
  { key: 'claude.model', label: 'Claude Model', description: 'Model ID (e.g. claude-sonnet-4-6)', isSecret: false, placeholder: 'claude-sonnet-4-6' },
  { key: 'gemini.apiKey', label: 'Gemini API Key', description: 'Google AI Studio key — free tier available at aistudio.google.com', isSecret: true, placeholder: 'AIzaSy...' },
  { key: 'groq.apiKey', label: 'Groq API Key', description: 'Free-tier key from console.groq.com', isSecret: true, placeholder: 'gsk_...' },
  { key: 'groq.model', label: 'Groq Model', description: 'Model ID', isSecret: false, placeholder: 'llama-3.1-8b-instant' },
  { key: 'ollama.baseUrl', label: 'Ollama Base URL', description: 'Local Ollama server (must be running on this machine)', isSecret: false, placeholder: 'http://localhost:11434' },
  { key: 'ollama.model', label: 'Ollama Model', description: 'Model name — must be pulled first with `ollama pull <name>`', isSecret: false, placeholder: 'llama3' },
  { key: 'local.baseUrl', label: 'Local Server URL', description: 'OpenAI-compatible endpoint. LM Studio: http://localhost:1234/v1 · llama.cpp: http://localhost:8080/v1 · vLLM: http://localhost:8000/v1', isSecret: false, placeholder: 'http://localhost:1234/v1' },
  { key: 'local.model', label: 'Local Model', description: 'Model name as the server reports it (GET /v1/models). llama.cpp serves one model and ignores this.', isSecret: false, placeholder: 'local-model' },
  { key: 'local.apiKey', label: 'Local Server API Key', description: 'Optional — only if your server requires a bearer token', isSecret: true, placeholder: 'leave empty for LM Studio / llama.cpp' },
  { key: 'local.supportsVision', label: 'Local Model Supports Images', description: 'Turn on only when a vision model is loaded (Qwen2-VL, LLaVA, …). Off means photo logging returns nothing.', isSecret: false, placeholder: '', type: 'toggle' },
  { key: 'local.timeoutSeconds', label: 'Local Request Timeout (seconds)', description: 'Local models are slow on CPU — raise this if long requests get cut off', isSecret: false, placeholder: '300' },
];

const AI_PROVIDERS = [
  { value: 'claude', label: 'Claude (Anthropic)', description: 'Best quality, requires paid API key' },
  { value: 'gemini', label: 'Gemini Flash (Google)', description: 'Free tier available, fast' },
  { value: 'groq', label: 'Groq / Llama', description: 'Free tier, OpenAI-compatible' },
  { value: 'ollama', label: 'Ollama (Local)', description: 'Completely free, runs on your machine' },
  { value: 'local', label: 'Local Server (OpenAI-compatible)', description: 'LM Studio, llama.cpp, vLLM, LocalAI, Jan — point it at any URL' },
];

/**
 * The recipe filter pills, held as a list here and written to one settings row as a JSON
 * array. Both apps read it from `GET /recipes/filter-tags`, which falls back to the built-in
 * list while the row is unset - so an empty list has to be stored as `[]` rather than as an
 * empty string, which would read as "never configured" and bring the defaults back.
 */
const RECIPE_FILTER_TAGS_KEY = 'recipes.filterTags';
const filterTags = ref<string[]>([]);
const newTag = ref('');

const values = ref<Record<string, string>>({});
const isSaving = ref(false);
const isLoading = ref(false);
const savedMessage = ref('');
const showSecrets = ref<Record<string, boolean>>({});

async function fetchSettings() {
  isLoading.value = true;
  try {
    const [settings, tags] = await Promise.all([
      adminService.getSettings(),
      // The effective list, defaults included, rather than the raw row - which is absent
      // until somebody saves one.
      recipeService.getFilterTags(),
    ]);
    for (const s of settings) {
      values.value[s.key] = s.value;
    }
    filterTags.value = tags;
  } finally {
    isLoading.value = false;
  }
}

function addTag() {
  const tag = newTag.value.trim();
  newTag.value = '';
  if (!tag) return;

  // Pills are matched against recipe tags case-insensitively, so "Vegan" and "vegan" would
  // be two pills that filter identically.
  if (filterTags.value.some((existing) => existing.toLowerCase() === tag.toLowerCase())) return;

  filterTags.value = [...filterTags.value, tag];
}

function removeTag(tag: string) {
  filterTags.value = filterTags.value.filter((existing) => existing !== tag);
}

async function save() {
  isSaving.value = true;
  try {
    values.value[RECIPE_FILTER_TAGS_KEY] = JSON.stringify(filterTags.value);

    const pairs = Object.entries(values.value)
      .filter(([, v]) => v !== undefined && v !== '')
      .map(([key, value]) => ({ key, value }));
    await adminService.updateSettings(pairs);
    savedMessage.value = 'Settings saved.';
    setTimeout(() => { savedMessage.value = ''; }, 3000);
  } finally {
    isSaving.value = false;
  }
}

onMounted(fetchSettings);
</script>

<template>
  <AppLayout>
    <div class="p-6 max-w-2xl space-y-6">
      <!-- Admin sub-nav -->
      <div class="flex items-center gap-1 mb-2">
        <RouterLink to="/admin/logs" class="px-3 py-1.5 text-sm rounded-lg font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800">Logs</RouterLink>
        <RouterLink to="/admin/users" class="px-3 py-1.5 text-sm rounded-lg font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800">Users</RouterLink>
        <RouterLink to="/admin/settings" class="px-3 py-1.5 text-sm rounded-lg font-medium bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400">Settings</RouterLink>
      </div>

      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">Admin Settings</h1>
        <span v-if="savedMessage" class="text-sm text-green-600 dark:text-green-400">{{ savedMessage }}</span>
      </div>

      <div v-if="isLoading" class="text-center text-gray-400 py-8">Loading...</div>

      <template v-else>
        <!-- Provider picker -->
        <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-5 space-y-3">
          <h2 class="font-semibold text-gray-900 dark:text-gray-100">AI Provider</h2>
          <p class="text-xs text-gray-500 dark:text-gray-400">
            Select the active provider. Configure its API key below.
            To bootstrap your first admin account: <code class="bg-gray-100 dark:bg-gray-800 px-1 rounded">UPDATE users SET is_admin=1 WHERE email='you@example.com';</code>
          </p>
          <div class="grid grid-cols-2 gap-2">
            <button
              v-for="p in AI_PROVIDERS"
              :key="p.value"
              :class="[
                'text-left px-3 py-2.5 rounded-xl border-2 transition-colors',
                (values['ai.provider'] ?? 'claude') === p.value
                  ? 'border-green-500 bg-green-50 dark:bg-green-900/20'
                  : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600',
              ]"
              @click="values['ai.provider'] = p.value"
            >
              <div class="font-medium text-sm text-gray-900 dark:text-gray-100">{{ p.label }}</div>
              <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ p.description }}</div>
            </button>
          </div>
        </div>

        <!-- API key fields -->
        <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-5 space-y-4">
          <h2 class="font-semibold text-gray-900 dark:text-gray-100">Keys & Configuration</h2>
          <div v-for="field in KNOWN_SETTINGS" :key="field.key" class="space-y-1">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {{ field.label }}
              <span v-if="field.isSecret" class="ml-1 text-xs text-gray-400">(secret)</span>
            </label>
            <p class="text-xs text-gray-500 dark:text-gray-400">{{ field.description }}</p>
            <label v-if="field.type === 'toggle'" class="inline-flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                :checked="values[field.key] === 'true'"
                class="w-4 h-4 rounded border-gray-300 dark:border-gray-600 text-green-600 focus:ring-green-500"
                @change="values[field.key] = values[field.key] === 'true' ? 'false' : 'true'"
              />
              <span class="text-sm text-gray-700 dark:text-gray-300">
                {{ values[field.key] === 'true' ? 'Enabled' : 'Disabled' }}
              </span>
            </label>
            <div v-else class="relative">
              <input
                v-model="values[field.key]"
                :type="field.isSecret && !showSecrets[field.key] ? 'password' : 'text'"
                :placeholder="field.placeholder"
                class="w-full px-3 py-2 pr-10 text-sm border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 rounded-lg focus:outline-none focus:ring-2 focus:ring-green-500 placeholder:text-gray-400"
              />
              <button
                v-if="field.isSecret"
                type="button"
                class="absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                @click="showSecrets[field.key] = !showSecrets[field.key]"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path v-if="showSecrets[field.key]" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                  <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
              </button>
            </div>
          </div>
        </div>

        <!-- Recipe filter pills -->
        <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-5 space-y-3">
          <h2 class="font-semibold text-gray-900 dark:text-gray-100">Recipe Filter Pills</h2>
          <p class="text-xs text-gray-500 dark:text-gray-400">
            The quick filters above recipe results on the web and on the Recipes tab in the app.
            Each one is matched against a recipe's own tags, so a pill only finds recipes already
            tagged with that word. Saved with the button at the bottom of this page.
          </p>

          <div v-if="filterTags.length" class="flex flex-wrap gap-2">
            <span
              v-for="tag in filterTags"
              :key="tag"
              class="inline-flex items-center gap-1 rounded-full border border-gray-300 py-1 pl-3 pr-1 text-sm text-gray-700 dark:border-gray-700 dark:text-gray-300"
            >
              {{ tag }}
              <button
                type="button"
                :aria-label="`Remove ${tag}`"
                class="flex h-5 w-5 items-center justify-center rounded-full text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-800 dark:hover:text-gray-200"
                @click="removeTag(tag)"
              >
                <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </span>
          </div>
          <p v-else class="text-xs text-amber-600 dark:text-amber-400">
            No pills. Both apps will show recipe results with no filter row at all.
          </p>

          <form class="flex gap-2" @submit.prevent="addTag">
            <input
              v-model="newTag"
              placeholder="Add a filter — e.g. Keto"
              class="flex-1 rounded-lg border border-gray-200 bg-white px-3 py-2 text-sm text-gray-900 placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100"
            />
            <button
              type="submit"
              :disabled="!newTag.trim()"
              class="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 transition-colors hover:border-green-400 hover:text-green-700 disabled:opacity-50 dark:border-gray-700 dark:text-gray-200 dark:hover:border-green-600 dark:hover:text-green-400"
            >
              Add
            </button>
          </form>
        </div>

        <button
          :disabled="isSaving"
          class="w-full py-2.5 bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white font-medium rounded-xl transition-colors"
          @click="save"
        >
          {{ isSaving ? 'Saving...' : 'Save Settings' }}
        </button>
      </template>
    </div>
  </AppLayout>
</template>
