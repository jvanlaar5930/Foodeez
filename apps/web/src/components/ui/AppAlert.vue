<template>
  <div
    v-if="visible"
    :role="role"
    class="flex items-start gap-3 rounded-xl border px-4 py-3 text-sm"
    :class="styles.container"
  >
    <span class="flex-shrink-0 text-base leading-5" aria-hidden="true">{{ icon ?? styles.icon }}</span>

    <div class="flex-1 min-w-0">
      <p v-if="title" class="font-semibold mb-0.5" :class="styles.title">{{ title }}</p>
      <div :class="styles.body">
        <slot>{{ message }}</slot>
      </div>
    </div>

    <button
      v-if="dismissible"
      type="button"
      class="flex-shrink-0 opacity-60 hover:opacity-100 transition-opacity leading-none"
      :class="styles.dismiss"
      aria-label="Dismiss"
      @click="handleDismiss"
    >
      ✕
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';

type Variant = 'error' | 'warning' | 'info' | 'success' | 'tip';

const props = withDefaults(defineProps<{
  variant: Variant;
  message?: string;
  title?: string;
  dismissible?: boolean;
  /**
   * How loudly a screen reader should take it. 'alert' interrupts, which is right for a
   * failure and wrong for a confirmation that arrives while the reader is mid-sentence.
   */
  role?: string;
  /**
   * Overrides the variant's own glyph, for a confirmation whose colour should say something
   * the variant does not - undoing something is a success, and still not green.
   *
   * Decorative only: the span is aria-hidden, so the message has to carry the meaning.
   */
  icon?: string;
}>(), {
  dismissible: false,
  role: 'alert',
});

const emit = defineEmits<{ dismiss: [] }>();

const visible = ref(true);

function handleDismiss() {
  visible.value = false;
  emit('dismiss');
}

const VARIANT_STYLES: Record<Variant, {
  container: string;
  title: string;
  body: string;
  dismiss: string;
  icon: string;
}> = {
  error: {
    container: 'bg-red-50 dark:bg-red-950/40 border-red-200 dark:border-red-900/50 text-red-700 dark:text-red-300',
    title: 'text-red-800 dark:text-red-300',
    body: 'text-red-700 dark:text-red-300',
    dismiss: 'text-red-400 hover:text-red-600 dark:hover:text-red-400',
    icon: '⚠️',
  },
  warning: {
    container: 'bg-yellow-50 dark:bg-yellow-950/40 border-yellow-200 dark:border-yellow-900/50 text-yellow-700 dark:text-yellow-400',
    title: 'text-yellow-800 dark:text-yellow-300',
    body: 'text-yellow-700 dark:text-yellow-400',
    dismiss: 'text-yellow-400 hover:text-yellow-600 dark:hover:text-yellow-400',
    icon: '⚠️',
  },
  info: {
    container: 'bg-blue-50 dark:bg-blue-950/40 border-blue-200 dark:border-blue-900/50 text-blue-700 dark:text-blue-400',
    title: 'text-blue-800',
    body: 'text-blue-700 dark:text-blue-400',
    dismiss: 'text-blue-400 hover:text-blue-600 dark:hover:text-blue-400',
    icon: 'ℹ️',
  },
  success: {
    container: 'bg-green-50 dark:bg-green-950/40 border-green-200 dark:border-green-800 text-green-700 dark:text-green-400',
    title: 'text-green-800 dark:text-green-300',
    body: 'text-green-700 dark:text-green-400',
    dismiss: 'text-green-400 hover:text-green-600 dark:hover:text-green-400',
    icon: '✅',
  },
  tip: {
    container: 'bg-green-50 dark:bg-green-950/40 border-green-200 dark:border-green-800 text-green-800 dark:text-green-300',
    title: 'text-green-900',
    body: 'text-green-700 dark:text-green-400',
    dismiss: 'text-green-400 hover:text-green-600 dark:hover:text-green-400',
    icon: '💡',
  },
};

const styles = computed(() => VARIANT_STYLES[props.variant]);
</script>
