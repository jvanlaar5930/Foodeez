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
import { useTheme, useThemedStyles, type ColorScheme, type Palette } from '@/theme';
import { RecipePanelDark, RecipePanelLight } from '@/constants/aiPanel';

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
              styles={styles}
              onAddToPlan={() => void onAddToPlan(message.id)}
              onSaveRecipes={() => void onSaveRecipes(message.id)}
            />
          ))}

          {isSending ? (
            <View style={[styles.bubble, styles.assistantBubble]}>
              <Text style={styles.assistantText}>
                {streamingText.length > 0 ? streamingText : 'Thinking...'}
              </Text>
            </View>
          ) : null}
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

function MessageBubble({
  message,
  styles,
  onAddToPlan,
  onSaveRecipes,
}: {
  message: ChatMessageDto;
  styles: ReturnType<typeof makeStyles>;
  onAddToPlan: () => void;
  onSaveRecipes: () => void;
}) {
  const isUser = message.role === ChatRole.User;
  // A message that only exists on screen has nothing on the server to act on yet.
  const isSaved = !message.id.startsWith('pending-');

  return (
    <View style={isUser ? styles.userRow : styles.assistantRow}>
      <View style={[styles.bubble, isUser ? styles.userBubble : styles.assistantBubble]}>
        <Text style={isUser ? styles.userText : styles.assistantText}>{message.content}</Text>
      </View>

      {!isUser && message.suggestions.length > 0 ? (
        <SuggestionsCard
          suggestions={message.suggestions}
          accepted={Boolean(message.suggestionsAcceptedAt)}
          canAdd={isSaved}
          styles={styles}
          onAdd={onAddToPlan}
        />
      ) : null}

      {!isUser && message.recipes.length > 0 ? (
        <RecipesCard
          recipes={message.recipes}
          saved={Boolean(message.recipesSavedAt)}
          canSave={isSaved}
          styles={styles}
          onSave={onSaveRecipes}
        />
      ) : null}
    </View>
  );
}

function SuggestionsCard({
  suggestions,
  accepted,
  canAdd,
  styles,
  onAdd,
}: {
  suggestions: PlannedMealDto[];
  accepted: boolean;
  canAdd: boolean;
  styles: ReturnType<typeof makeStyles>;
  onAdd: () => void;
}) {
  return (
    <View style={styles.suggestions}>
      <Text style={styles.suggestionsLabel}>SUGGESTED MEALS</Text>

      {suggestions.map((meal, index) => (
        <Text key={`${meal.date}-${meal.mealType}-${index}`} style={styles.suggestionLine}>
          <Text style={styles.suggestionDay}>
            {format(parseISO(meal.date), 'EEE d MMM')} {meal.mealType}:{' '}
          </Text>
          {meal.name}
        </Text>
      ))}

      {accepted ? (
        <Text style={styles.suggestionsDone}>Added to your meal plan.</Text>
      ) : canAdd ? (
        <TouchableOpacity style={styles.suggestionsButton} onPress={onAdd}>
          <Text style={styles.suggestionsButtonText}>
            Add {suggestions.length} {suggestions.length === 1 ? 'meal' : 'meals'} to my plan
          </Text>
        </TouchableOpacity>
      ) : null}
    </View>
  );
}

/** "300 g flour", or just the name when the model gave no measurement. */
function ingredientLine(ingredient: SuggestedRecipeDto['ingredients'][number]): string {
  const amount = [ingredient.quantity > 0 ? String(ingredient.quantity) : '', ingredient.unit]
    .filter(Boolean)
    .join(' ')
    .trim();
  const line = amount.length > 0 ? `${amount} ${ingredient.name}` : ingredient.name;
  return ingredient.notes ? `${line} (${ingredient.notes})` : line;
}

function RecipesCard({
  recipes,
  saved,
  canSave,
  styles,
  onSave,
}: {
  recipes: SuggestedRecipeDto[];
  saved: boolean;
  canSave: boolean;
  styles: ReturnType<typeof makeStyles>;
  onSave: () => void;
}) {
  // Collapsed by default: a full method inline would bury the conversation around it.
  const [expanded, setExpanded] = useState<number | null>(null);

  return (
    <View style={styles.recipes}>
      <Text style={styles.recipesLabel}>{recipes.length === 1 ? 'RECIPE' : 'RECIPES'}</Text>

      {recipes.map((recipe, index) => {
        const totalTime = recipe.prepTimeMinutes + recipe.cookTimeMinutes;
        const isOpen = expanded === index;
        const steps = recipe.instructions
          .split('\n')
          .map((step) => step.trim().replace(/^\d+[.)]\s*/, ''))
          .filter(Boolean);

        return (
          <View key={`${recipe.name}-${index}`} style={styles.recipeItem}>
            <TouchableOpacity
              style={styles.recipeHead}
              onPress={() => setExpanded(isOpen ? null : index)}
              accessibilityLabel={`${isOpen ? 'Hide' : 'View'} ${recipe.name}`}
            >
              <View style={styles.flex}>
                <Text style={styles.recipeName}>{recipe.name}</Text>
                <Text style={styles.recipeMeta}>
                  {totalTime > 0 ? `${totalTime} min · ` : ''}
                  {recipe.servings} {recipe.servings === 1 ? 'serving' : 'servings'}
                  {recipe.calories > 0 ? ` · ${Math.round(recipe.calories)} kcal each` : ''}
                </Text>
              </View>
              {/* Read off the themed style so the chevron and the label it sits beside can
                  never disagree about which amber they are. */}
              <Ionicons
                name={isOpen ? 'chevron-up' : 'chevron-down'}
                size={18}
                color={styles.recipesLabel.color}
              />
            </TouchableOpacity>

            {isOpen ? (
              <View style={styles.recipeBody}>
                {recipe.description ? (
                  <Text style={styles.recipeDesc}>{recipe.description}</Text>
                ) : null}

                <Text style={styles.recipeSection}>Ingredients</Text>
                {recipe.ingredients.map((ingredient, i) => (
                  <Text key={`${ingredient.name}-${i}`} style={styles.recipeDetail}>
                    {ingredientLine(ingredient)}
                  </Text>
                ))}

                {steps.length > 0 ? (
                  <>
                    <Text style={styles.recipeSection}>Method</Text>
                    {steps.map((step, i) => (
                      <Text key={i} style={styles.recipeDetail}>
                        {i + 1}. {step}
                      </Text>
                    ))}
                  </>
                ) : null}
              </View>
            ) : null}
          </View>
        );
      })}

      {saved ? (
        <Text style={styles.recipesDone}>Saved to your recipes.</Text>
      ) : canSave ? (
        <TouchableOpacity style={styles.recipesButton} onPress={onSave}>
          <Text style={styles.recipesButtonText}>
            Save {recipes.length === 1 ? 'this recipe' : `these ${recipes.length} recipes`}
          </Text>
        </TouchableOpacity>
      ) : null}
    </View>
  );
}

