import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  Image,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { RecipesStackParamList } from '@/navigation/types';
import { recipeService } from '@/services/recipeService';
import { useSavedRecipeStore } from '@/store/savedRecipeStore';
import { RecipeDto } from '@/types';
import { Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

type Props = NativeStackScreenProps<RecipesStackParamList, 'RecipeDetail'>;

export function RecipeDetailScreen({ route, navigation }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const { recipeId } = route.params;
  const [recipe, setRecipe] = useState<RecipeDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRetrying, setIsRetrying] = useState(false);

  const savedIds = useSavedRecipeStore((s) => s.savedIds);
  const savedPending = useSavedRecipeStore((s) => s.pending);
  const savedHasLoaded = useSavedRecipeStore((s) => s.hasLoaded);
  const fetchSaved = useSavedRecipeStore((s) => s.fetch);
  const toggleSaved = useSavedRecipeStore((s) => s.toggle);

  useEffect(() => {
    recipeService.getRecipeById(recipeId)
      .then(setRecipe)
      .finally(() => setIsLoading(false));
  }, [recipeId]);

  /** The server retries upstream on every read, so a second attempt can genuinely succeed. */
  const retryDetail = async () => {
    setIsRetrying(true);
    try {
      setRecipe(await recipeService.getRecipeById(recipeId));
    } catch {
      // Keep what is on screen; the notice still offers another try.
    } finally {
      setIsRetrying(false);
    }
  };

  // Reached directly from a notification or deep link, this screen may be the first to
  // need the saved list - so make sure it exists before drawing the bookmark.
  useEffect(() => {
    if (!savedHasLoaded) fetchSaved();
  }, [savedHasLoaded, fetchSaved]);

  if (isLoading) {
    return (
      <SafeAreaView style={[styles.container, styles.centered]}>
        <ActivityIndicator size="large" color={C.primary} />
      </SafeAreaView>
    );
  }

  if (!recipe) {
    return (
      <SafeAreaView style={[styles.container, styles.centered]}>
        <Text style={styles.errorText}>Recipe not found</Text>
        <TouchableOpacity onPress={() => navigation.goBack()}>
          <Text style={styles.backLink}>Go Back</Text>
        </TouchableOpacity>
      </SafeAreaView>
    );
  }

  const isSaved = savedIds.has(recipe.id);
  const totalTime = recipe.prepTimeMinutes + recipe.cookTimeMinutes;
  const tags = recipe.tags?.split(',').map(t => t.trim()).filter(Boolean) ?? [];
  const steps = recipe.instructions.split('\n').filter(s => s.trim());

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <ScrollView showsVerticalScrollIndicator={false}>
        {/* Hero */}
        <View style={styles.hero}>
          <TouchableOpacity style={styles.backButton} onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={24} color={C.surface} />
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.saveButton}
            disabled={savedPending.has(recipe.id)}
            onPress={() => toggleSaved(recipe)}
            accessibilityLabel={isSaved ? 'Remove from saved recipes' : 'Save this recipe'}
          >
            <Ionicons
              name={isSaved ? 'bookmark' : 'bookmark-outline'}
              size={22}
              color={C.surface}
            />
          </TouchableOpacity>
          {recipe.imageUrl
            ? <Image source={{ uri: recipe.imageUrl }} style={styles.heroImage} resizeMode="cover" />
            : <View style={styles.heroImagePlaceholder}><Ionicons name="restaurant" size={64} color={C.primaryLight} /></View>
          }
          {recipe.isAIGenerated && (
            <View style={styles.aiBadge}>
              <Ionicons name="sparkles" size={14} color={C.secondary} />
              <Text style={styles.aiBadgeText}>AI Generated</Text>
            </View>
          )}
        </View>

        <View style={styles.content}>
          {/* Title */}
          <Text style={styles.recipeName}>{recipe.name}</Text>
          {recipe.description && <Text style={styles.recipeDesc}>{recipe.description}</Text>}

          {/* Stats */}
          <View style={styles.statsRow}>
            <View style={styles.statItem}>
              <Ionicons name="time-outline" size={20} color={C.primary} />
              <Text style={styles.statValue}>{recipe.prepTimeMinutes}m</Text>
              <Text style={styles.statLabel}>Prep</Text>
            </View>
            <View style={styles.statDivider} />
            <View style={styles.statItem}>
              <Ionicons name="flame-outline" size={20} color={C.secondary} />
              <Text style={styles.statValue}>{recipe.cookTimeMinutes}m</Text>
              <Text style={styles.statLabel}>Cook</Text>
            </View>
            <View style={styles.statDivider} />
            <View style={styles.statItem}>
              <Ionicons name="people-outline" size={20} color={C.info} />
              <Text style={styles.statValue}>{recipe.servings}</Text>
              <Text style={styles.statLabel}>Servings</Text>
            </View>
            <View style={styles.statDivider} />
            <View style={styles.statItem}>
              <Ionicons name="nutrition-outline" size={20} color={C.error} />
              <Text style={styles.statValue}>{Math.round(recipe.nutritionalInfoPerServing.calories)}</Text>
              <Text style={styles.statLabel}>kcal/srv</Text>
            </View>
          </View>

          {/* Tags */}
          {tags.length > 0 && (
            <View style={styles.tagRow}>
              {tags.map(tag => (
                <View key={tag} style={styles.tag}>
                  <Text style={styles.tagText}>{tag}</Text>
                </View>
              ))}
            </View>
          )}

          {/* Nutrition */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Nutrition per serving</Text>
            <View style={styles.nutritionGrid}>
              {[
                { label: 'Calories', value: Math.round(recipe.nutritionalInfoPerServing.calories), unit: 'kcal' },
                { label: 'Protein', value: Math.round(recipe.nutritionalInfoPerServing.protein), unit: 'g' },
                { label: 'Carbs', value: Math.round(recipe.nutritionalInfoPerServing.carbohydrates), unit: 'g' },
                { label: 'Fat', value: Math.round(recipe.nutritionalInfoPerServing.fat), unit: 'g' },
                { label: 'Fiber', value: Math.round(recipe.nutritionalInfoPerServing.fiber), unit: 'g' },
                { label: 'Sugar', value: Math.round(recipe.nutritionalInfoPerServing.sugar), unit: 'g' },
              ].map(({ label, value, unit }) => (
                <View key={label} style={styles.nutritionCell}>
                  <Text style={styles.nutritionValue}>{value}<Text style={styles.nutritionUnit}>{unit}</Text></Text>
                  <Text style={styles.nutritionLabel}>{label}</Text>
                </View>
              ))}
            </View>
          </View>

          {/* Ingredients */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Ingredients</Text>
            {recipe.ingredients.map((ing, i) => (
              <View key={`${ing.foodItemName}-${i}`} style={styles.ingredientRow}>
                <View style={styles.ingredientBullet} />
                <Text style={styles.ingredientText}>
                  <Text style={styles.ingredientQty}>{ing.quantity} {ing.unit} </Text>
                  {ing.foodItemName}
                  {ing.notes ? <Text style={styles.ingredientNotes}> ({ing.notes})</Text> : null}
                </Text>
              </View>
            ))}
          </View>

          {/* Instructions */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Instructions</Text>
            {steps.length === 0 &&
              (recipe.detailUnavailable ? (
                // A failed fetch is not the same as a recipe with no method. Saying the
                // latter would be wrong, and gives the reader no reason to come back.
                <View style={styles.noticeBox}>
                  <Text style={styles.noticeText}>
                    We couldn't load the steps for this recipe just now. This is usually
                    temporary.
                  </Text>
                  <TouchableOpacity onPress={retryDetail} disabled={isRetrying}>
                    <Text style={styles.noticeAction}>
                      {isRetrying ? 'Trying...' : 'Try again'}
                    </Text>
                  </TouchableOpacity>
                </View>
              ) : (
                <Text style={styles.noMethodText}>
                  {recipe.sourceUrl
                    ? `The method for this recipe is published by ${recipe.sourceName || 'the original site'} rather than here.`
                    : 'No method is available for this recipe.'}
                </Text>
              ))}
            {steps.map((step, i) => (
              <View key={i} style={styles.stepRow}>
                <View style={styles.stepNumber}>
                  <Text style={styles.stepNumberText}>{i + 1}</Text>
                </View>
                <Text style={styles.stepText}>{step.replace(/^\d+\.\s*/, '')}</Text>
              </View>
            ))}
          </View>

          {/* Actions */}
          <View style={styles.actions}>
            <TouchableOpacity
              style={styles.actionButton}
              onPress={() => Alert.alert('Add to Meal Plan', 'Feature coming soon!')}
            >
              <Ionicons name="calendar-outline" size={20} color={C.surface} />
              <Text style={styles.actionButtonText}>Add to Meal Plan</Text>
            </TouchableOpacity>
            <TouchableOpacity
              style={[styles.actionButton, styles.actionButtonSecondary]}
              onPress={() => Alert.alert('Log Meal', 'Feature coming soon!')}
            >
              <Ionicons name="add-circle-outline" size={20} color={C.primary} />
              <Text style={[styles.actionButtonText, styles.actionButtonTextSecondary]}>Log This Meal</Text>
            </TouchableOpacity>
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: { flex: 1, backgroundColor: C.background },
  centered: { justifyContent: 'center', alignItems: 'center' },
  hero: { height: 200, backgroundColor: C.primaryLight, justifyContent: 'center', alignItems: 'center' },
  backButton: {
    position: 'absolute',
    top: Spacing.lg,
    left: Spacing.lg,
    backgroundColor: 'rgba(0,0,0,0.3)',
    borderRadius: BorderRadius.full,
    padding: Spacing.sm,
  },
  saveButton: {
    position: 'absolute',
    top: Spacing.lg,
    right: Spacing.lg,
    backgroundColor: 'rgba(0,0,0,0.3)',
    borderRadius: BorderRadius.full,
    padding: Spacing.sm,
  },
  noMethodText: { fontSize: FontSize.md, color: C.textSecondary, lineHeight: 22 },
  noticeBox: {
    borderWidth: 1,
    borderColor: C.warning,
    borderRadius: BorderRadius.md,
    padding: Spacing.md,
    gap: Spacing.xs,
  },
  noticeText: { fontSize: FontSize.sm, color: C.text, lineHeight: 20 },
  noticeAction: { fontSize: FontSize.sm, fontWeight: FontWeight.semibold, color: C.primaryDark },
  heroImage: { width: '100%', height: '100%' },
  heroImagePlaceholder: { alignItems: 'center', justifyContent: 'center' },
  aiBadge: {
    position: 'absolute',
    bottom: Spacing.md,
    right: Spacing.md,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    backgroundColor: C.surface,
    paddingHorizontal: Spacing.sm,
    paddingVertical: 4,
    borderRadius: BorderRadius.full,
  },
  aiBadgeText: { fontSize: FontSize.sm, color: C.secondary, fontWeight: FontWeight.semibold },
  content: { padding: Spacing.lg },
  recipeName: { fontSize: FontSize.xxl, fontWeight: FontWeight.bold, color: C.text, marginBottom: Spacing.xs },
  recipeDesc: { fontSize: FontSize.md, color: C.textSecondary, lineHeight: 22, marginBottom: Spacing.md },
  statsRow: {
    flexDirection: 'row',
    backgroundColor: C.surface,
    borderRadius: BorderRadius.lg,
    padding: Spacing.md,
    marginBottom: Spacing.md,
    ...Shadows.sm,
  },
  statItem: { flex: 1, alignItems: 'center', gap: 2 },
  statDivider: { width: 1, backgroundColor: C.divider, marginVertical: Spacing.xs },
  statValue: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text },
  statLabel: { fontSize: FontSize.xs, color: C.textSecondary },
  tagRow: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.xs, marginBottom: Spacing.md },
  tag: { backgroundColor: C.primaryLight, paddingHorizontal: Spacing.sm, paddingVertical: 4, borderRadius: BorderRadius.full },
  tagText: { fontSize: FontSize.sm, color: C.primaryDark, fontWeight: FontWeight.medium },
  section: { marginBottom: Spacing.xl },
  sectionTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text, marginBottom: Spacing.md },
  nutritionGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.sm },
  nutritionCell: {
    width: '30%',
    backgroundColor: C.surface,
    borderRadius: BorderRadius.md,
    padding: Spacing.md,
    alignItems: 'center',
    ...Shadows.sm,
  },
  nutritionValue: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: C.text },
  nutritionUnit: { fontSize: FontSize.sm, color: C.textSecondary },
  nutritionLabel: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 2 },
  ingredientRow: { flexDirection: 'row', alignItems: 'flex-start', marginBottom: Spacing.sm, gap: Spacing.sm },
  ingredientBullet: { width: 6, height: 6, borderRadius: 3, backgroundColor: C.primary, marginTop: 8 },
  ingredientText: { flex: 1, fontSize: FontSize.md, color: C.text, lineHeight: 22 },
  ingredientQty: { fontWeight: FontWeight.semibold },
  ingredientNotes: { color: C.textSecondary, fontStyle: 'italic' },
  stepRow: { flexDirection: 'row', gap: Spacing.md, marginBottom: Spacing.md, alignItems: 'flex-start' },
  stepNumber: {
    width: 28,
    height: 28,
    borderRadius: 14,
    backgroundColor: C.primary,
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
  },
  stepNumberText: { color: C.surface, fontWeight: FontWeight.bold, fontSize: FontSize.sm },
  stepText: { flex: 1, fontSize: FontSize.md, color: C.text, lineHeight: 22 },
  actions: { gap: Spacing.md, marginBottom: Spacing.xxl },
  actionButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.sm,
    backgroundColor: C.primary,
    borderRadius: BorderRadius.lg,
    paddingVertical: Spacing.md,
  },
  actionButtonSecondary: { backgroundColor: C.surface, borderWidth: 2, borderColor: C.primary },
  actionButtonText: { color: C.surface, fontWeight: FontWeight.semibold, fontSize: FontSize.md },
  actionButtonTextSecondary: { color: C.primary },
  errorText: { fontSize: FontSize.lg, color: C.text, marginBottom: Spacing.md },
  backLink: { color: C.primary, fontWeight: FontWeight.semibold },
});
