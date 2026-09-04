import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { format, parseISO } from 'date-fns';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useThemedStyles, type Palette } from '@/theme';
import type { PlannedMealDto } from '@/types';

interface SuggestionsCardProps {
  suggestions: PlannedMealDto[];
  /** Already added, so the button becomes a confirmation. */
  accepted: boolean;
  /** False while the message exists only on screen - there is nothing yet to act on. */
  canAdd: boolean;
  onAdd: () => void;
}

/** Meals the assistant proposed in a reply, and the one tap that puts them on the calendar. */
export function SuggestionsCard({ suggestions, accepted, canAdd, onAdd }: SuggestionsCardProps) {
  const styles = useThemedStyles(makeStyles);

  return (
    <View style={styles.card}>
      <Text style={styles.label}>SUGGESTED MEALS</Text>

      {suggestions.map((meal, index) => (
        <Text key={`${meal.date}-${meal.mealType}-${index}`} style={styles.line}>
          <Text style={styles.day}>
            {format(parseISO(meal.date), 'EEE d MMM')} {meal.mealType}:{' '}
          </Text>
          {meal.name}
        </Text>
      ))}

      {accepted ? (
        <Text style={styles.done}>Added to your meal plan.</Text>
      ) : canAdd ? (
        <TouchableOpacity style={styles.button} onPress={onAdd} accessibilityRole="button">
          <Text style={styles.buttonText}>
            Add {suggestions.length} {suggestions.length === 1 ? 'meal' : 'meals'} to my plan
          </Text>
        </TouchableOpacity>
      ) : null}
    </View>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    card: {
      marginTop: Spacing.sm,
      maxWidth: '88%',
      borderRadius: BorderRadius.lg,
      borderWidth: 1,
      borderColor: C.primary,
      backgroundColor: C.primaryLight,
      padding: Spacing.md,
      gap: 2,
    },
    label: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.bold,
      color: C.primaryDark,
      letterSpacing: 0.5,
      marginBottom: Spacing.xs,
    },
    line: { fontSize: FontSize.md, color: C.text, lineHeight: 20 },
    day: { fontWeight: FontWeight.semibold, color: C.textSecondary },
    done: {
      marginTop: Spacing.sm,
      fontSize: FontSize.md,
      fontWeight: FontWeight.semibold,
      color: C.primaryDark,
    },
    button: {
      marginTop: Spacing.sm,
      backgroundColor: C.primary,
      borderRadius: BorderRadius.lg,
      paddingVertical: Spacing.sm,
      alignItems: 'center',
    },
    buttonText: { color: C.onPrimary, fontWeight: FontWeight.semibold, fontSize: FontSize.md },
  });
