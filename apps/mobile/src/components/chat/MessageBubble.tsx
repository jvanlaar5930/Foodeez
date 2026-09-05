import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { RecipesCard } from '@/components/chat/RecipesCard';
import { SuggestionsCard } from '@/components/chat/SuggestionsCard';
import { BorderRadius, FontSize, Shadows, Spacing } from '@/constants/theme';
import { useThemedStyles, type Palette } from '@/theme';
import { ChatRole, type ChatMessageDto } from '@/types';

interface MessageBubbleProps {
  message: ChatMessageDto;
  onAddToPlan: () => void;
  onSaveRecipes: () => void;
}

/**
 * One turn of the conversation, with whatever the assistant attached to it.
 *
 * Memoised: the thread re-renders on every streamed token of the reply being written, and
 * none of the messages above it change while that happens.
 */
export const MessageBubble = React.memo(function MessageBubble({
  message,
  onAddToPlan,
  onSaveRecipes,
}: MessageBubbleProps) {
  const styles = useThemedStyles(makeStyles);

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
          onAdd={onAddToPlan}
        />
      ) : null}

      {!isUser && message.recipes.length > 0 ? (
        <RecipesCard
          recipes={message.recipes}
          saved={Boolean(message.recipesSavedAt)}
          canSave={isSaved}
          onSave={onSaveRecipes}
        />
      ) : null}
    </View>
  );
});

/**
 * The reply being written, shown in the same bubble a finished one lands in - so the text
 * does not jump when the stream ends and the real message takes its place.
 */
export function StreamingBubble({ text }: { text: string }) {
  const styles = useThemedStyles(makeStyles);

  return (
    <View style={styles.assistantRow}>
      <View style={[styles.bubble, styles.assistantBubble]}>
        <Text style={styles.assistantText}>{text.length > 0 ? text : 'Thinking...'}</Text>
      </View>
    </View>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    userRow: { alignItems: 'flex-end', marginBottom: Spacing.md },
    assistantRow: { alignItems: 'flex-start', marginBottom: Spacing.md },
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
  });
