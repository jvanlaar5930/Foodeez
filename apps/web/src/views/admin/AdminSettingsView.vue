<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { adminService } from '@/services/adminService';
import { recipeService } from '@/services/recipeService';
import AppLayout from '@/components/layout/AppLayout.vue';
import { useToastStore } from '@/stores/toast';
import { extractErrorMessage } from '@/utils/apiError';

interface SettingField {
  key: string;
  label: string;
  description: string;
  isSecret: boolean;
  placeholder: string;
  type?: 'text' | 'toggle';
  /**
   * What to show when no row has been saved yet, for a setting the API does not treat as
   * "off when absent". A toggle with no row renders as Disabled, which would misreport a
   * default-on setting - and then write that misreport the first time anything on the page
   * is saved.
   */
  defaultValue?: string;
}

/**
 * A provider and the settings that belong to it, in one place rather than two.
 *
 * These were a flat list of every key next to a separate list of providers, which put four
 * unrelated model configurations in a single column: to find the one field that mattered you
 * had to know which prefix its key happened to start with. Grouping them means the picker and
 * the sections below it are driven by the same array, and `value` is both what `ai.provider`
 * is set to and which section is the live one.
 */
interface ProviderSection {
  value: string;
  label: string;
  description: string;
  fields: SettingField[];
}

