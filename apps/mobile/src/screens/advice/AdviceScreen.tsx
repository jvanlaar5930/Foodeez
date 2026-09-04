import React, { useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  KeyboardAvoidingView,
  Modal,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { format, parseISO } from 'date-fns';
import { useChatStore } from '@/store/chatStore';
import {
  ChatRole,
  type ChatMessageDto,
  type PlannedMealDto,
  type SuggestedRecipeDto,
} from '@/types';
import { BorderRadius, FontSize, FontWeight, Shadows, Spacing } from '@/constants/theme';
import { MessageBubble, StreamingBubble } from '@/components/chat/MessageBubble';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

/** The openers worth one tap, for a tab with nothing in it yet. */
const STARTERS = [
  'What should I eat tonight to hit my protein target?',
  'Plan my dinners for the next five days.',
  'Am I eating enough fibre?',
  'Give me three lunches I can make in 15 minutes.',
];

export function AdviceScreen() {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const scroller = useRef<ScrollView>(null);

  const [draft, setDraft] = useState('');
  const [showHistory, setShowHistory] = useState(false);
  const [planNotice, setPlanNotice] = useState<string | null>(null);

  const conversations = useChatStore((state) => state.conversations);
  const activeConversation = useChatStore((state) => state.activeConversation);
  const isLoading = useChatStore((state) => state.isLoading);
  const isSending = useChatStore((state) => state.isSending);
  const streamingText = useChatStore((state) => state.streamingText);
  const error = useChatStore((state) => state.error);
  const fetchConversations = useChatStore((state) => state.fetchConversations);
  const openConversation = useChatStore((state) => state.openConversation);
  const startNew = useChatStore((state) => state.startNew);
  const send = useChatStore((state) => state.send);
  const cancelSend = useChatStore((state) => state.cancelSend);
  const remove = useChatStore((state) => state.remove);
  const addSuggestionsToPlan = useChatStore((state) => state.addSuggestionsToPlan);
  const saveRecipes = useChatStore((state) => state.saveRecipes);

  // Straight back into the last thread: advice is a conversation, and starting every visit
  // from a blank page would throw away the context that makes it worth having.
  useEffect(() => {
    void (async () => {
      await fetchConversations();
      const latest = useChatStore.getState().conversations[0];
      if (latest && !useChatStore.getState().activeConversation) {
        await openConversation(latest.id);
      }
    })();
  }, []);

  const messages = activeConversation?.messages ?? [];

  const onSend = async (text: string) => {
    setPlanNotice(null);
    setDraft('');
    await send(text);
  };

  const onAddToPlan = async (messageId: string) => {
    const added = await addSuggestionsToPlan(messageId);
    setPlanNotice(
      added ? 'Added to your meal plan. They are on the calendar and in your grocery list.' : null,
    );
  };

  const onSaveRecipes = async (messageId: string) => {
    const saved = await saveRecipes(messageId);
    setPlanNotice(
      saved > 0
        ? `Saved ${saved === 1 ? 'the recipe' : `${saved} recipes`} to your recipe collection.`
        : null,
    );
  };

  const confirmDelete = (conversationId: string, title: string) => {
    Alert.alert('Delete conversation', `Delete "${title}"? This cannot be undone.`, [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Delete', style: 'destructive', onPress: () => void remove(conversationId) },
    ]);
  };

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <View style={styles.headerText}>
          <Text style={styles.title} numberOfLines={1}>
            {activeConversation?.title ?? 'Ask for advice'}
          </Text>
          <Text style={styles.subtitle}>Knows your profile, targets and the foods you avoid</Text>
        </View>

        <TouchableOpacity
          style={styles.headerButton}
          onPress={() => setShowHistory(true)}
          accessibilityLabel="Conversation history"
        >
          <Ionicons name="time-outline" size={20} color={C.textSecondary} />
        </TouchableOpacity>
        <TouchableOpacity
          style={styles.headerButton}
          onPress={() => {
            setPlanNotice(null);
            startNew();
          }}
          accessibilityLabel="New conversation"
        >
          <Ionicons name="create-outline" size={20} color={C.primary} />
        </TouchableOpacity>
      </View>

      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        keyboardVerticalOffset={Platform.OS === 'ios' ? 90 : 0}
      >
        <ScrollView
          ref={scroller}
          style={styles.flex}
          contentContainerStyle={styles.thread}
          // Not animated while a reply is streaming: this fires on every token, and each
          // animated scroll queues behind the last, so the thread ends up chasing itself
          // instead of following the text. A finished message still slides into view.
          onContentSizeChange={() => scroller.current?.scrollToEnd({ animated: !isSending })}
        >
          {isLoading && messages.length === 0 ? (
            <ActivityIndicator color={C.primary} style={styles.loader} />
          ) : null}

          {messages.length === 0 && !isLoading ? (
            <View style={styles.empty}>
              <Text style={styles.emptyEmoji}>🥦</Text>
              <Text style={styles.emptyTitle}>What would you like to know?</Text>
              <Text style={styles.emptyText}>
                Ask about what you have been eating, or ask for a few days of meals - anything
                planned here can go straight onto your calendar and into your grocery list.
              </Text>

              {STARTERS.map((starter) => (
                <TouchableOpacity
                  key={starter}
                  style={styles.starter}
                  onPress={() => void onSend(starter)}
                >
                  <Text style={styles.starterText}>{starter}</Text>
                </TouchableOpacity>
              ))}
            </View>
          ) : null}

          {messages.map((message) => (
            <MessageBubble
              key={message.id}
              message={message}
              onAddToPlan={() => void onAddToPlan(message.id)}
              onSaveRecipes={() => void onSaveRecipes(message.id)}
            />
          ))}

          {isSending ? <StreamingBubble text={streamingText} /> : null}
        </ScrollView>

        {planNotice ? <Text style={styles.notice}>{planNotice}</Text> : null}
        {error ? <Text style={styles.error}>{error}</Text> : null}

        <View style={styles.composer}>
          <TextInput
            style={styles.input}
            value={draft}
            onChangeText={setDraft}
            placeholder="Ask about your eating, or say what you want to plan..."
            placeholderTextColor={C.textHint}
            multiline
            editable={!isSending}
          />

          {isSending ? (
            <TouchableOpacity style={styles.stopButton} onPress={cancelSend}>
              <Text style={styles.stopText}>Stop</Text>
            </TouchableOpacity>
          ) : (
            <TouchableOpacity
              style={[styles.sendButton, draft.trim().length === 0 && styles.sendDisabled]}
              disabled={draft.trim().length === 0}
              onPress={() => void onSend(draft)}
            >
              <Ionicons name="arrow-up" size={20} color={C.onPrimary} />
            </TouchableOpacity>
          )}
        </View>
      </KeyboardAvoidingView>

      <Modal visible={showHistory} animationType="slide" transparent onRequestClose={() => setShowHistory(false)}>
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Conversations</Text>
              <TouchableOpacity onPress={() => setShowHistory(false)}>
                <Ionicons name="close" size={22} color={C.textSecondary} />
              </TouchableOpacity>
            </View>

            <TouchableOpacity
              style={styles.newButton}
              onPress={() => {
                setShowHistory(false);
                setPlanNotice(null);
                startNew();
              }}
            >
              <Text style={styles.newButtonText}>+ New conversation</Text>
            </TouchableOpacity>

            <ScrollView style={styles.modalList}>
              {conversations.length === 0 ? (
                <Text style={styles.emptyText}>Nothing yet. Ask a question to start one.</Text>
              ) : null}

              {conversations.map((conversation) => (
                <View key={conversation.id} style={styles.historyRow}>
                  <TouchableOpacity
                    style={styles.flex}
                    onPress={() => {
                      setShowHistory(false);
                      setPlanNotice(null);
                      void openConversation(conversation.id);
                    }}
                  >
                    <Text style={styles.historyTitle} numberOfLines={1}>
                      {conversation.title}
                    </Text>
                    {conversation.preview ? (
                      <Text style={styles.historyPreview} numberOfLines={1}>
                        {conversation.preview}
                      </Text>
                    ) : null}
                  </TouchableOpacity>

                  <TouchableOpacity
                    onPress={() => confirmDelete(conversation.id, conversation.title)}
                    accessibilityLabel={`Delete ${conversation.title}`}
                  >
                    <Ionicons name="trash-outline" size={18} color={C.textHint} />
                  </TouchableOpacity>
                </View>
              ))}
            </ScrollView>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    container: { flex: 1, backgroundColor: C.background },
    flex: { flex: 1 },
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: Spacing.sm,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.sm,
      backgroundColor: C.surface,
      borderBottomWidth: 1,
      borderBottomColor: C.divider,
    },
    headerText: { flex: 1 },
    title: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text },
    subtitle: { fontSize: FontSize.sm, color: C.textSecondary },
    headerButton: { padding: Spacing.sm },
    thread: { padding: Spacing.md, gap: Spacing.md, paddingBottom: Spacing.xl },
    loader: { marginTop: Spacing.xl },
    empty: { alignItems: 'center', gap: Spacing.sm, paddingVertical: Spacing.xl },
    emptyEmoji: { fontSize: 40 },
    emptyTitle: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: C.text },
    emptyText: {
      fontSize: FontSize.md,
      color: C.textSecondary,
      textAlign: 'center',
      lineHeight: 20,
      marginBottom: Spacing.sm,
    },
    starter: {
      width: '100%',
      borderWidth: 1,
      borderColor: C.divider,
      borderRadius: BorderRadius.lg,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.md,
    },
    starterText: { fontSize: FontSize.md, color: C.textSecondary },
    // Amber rather than the palette green, so a recipe offer reads as a different action
    // from a plan offer when a reply carries both.
    notice: {
      marginHorizontal: Spacing.md,
      marginBottom: Spacing.sm,
      color: C.primaryDark,
      fontSize: FontSize.sm,
    },
    error: {
      marginHorizontal: Spacing.md,
      marginBottom: Spacing.sm,
      color: C.error,
      fontSize: FontSize.sm,
    },
    composer: {
      flexDirection: 'row',
      alignItems: 'flex-end',
      gap: Spacing.sm,
      padding: Spacing.md,
      backgroundColor: C.surface,
      borderTopWidth: 1,
      borderTopColor: C.divider,
    },
    input: {
      flex: 1,
      maxHeight: 120,
      borderWidth: 1,
      borderColor: C.divider,
      borderRadius: BorderRadius.lg,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.sm,
      fontSize: FontSize.md,
      color: C.text,
      backgroundColor: C.background,
    },
    sendButton: {
      width: 44,
      height: 44,
      borderRadius: BorderRadius.full,
      backgroundColor: C.primary,
      alignItems: 'center',
      justifyContent: 'center',
    },
    sendDisabled: { opacity: 0.4 },
    stopButton: {
      paddingHorizontal: Spacing.md,
      height: 44,
      borderRadius: BorderRadius.lg,
      borderWidth: 2,
      borderColor: C.divider,
      alignItems: 'center',
      justifyContent: 'center',
    },
    stopText: { color: C.textSecondary, fontWeight: FontWeight.semibold },
    modalOverlay: { flex: 1, backgroundColor: C.overlay, justifyContent: 'flex-end' },
    modalContent: {
      backgroundColor: C.surface,
      borderTopLeftRadius: BorderRadius.xl,
      borderTopRightRadius: BorderRadius.xl,
      padding: Spacing.lg,
      maxHeight: '75%',
      gap: Spacing.md,
    },
    modalHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
    modalTitle: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: C.text },
    modalList: { maxHeight: 400 },
    newButton: {
      borderWidth: 2,
      borderStyle: 'dashed',
      borderColor: C.primary,
      borderRadius: BorderRadius.lg,
      paddingVertical: Spacing.md,
      alignItems: 'center',
    },
    newButtonText: { color: C.primary, fontWeight: FontWeight.semibold },
    historyRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: Spacing.md,
      paddingVertical: Spacing.md,
      borderBottomWidth: 1,
      borderBottomColor: C.divider,
    },
    historyTitle: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
    historyPreview: { fontSize: FontSize.sm, color: C.textHint, marginTop: 2 },
  });
