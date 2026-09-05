import React from 'react';
import { Image, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { AiRecipeThumb } from '@/components/recipe/AiRecipeThumb';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import { isAiRecipeImage, type RecipeDto } from '@/types';

interface LinkedRecipeCardProps {
  recipe: RecipeDto;
  onOpen: () => void;
}

/**
 * The recipe behind a planned slot: what the assistant actually planned, and where someone
 * goes looking for how to cook it.
 *
 * A meal the assistant planned carries a whole recipe - method, ingredients, timings - and
 * the point of opening the slot is usually to read it, so it is shown rather than left as a
 * bare name in a text box.
 */
export function LinkedRecipeCard({ recipe, onOpen }: LinkedRecipeCardProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  const totalMinutes = recipe.prepTimeMinutes + recipe.cookTimeMinutes;

  return (
    <View style={styles.card}>
      <View style={styles.row}>
        <View style={styles.thumb}>
          {isAiRecipeImage(recipe.imageUrl) ? (
            <AiRecipeThumb size={20} />
          ) : recipe.imageUrl ? (
            <Image source={{ uri: recipe.imageUrl }} style={styles.thumbImage} resizeMode="cover" />
          ) : (
            <Ionicons name="restaurant" size={20} color={C.primary} />
          )}
        </View>

        <View style={styles.info}>
          <Text style={styles.name} numberOfLines={1}>
            {recipe.name}
          </Text>
          {recipe.description ? (
            <Text style={styles.description} numberOfLines={2}>
              {recipe.description}
            </Text>
          ) : null}
          <Text style={styles.meta}>
            {totalMinutes > 0 ? `${totalMinutes} min · ` : ''}
            {Math.round(recipe.nutritionalInfoPerServing.calories)} kcal · serves {recipe.servings}
          </Text>
        </View>
      </View>

      <TouchableOpacity style={styles.link} onPress={onOpen} accessibilityRole="button">
        <Ionicons name="book-outline" size={16} color={C.primary} />
        <Text style={styles.linkText}>View full recipe</Text>
      </TouchableOpacity>
    </View>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    card: {
      marginTop: Spacing.md,
      borderWidth: 1,
      borderColor: C.divider,
      borderRadius: BorderRadius.md,
      overflow: 'hidden',
    },
    row: { flexDirection: 'row', gap: Spacing.sm, padding: Spacing.sm },
    thumb: {
      width: 56,
      height: 56,
      borderRadius: BorderRadius.sm,
      overflow: 'hidden',
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: C.background,
    },
    thumbImage: { width: '100%', height: '100%' },
    info: { flex: 1 },
    name: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
    description: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 2 },
    meta: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 4 },
    link: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      gap: Spacing.xs,
      paddingVertical: Spacing.sm,
      borderTopWidth: 1,
      borderTopColor: C.divider,
    },
    linkText: { color: C.primary, fontSize: FontSize.sm, fontWeight: FontWeight.semibold },
  });