const makeStyles = (C: Palette, scheme: ColorScheme) => {
  // The assistant's recipe cards carry their own amber pair, the way the AI analysis
  // panels carry a purple one - see constants/aiPanel.
  const R = scheme === 'dark' ? RecipePanelDark : RecipePanelLight;

  return StyleSheet.create({
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
    userRow: { alignItems: 'flex-end' },
    assistantRow: { alignItems: 'flex-start' },
    bubble: {
      maxWidth: '88%',
      borderRadius: BorderRadius.lg,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.sm,
    },
    userBubble: { backgroundColor: C.primary },
    assistantBubble: { backgroundColor: C.surface, ...Shadows.sm },
    userText: { color: C.onPrimary, fontSize: FontSize.md, lineHeight: 21 },
    assistantText: { color: C.text, fontSize: FontSize.md, lineHeight: 21 },
    suggestions: {
      marginTop: Spacing.sm,
      maxWidth: '88%',
      borderRadius: BorderRadius.lg,
      borderWidth: 1,
      borderColor: C.primary,
      backgroundColor: C.primaryLight,
      padding: Spacing.md,
      gap: 2,
    },
    suggestionsLabel: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.bold,
      color: C.primaryDark,
      letterSpacing: 0.5,
      marginBottom: Spacing.xs,
    },
    suggestionLine: { fontSize: FontSize.md, color: C.text, lineHeight: 20 },
    suggestionDay: { fontWeight: FontWeight.semibold, color: C.textSecondary },
    suggestionsDone: {
      marginTop: Spacing.sm,
      fontSize: FontSize.md,
      fontWeight: FontWeight.semibold,
      color: C.primaryDark,
    },
    suggestionsButton: {
      marginTop: Spacing.sm,
      backgroundColor: C.primary,
      borderRadius: BorderRadius.lg,
      paddingVertical: Spacing.sm,
      alignItems: 'center',
    },
    suggestionsButtonText: { color: C.onPrimary, fontWeight: FontWeight.semibold, fontSize: FontSize.md },
    // Amber rather than the palette green, so a recipe offer reads as a different action
    // from a plan offer when a reply carries both.
    recipes: {
      marginTop: Spacing.sm,
      maxWidth: '88%',
      borderRadius: BorderRadius.lg,
      borderWidth: 1,
      borderColor: R.panelBorder,
      backgroundColor: R.panelBg,
      padding: Spacing.md,
    },
    recipesLabel: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.bold,
      color: R.accentText,
      letterSpacing: 0.5,
      marginBottom: Spacing.xs,
    },
    recipeItem: {
      borderRadius: BorderRadius.md,
      backgroundColor: R.itemBg,
      padding: Spacing.sm,
      marginBottom: Spacing.xs,
    },
    recipeHead: { flexDirection: 'row', alignItems: 'center', gap: Spacing.sm },
    recipeName: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: R.title },
    recipeMeta: { fontSize: FontSize.xs, color: R.meta, marginTop: 2 },
    recipeBody: {
      marginTop: Spacing.sm,
      borderTopWidth: 1,
      borderTopColor: R.itemDivider,
      paddingTop: Spacing.sm,
    },
    recipeDesc: { fontSize: FontSize.xs, color: R.body, marginBottom: Spacing.xs },
    recipeSection: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.semibold,
      color: R.title,
      marginTop: Spacing.xs,
      marginBottom: 2,
    },
    recipeDetail: { fontSize: FontSize.xs, color: R.body, lineHeight: 18 },
    recipesDone: {
      marginTop: Spacing.xs,
      fontSize: FontSize.md,
      fontWeight: FontWeight.semibold,
      color: R.accentText,
    },
    recipesButton: {
      marginTop: Spacing.xs,
      backgroundColor: R.buttonBg,
      borderRadius: BorderRadius.lg,
      paddingVertical: Spacing.sm,
      alignItems: 'center',
    },
    recipesButtonText: { color: C.onPrimary, fontWeight: FontWeight.semibold, fontSize: FontSize.md },
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
};
