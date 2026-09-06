<script setup lang="ts">
import AppAlert from '@/components/ui/AppAlert.vue';
import { computed, nextTick, onMounted, ref, watch } from 'vue';
import AppLayout from '@/components/layout/AppLayout.vue';
import ChatBubble from '@/components/chat/ChatBubble.vue';
import ChatComposer from '@/components/chat/ChatComposer.vue';
import ConversationList from '@/components/chat/ConversationList.vue';
import StreamingText from '@/components/ai/StreamingText.vue';
import { useChatStore } from '@/stores/chat';
import { useToastStore } from '@/stores/toast';

const chatStore = useChatStore();
const toastStore = useToastStore();

const thread = ref<HTMLElement | null>(null);
const showHistory = ref(false);

const hasMessages = computed(() => chatStore.messages.length > 0);

/** The openers worth one tap, for a tab with nothing in it yet. */
const STARTERS = [
  'What should I eat tonight to hit my protein target?',
  'Plan my dinners for the next five days.',
  'Am I eating enough fibre?',
  'Give me three lunches I can make in 15 minutes.',
];

onMounted(async () => {
  await chatStore.fetchConversations();

  // Straight back into the last thread: advice is a conversation, and starting every visit
  // from a blank page would throw away the context that makes it worth having.
  const latest = chatStore.conversations[0];
  if (latest) {
    await chatStore.openConversation(latest.id);
  }
});

// Follow the conversation the way a chat window does.
watch(
  () => [chatStore.messages.length, chatStore.streamingText],
  async () => {
    await nextTick();
    if (thread.value) {
      thread.value.scrollTop = thread.value.scrollHeight;
    }
  },
);

async function send(text: string) {
  await chatStore.send(text);
}

async function openConversation(conversationId: string) {
  showHistory.value = false;
  await chatStore.openConversation(conversationId);
}

function startNew() {
  showHistory.value = false;
  chatStore.startNew();
}

// Both of these land somewhere else entirely - the calendar, the recipe collection - so the
// only sign anything happened is what is said here. A failure is left to the error banner
// below the thread, which the stores already feed.
async function addToPlan(messageId: string) {
  if (await chatStore.addSuggestionsToPlan(messageId)) {
    toastStore.success(
      'Added to your meal plan. They are on the calendar and in your grocery list.',
    );
  }
}

async function saveRecipes(messageId: string) {
  const saved = await chatStore.saveRecipes(messageId);
  if (saved > 0) {
    toastStore.success(
      `Saved ${saved === 1 ? 'the recipe' : `${saved} recipes`} to your recipe collection.`,
    );
  }
}
</script>

<template>
  <AppLayout>
    <div class="mx-auto flex h-[calc(100vh-5rem)] max-w-6xl gap-6 p-4 sm:p-6 lg:h-screen">
      <!-- Thread list: a sidebar on a wide screen, a drawer on a narrow one. -->
      <aside class="hidden w-72 shrink-0 lg:block">
        <ConversationList
          :conversations="chatStore.conversations"
          :active-id="chatStore.activeConversation?.id"
          @open="openConversation"
          @remove="chatStore.remove"
          @start-new="startNew"
        />
      </aside>

      <section class="flex min-w-0 flex-1 flex-col">
        <header class="mb-3 flex items-center justify-between gap-3">
          <div class="min-w-0">
            <h1 class="truncate text-lg font-bold text-gray-900 dark:text-gray-100">
              {{ chatStore.activeConversation?.title ?? 'Ask for advice' }}
            </h1>
            <p class="text-xs text-gray-500 dark:text-gray-400">
              Knows your profile, targets and the foods you avoid
            </p>
          </div>

          <div class="flex shrink-0 items-center gap-2">
            <button
              type="button"
              class="rounded-lg border-2 border-gray-200 px-3 py-1.5 text-xs font-semibold text-gray-600 transition-colors hover:border-gray-300 lg:hidden dark:border-gray-700 dark:text-gray-300"
              @click="showHistory = !showHistory"
            >
              History
            </button>
            <button
              type="button"
              class="rounded-lg bg-green-600 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-green-700"
              @click="startNew"
            >
              New
            </button>
          </div>
        </header>

        <!-- Narrow-screen drawer -->
        <div
          v-if="showHistory"
          class="mb-3 max-h-64 overflow-y-auto rounded-2xl bg-white p-3 shadow-sm lg:hidden dark:bg-gray-900"
        >
          <ConversationList
            :conversations="chatStore.conversations"
            :active-id="chatStore.activeConversation?.id"
            @open="openConversation"
            @remove="chatStore.remove"
            @start-new="startNew"
          />
        </div>

        <div ref="thread" class="flex-1 space-y-4 overflow-y-auto pb-4">
          <div v-if="!hasMessages" class="py-10 text-center">
            <p class="mb-1 text-4xl">🥦</p>
            <p class="text-lg font-semibold text-gray-800 dark:text-gray-100">
              What would you like to know?
            </p>
            <p class="mx-auto mt-1 max-w-md text-sm text-gray-500 dark:text-gray-400">
              Ask about what you have been eating, or ask for a few days of meals - anything
              planned here can go straight onto your calendar and into your grocery list.
            </p>

            <div class="mx-auto mt-5 flex max-w-lg flex-col gap-2">
              <button
                v-for="starter in STARTERS"
                :key="starter"
                type="button"
                class="rounded-xl border border-gray-200 px-4 py-2 text-left text-sm text-gray-600 transition-colors hover:border-green-300 hover:text-green-700 dark:border-gray-800 dark:text-gray-300 dark:hover:border-green-800 dark:hover:text-green-400"
                @click="send(starter)"
              >
                {{ starter }}
              </button>
            </div>
          </div>

          <ChatBubble
            v-for="message in chatStore.messages"
            :key="message.id"
            :message="message"
            :is-saved="!message.id.startsWith('pending-')"
            @add-to-plan="addToPlan"
            @save-recipes="saveRecipes"
          />

          <div
            v-if="chatStore.isSending"
            class="max-w-[85%] rounded-2xl bg-white px-4 py-2.5 shadow-sm sm:max-w-[75%] dark:bg-gray-900"
          >
            <StreamingText :text="chatStore.streamingText" placeholder="Thinking..." />
          </div>
        </div>

        <AppAlert v-if="chatStore.error" variant="error" :message="chatStore.error" class="mb-2" />

        <ChatComposer
          :disabled="chatStore.isSending"
          :is-sending="chatStore.isSending"
          @send="send"
          @cancel="chatStore.cancelSend"
        />
      </section>
    </div>
  </AppLayout>
</template>
