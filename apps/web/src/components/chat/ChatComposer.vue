<script setup lang="ts">
import { nextTick, ref, watch } from 'vue';

const props = defineProps<{ disabled?: boolean; isSending?: boolean }>();

const emit = defineEmits<{ send: [text: string]; cancel: [] }>();

const draft = ref('');
const input = ref<HTMLTextAreaElement | null>(null);

/** Grows with the question, up to the point where it would take over the screen. */
const MaxHeight = 160;

watch(draft, async () => {
  await nextTick();
  const el = input.value;
  if (!el) {
    return;
  }

  el.style.height = 'auto';
  el.style.height = `${Math.min(el.scrollHeight, MaxHeight)}px`;
});

function submit() {
  const text = draft.value.trim();
  if (text.length === 0 || props.disabled) {
    return;
  }

  emit('send', text);
  draft.value = '';
}

/** Enter sends; shift-enter is a new line, as in every other chat box. */
function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' && !event.shiftKey) {
    event.preventDefault();
    submit();
  }
}
</script>

<template>
  <div class="flex items-end gap-2">
    <textarea
      ref="input"
      v-model="draft"
      rows="1"
      :disabled="disabled"
      placeholder="Ask about your eating, or say what you want to plan..."
      class="max-h-40 w-full resize-none rounded-2xl border border-gray-300 px-4 py-2.5 text-sm leading-relaxed focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-60 dark:border-gray-700 dark:bg-gray-900 dark:text-gray-100"
      @keydown="onKeydown"
    />

    <button
      v-if="isSending"
      type="button"
      class="shrink-0 rounded-2xl border-2 border-gray-200 px-4 py-2.5 text-sm font-semibold text-gray-600 transition-colors hover:border-gray-300 dark:border-gray-700 dark:text-gray-300"
      @click="emit('cancel')"
    >
      Stop
    </button>
    <button
      v-else
      type="button"
      :disabled="disabled || draft.trim().length === 0"
      class="shrink-0 rounded-2xl bg-green-600 px-5 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-green-700 disabled:opacity-40"
      @click="submit"
    >
      Send
    </button>
  </div>
</template>
