import React, { useMemo, useState } from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { RecipePanelDark, RecipePanelLight, type RecipePanelColors } from '@/constants/aiPanel';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useThemeMode } from '@/theme';
import type { SuggestedRecipeDto } from '@/types';

interface RecipesCardProps {
  recipes: SuggestedRecipeDto[];
  /** Already saved, so the button becomes a confirmation. */
  saved: boolean;
  /** False while the message exists only on screen - there is nothing yet to act on. */
  canSave: boolean;
  onSave: () => void;
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

/**
 * Recipes the assistant wrote into a reply, collapsed until asked for.
 *
 * Carries its own light/dark pair rather than the main palette, the way the AI analysis
 * panels do - see constants/aiPanel.
 */
export function RecipesCard({ recipes, saved, canSave, onSave }: RecipesCardProps) {
  const { scheme } = useThemeMode();
  const R = scheme === 'dark' ? RecipePanelDark : RecipePanelLight;
  const styles = useMemo(() => makeStyles(R), [R]);

  // Collapsed by default: a full method inline would bury the conversation around it.
  const [expanded, setExpanded] = useState<number | null>(null);

  return (
    <View style={styles.card}>
      <Text style={styles.label}>{recipes.length === 1 ? 'RECIPE' : 'RECIPES'}</Text>

      {recipes.map((recipe, index) => {
        const totalTime = recipe.prepTimeMinutes + recipe.cookTimeMinutes;
        const isOpen = expanded === index;
        const steps = recipe.instructions
          .split('\n')
          .map((step) => step.trim().replace(/^\d+[.)]\s*/, ''))
          .filter(Boolean);

        return (
          <View key={`${recipe.name}-${index}`} style={styles.item}>
            <TouchableOpacity
              style={styles.head}
              onPress={() => setExpanded(isOpen ? null : index)}
              accessibilityLabel={`${isOpen ? 'Hide' : 'View'} ${recipe.name}`}
            >
              <View style={styles.flex}>
                <Text style={styles.name}>{recipe.name}</Text>
                <Text style={styles.meta}>
                  {totalTime > 0 ? `${totalTime} min · ` : ''}
                  {recipe.servings} {recipe.servings === 1 ? 'serving' : 'servings'}
                  {recipe.calories > 0 ? ` · ${Math.round(recipe.calories)} kcal each` : ''}
                </Text>
              </View>
              <Ionicons
                name={isOpen ? 'chevron-up' : 'chevron-down'}
                size={18}
                color={R.accentText}
              />
            </TouchableOpacity>

            {isOpen ? (
              <View style={styles.body}>
                {recipe.description ? (
                  <Text style={styles.description}>{recipe.description}</Text>
                ) : null}

                <Text style={styles.section}>Ingredients</Text>
                {recipe.ingredients.map((ingredient, i) => (
                  <Text key={`${ingredient.name}-${i}`} style={styles.detail}>
                    {ingredientLine(ingredient)}
                  </Text>
                ))}

                {steps.length > 0 ? (
                  <>
                    <Text style={styles.section}>Method</Text>
                    {steps.map((step, i) => (
                      <Text key={i} style={styles.detail}>
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
        <Text style={styles.done}>Saved to your recipes.</Text>
      ) : canSave ? (
        <TouchableOpacity style={styles.button} onPress={onSave} accessibilityRole="button">
          <Text style={styles.buttonText}>
            Save {recipes.length === 1 ? 'this recipe' : `these ${recipes.length} recipes`}
          </Text>
        </TouchableOpacity>
      ) : null}
    </View>
  );
}

const makeStyles = (R: RecipePanelColors) =>
  StyleSheet.create({
    flex: { flex: 1 },
    card: {
      marginTop: Spacing.sm,
      maxWidth: '88%',
      borderRadius: BorderRadius.lg,
      borderWidth: 1,
      borderColor: R.panelBorder,
      backgroundColor: R.panelBg,
      padding: Spacing.md,
    },
    label: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.bold,
      color: R.accentText,
      letterSpacing: 0.5,
      marginBottom: Spacing.xs,
    },
    item: {
      borderRadius: BorderRadius.md,
      backgroundColor: R.itemBg,
      padding: Spacing.sm,
      marginBottom: Spacing.xs,
    },
    head: { flexDirection: 'row', alignItems: 'center', gap: Spacing.sm },
    name: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: R.title },
    meta: { fontSize: FontSize.xs, color: R.meta, marginTop: 2 },
    body: {
      marginTop: Spacing.sm,
      borderTopWidth: 1,
      borderTopColor: R.itemDivider,
      paddingTop: Spacing.sm,
    },
    description: { fontSize: FontSize.xs, color: R.body, marginBottom: Spacing.xs },
    section: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.semibold,
      color: R.title,
      marginTop: Spacing.xs,
      marginBottom: 2,
    },
    detail: { fontSize: FontSize.xs, color: R.body, lineHeight: 18 },
    done: {
      marginTop: Spacing.xs,
      fontSize: FontSize.md,
      fontWeight: FontWeight.semibold,
      color: R.accentText,
    },
    button: {
      marginTop: Spacing.xs,
      backgroundColor: R.buttonBg,
      borderRadius: BorderRadius.lg,
      paddingVertical: Spacing.sm,
      alignItems: 'center',
    },
    buttonText: { color: '#FFFFFF', fontWeight: FontWeight.semibold, fontSize: FontSize.md },
  });
