<template>
  <div
    v-if="visible"
    role="alert"
    class="flex items-start gap-3 rounded-xl border px-4 py-3 text-sm"
    :class="styles.container"
  >
    <span class="flex-shrink-0 text-base leading-5" aria-hidden="true">{{ styles.icon }}</span>

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
}>(), {
  dismissible: false,
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
    container: 'bg-red-50 border-red-200 text-red-700',
    title: 'text-red-800',
    body: 'text-red-700',
    dismiss: 'text-red-400 hover:text-red-600',
    icon: '⚠️',
  },
  warning: {
    container: 'bg-yellow-50 border-yellow-200 text-yellow-700',
    title: 'text-yellow-800',
    body: 'text-yellow-700',
    dismiss: 'text-yellow-400 hover:text-yellow-600',
    icon: '⚠️',
  },
  info: {
    container: 'bg-blue-50 border-blue-200 text-blue-700',
    title: 'text-blue-800',
    body: 'text-blue-700',
    dismiss: 'text-blue-400 hover:text-blue-600',
    icon: 'ℹ️',
  },
  success: {
    container: 'bg-green-50 border-green-200 text-green-700',
    title: 'text-green-800',
    body: 'text-green-700',
    dismiss: 'text-green-400 hover:text-green-600',
    icon: '✅',
  },
  tip: {
    container: 'bg-green-50 border-green-200 text-green-800',
    title: 'text-green-900',
    body: 'text-green-700',
    dismiss: 'text-green-400 hover:text-green-600',
    icon: '💡',
  },
};

const styles = computed(() => VARIANT_STYLES[props.variant]);
</script>
