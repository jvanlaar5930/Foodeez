import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useThemedStyles, type Palette } from '@/theme';

export interface Choice<T> {
  value: T;
  title: string;
  description: string;
  /** Shown to the left of the title, where the option has one. */
  emoji?: string;
}

interface ChoiceListProps<T> {
  options: ReadonlyArray<Choice<T>>;
  /** Null while nothing has been picked yet - which is how profile setup starts. */
  value: T | null | undefined;
  onChange: (value: T) => void;
}

/**
 * A list of mutually exclusive options, each with a name and a line explaining it.
 *
 * Profile setup rendered this markup twice, identically, over different data - once for the
 * dietary goal and once for the activity level - and the edit-profile screen has the same
 * shape again. The only real difference was that one carried an emoji.
 */
export function ChoiceList<T>({
  options,
  value,
  onChange,
}: ChoiceListProps<T>) {
  const styles = useThemedStyles(makeStyles);

  return (
    <View style={styles.list}>
      {options.map((option) => {
        const selected = value === option.value;

        return (
          <TouchableOpacity
            key={String(option.value)}
            style={[styles.card, selected && styles.cardActive]}
            onPress={() => onChange(option.value)}
            activeOpacity={0.8}
            accessibilityRole="radio"
            accessibilityState={{ selected }}
            accessibilityLabel={option.title}
          >
            {option.emoji ? <Text style={styles.emoji}>{option.emoji}</Text> : null}

            <View style={styles.text}>
              <Text style={[styles.title, selected && styles.titleActive]}>{option.title}</Text>
              <Text style={styles.description}>{option.description}</Text>
            </View>

            {selected ? (
              <View style={styles.check}>
                <Text style={styles.checkMark}>✓</Text>
              </View>
            ) : null}
          </TouchableOpacity>
        );
      })}
    </View>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    list: { gap: Spacing.sm },
    card: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: Spacing.md,
      backgroundColor: C.surface,
      borderRadius: BorderRadius.lg,
      borderWidth: 2,
      borderColor: C.divider,
      padding: Spacing.md,
    },
    cardActive: { borderColor: C.primary, backgroundColor: C.primaryLight },
    emoji: { fontSize: 28 },
    text: { flex: 1 },
    title: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
    titleActive: { color: C.primaryDark },
    description: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2 },
    check: {
      width: 24,
      height: 24,
      borderRadius: 12,
      backgroundColor: C.primary,
      alignItems: 'center',
      justifyContent: 'center',
    },
    checkMark: { color: C.onPrimary, fontSize: FontSize.sm, fontWeight: FontWeight.bold },
  });