const AI_PROVIDERS: ProviderSection[] = [
  {
    value: 'claude',
    label: 'Claude (Anthropic)',
    description: 'Best quality, requires paid API key',
    fields: [
      { key: 'claude.apiKey', label: 'Claude API Key', description: 'Anthropic Claude API key (sk-ant-...)', isSecret: true, placeholder: 'sk-ant-api03-...' },
      { key: 'claude.model', label: 'Claude Model', description: 'Model ID (e.g. claude-sonnet-4-6)', isSecret: false, placeholder: 'claude-sonnet-4-6' },
      { key: 'claude.timeoutSeconds', label: 'Request Timeout (seconds)', description: 'How long one call may run before it is abandoned and the feature falls back', isSecret: false, placeholder: '120' },
    ],
  },
  {
    value: 'gemini',
    label: 'Gemini Flash (Google)',
    description: 'Free tier available, fast',
    fields: [
      { key: 'gemini.apiKey', label: 'Gemini API Key', description: 'Google AI Studio key — free tier available at aistudio.google.com', isSecret: true, placeholder: 'AIzaSy...' },
      { key: 'gemini.model', label: 'Gemini Model', description: 'Model ID. Google retires these on their own schedule, so this is the field to change when every AI feature starts 404ing.', isSecret: false, placeholder: 'gemini-3.6-flash' },
      { key: 'gemini.timeoutSeconds', label: 'Request Timeout (seconds)', description: 'How long one call may run before it is abandoned and the feature falls back', isSecret: false, placeholder: '120' },
    ],
  },
  {
    value: 'groq',
    label: 'Groq / Llama',
    description: 'Free tier, OpenAI-compatible',
    fields: [
      { key: 'groq.apiKey', label: 'Groq API Key', description: 'Free-tier key from console.groq.com', isSecret: true, placeholder: 'gsk_...' },
      { key: 'groq.model', label: 'Groq Model', description: 'Model ID', isSecret: false, placeholder: 'llama-3.1-8b-instant' },
      { key: 'groq.timeoutSeconds', label: 'Request Timeout (seconds)', description: 'How long one call may run before it is abandoned and the feature falls back', isSecret: false, placeholder: '60' },
    ],
  },
  {
    value: 'openrouter',
    label: 'OpenRouter',
    description: 'Free models available, one key for many providers',
    fields: [
      { key: 'openrouter.apiKey', label: 'OpenRouter API Key', description: 'Key from openrouter.ai/keys. Free models still need one — it identifies the account the free quota belongs to.', isSecret: true, placeholder: 'sk-or-v1-...' },
      { key: 'openrouter.useFreeModels', label: 'Use Free Models', description: 'Routes every call through openrouter/free, which picks a zero-cost model per request. Turn this off and calls are billed to your OpenRouter credit.', isSecret: false, placeholder: '', type: 'toggle', defaultValue: 'true' },
      { key: 'openrouter.model', label: 'Model', description: 'Only used when free models are off. A slug from openrouter.ai/models.', isSecret: false, placeholder: 'anthropic/claude-sonnet-4.5' },
      { key: 'openrouter.supportsVision', label: 'Model Supports Images', description: 'Only used when free models are off — the free router always accepts photos. Off means photo logging returns nothing.', isSecret: false, placeholder: '', type: 'toggle' },
      { key: 'openrouter.timeoutSeconds', label: 'Request Timeout (seconds)', description: 'Free models queue behind paid traffic, so allow more here than for a paid endpoint', isSecret: false, placeholder: '120' },
    ],
  },
  {
    value: 'ollama',
    label: 'Ollama (Local)',
    description: 'Completely free, runs on your machine',
    fields: [
      { key: 'ollama.baseUrl', label: 'Ollama Base URL', description: 'Local Ollama server (must be running on this machine)', isSecret: false, placeholder: 'http://localhost:11434' },
      { key: 'ollama.model', label: 'Ollama Model', description: 'Model name — must be pulled first with `ollama pull <name>`', isSecret: false, placeholder: 'llama3' },
      { key: 'ollama.timeoutSeconds', label: 'Request Timeout (seconds)', description: 'Runs on your own hardware, so this is generous by default — raise it if long requests get cut off', isSecret: false, placeholder: '300' },
    ],
  },
  {
    value: 'local',
    label: 'Local Server (OpenAI-compatible)',
    description: 'LM Studio, llama.cpp, vLLM, LocalAI, Jan — point it at any URL',
    fields: [
      { key: 'local.baseUrl', label: 'Local Server URL', description: 'OpenAI-compatible endpoint. LM Studio: http://localhost:1234/v1 · llama.cpp: http://localhost:8080/v1 · vLLM: http://localhost:8000/v1', isSecret: false, placeholder: 'http://localhost:1234/v1' },
      { key: 'local.model', label: 'Local Model', description: 'Model name as the server reports it (GET /v1/models). llama.cpp serves one model and ignores this.', isSecret: false, placeholder: 'local-model' },
      { key: 'local.apiKey', label: 'Local Server API Key', description: 'Optional — only if your server requires a bearer token', isSecret: true, placeholder: 'leave empty for LM Studio / llama.cpp' },
      { key: 'local.supportsVision', label: 'Local Model Supports Images', description: 'Turn on only when a vision model is loaded (Qwen2-VL, LLaVA, …). Off means photo logging returns nothing.', isSecret: false, placeholder: '', type: 'toggle' },
      { key: 'local.timeoutSeconds', label: 'Request Timeout (seconds)', description: 'Local models are slow on CPU — raise this if long requests get cut off', isSecret: false, placeholder: '300' },
      { key: 'local.maxTokens', label: 'Max Output Tokens', description: 'Optional. Left unset the server uses its own per-model default, which is safer than guessing a number too small (truncating a long answer) or too large (rejected outright).', isSecret: false, placeholder: 'leave empty unless you know the model' },
    ],
  },
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

const toastStore = useToastStore();

const values = ref<Record<string, string>>({});
const isSaving = ref(false);
const isLoading = ref(false);
const showSecrets = ref<Record<string, boolean>>({});

/** Claude is what the API falls back to when the row is unset, so the page has to agree. */
const activeProvider = computed(() => values.value['ai.provider'] ?? 'claude');

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

    // Fill in the defaults the API applies to absent rows, so the form shows what is actually
    // in effect rather than what an empty row happens to look like.
    for (const provider of AI_PROVIDERS) {
      for (const field of provider.fields) {
        if (field.defaultValue !== undefined && values.value[field.key] === undefined) {
          values.value[field.key] = field.defaultValue;
        }
      }
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

    // Which provider answers is what this page is really for, and it is chosen at the top of
    // a long form - far enough from the button that commits it to be worth repeating back.
    const provider = AI_PROVIDERS.find((p) => p.value === activeProvider.value);
    toastStore.success(
      provider ? `Settings saved. ${provider.label} is answering.` : 'Settings saved.',
    );
  } catch (err: unknown) {
    // The save button sits at the bottom of a long page and the failure was silent before
    // this: the spinner stopped and the settings looked saved.
    toastStore.error(extractErrorMessage(err, 'Those settings could not be saved.'));
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
      </div>

      <div v-if="isLoading" class="text-center text-gray-400 py-8">Loading...</div>

      <template v-else>
        <!-- Provider picker -->
        <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-5 space-y-3">
          <h2 class="font-semibold text-gray-900 dark:text-gray-100">AI Provider</h2>
          <p class="text-xs text-gray-500 dark:text-gray-400">
            Select the active provider, then configure it in its own section below.
            To bootstrap your first admin account: <code class="bg-gray-100 dark:bg-gray-800 px-1 rounded">UPDATE users SET is_admin=1 WHERE email='you@example.com';</code>
          </p>
          <div class="grid grid-cols-2 gap-2">
            <button
              v-for="p in AI_PROVIDERS"
              :key="p.value"
              :class="[
                'text-left px-3 py-2.5 rounded-xl border-2 transition-colors',
                activeProvider === p.value
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

        <!-- One section per provider, so a model's settings are read together rather than
             picked out of a single column by key prefix. The active one is marked: every
             other section is configuration for a provider that is not currently answering. -->
        <div
          v-for="p in AI_PROVIDERS"
          :key="p.value"
          :class="[
            'bg-white dark:bg-gray-900 border rounded-xl p-5 space-y-4',
            activeProvider === p.value
              ? 'border-green-500 dark:border-green-600'
              : 'border-gray-200 dark:border-gray-700',
          ]"
        >
          <div class="flex items-center justify-between gap-3">
            <div>
              <h2 class="font-semibold text-gray-900 dark:text-gray-100">{{ p.label }}</h2>
              <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ p.description }}</p>
            </div>
            <span
              v-if="activeProvider === p.value"
              class="shrink-0 rounded-full bg-green-100 px-2.5 py-1 text-xs font-medium text-green-700 dark:bg-green-900/30 dark:text-green-400"
            >
              Active
            </span>
            <button
              v-else
              type="button"
              class="shrink-0 rounded-full border border-gray-300 px-2.5 py-1 text-xs font-medium text-gray-600 transition-colors hover:border-green-400 hover:text-green-700 dark:border-gray-700 dark:text-gray-400 dark:hover:border-green-600 dark:hover:text-green-400"
              @click="values['ai.provider'] = p.value"
            >
              Use this
            </button>
          </div>

          <div v-for="field in p.fields" :key="field.key" class="space-y-1">
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
