import React, { useEffect, useState, useCallback, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  TextInput,
  ActivityIndicator,
  RefreshControl,
  Image,
} from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { RecipesStackParamList } from '@/navigation/types';
import { recipeService } from '@/services/recipeService';
import { useSavedRecipeStore } from '@/store/savedRecipeStore';
import { SavedRecipeDeck } from '@/components/recipe/SavedRecipeDeck';
import { isAiRecipeImage, RecipeDto } from '@/types';
import { AiRecipeThumb } from '@/components/recipe/AiRecipeThumb';
import { Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

type Props = NativeStackScreenProps<RecipesStackParamList, 'RecipesList'>;

const FILTER_TAGS = ['Vegetarian', 'Vegan', 'High-Protein', 'Low-Carb', 'Quick', 'Gluten-Free', 'Dairy-Free'];

export function RecipesScreen({ navigation, route }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const [recipes, setRecipes] = useState<RecipeDto[]>([]);
  const [filteredRecipes, setFilteredRecipes] = useState<RecipeDto[]>([]);
  const [search, setSearch] = useState('');
  const [activeTags, setActiveTags] = useState<Set<string>>(new Set());
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [hasMore, setHasMore] = useState(false);
  const pageRef = useRef(1);
  const searchRef = useRef<TextInput>(null);

  const savedRecipes = useSavedRecipeStore((s) => s.recipes);
  const savedIds = useSavedRecipeStore((s) => s.savedIds);
  const savedPending = useSavedRecipeStore((s) => s.pending);
  const savedHasLoaded = useSavedRecipeStore((s) => s.hasLoaded);
  const fetchSaved = useSavedRecipeStore((s) => s.fetch);
  const toggleSaved = useSavedRecipeStore((s) => s.toggle);

  // The deck is the user's own collection, so it stays out of the way while they search.
  const showDeck = search.trim().length === 0 && savedRecipes.length > 0;

  // Apply initialSearch whenever the screen comes into focus with a new query
  useFocusEffect(
    useCallback(() => {
      const q = route.params?.initialSearch ?? '';
      setSearch(q);
      if (q) {
        // Brief delay so the list has rendered before focusing
        setTimeout(() => searchRef.current?.focus(), 150);
      }
    }, [route.params?.initialSearch])
  );

  const loadRecipes = useCallback(async (query?: string) => {
    pageRef.current = 1;
    try {
      const q = query?.trim();
      const result = q
        ? await recipeService.searchRecipes(q, 1)
        : await recipeService.getRecipes(1);
      setRecipes(result.items);
      setHasMore(result.hasMore);
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, []);

  /**
   * Appends the next page as the list nears its end. The in-flight guard matters here:
   * FlatList fires onEndReached repeatedly while the user keeps scrolling.
   */
  const loadMore = useCallback(async () => {
    if (isLoadingMore || isLoading || !hasMore) return;

    setIsLoadingMore(true);
    const next = pageRef.current + 1;

    try {
      const q = search.trim();
      const result = q
        ? await recipeService.searchRecipes(q, next)
        : await recipeService.getRecipes(next);

      // Dedupe: a recipe cached between pages could otherwise arrive twice.
      setRecipes((prev) => {
        const seen = new Set(prev.map((r) => r.id));
        return [...prev, ...result.items.filter((r) => !seen.has(r.id))];
      });
      pageRef.current = next;
      setHasMore(result.hasMore && result.items.length > 0);
    } catch {
      setHasMore(false);
    } finally {
      setIsLoadingMore(false);
    }
  }, [hasMore, isLoading, isLoadingMore, search]);

  // Initial load
  useEffect(() => {
    loadRecipes();
  }, []);

  // Saves can be made from the detail screen, so re-read them on the way back in.
  // The first fetch is unconditional; later ones only matter once something has loaded.
  useFocusEffect(
    useCallback(() => {
      if (!savedHasLoaded) fetchSaved();
    }, [savedHasLoaded, fetchSaved])
  );

  // Debounced server search when text changes
  useEffect(() => {
    const timer = setTimeout(() => {
      setIsLoading(true);
      loadRecipes(search);
    }, 400);
    return () => clearTimeout(timer);
  }, [search]);

  // Client-side tag filter on top of server results
  useEffect(() => {
    const tags = Array.from(activeTags);
    setFilteredRecipes(
      tags.length === 0
        ? recipes
        : recipes.filter(r =>
            tags.every(t => r.tags?.toLowerCase().includes(t.toLowerCase()))
          )
    );
  }, [activeTags, recipes]);

  const toggleTag = (tag: string) => {
    setActiveTags(prev => {
      const next = new Set(prev);
      if (next.has(tag)) next.delete(tag);
      else next.add(tag);
      return next;
    });
  };

  const renderRecipe = ({ item }: { item: RecipeDto }) => {
    const isSaved = savedIds.has(item.id);

    return (
    <TouchableOpacity
      style={styles.recipeCard}
      onPress={() => navigation.navigate('RecipeDetail', { recipeId: item.id })}
    >
      <TouchableOpacity
        style={styles.saveButton}
        hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
        disabled={savedPending.has(item.id)}
        onPress={() => toggleSaved(item)}
        accessibilityLabel={isSaved ? `Remove ${item.name} from saved` : `Save ${item.name}`}
      >
        <Ionicons
          name={isSaved ? 'bookmark' : 'bookmark-outline'}
          size={20}
          color={isSaved ? C.primary : C.textSecondary}
        />
      </TouchableOpacity>
      <View style={styles.recipeImage}>
        {isAiRecipeImage(item.imageUrl)
          ? <AiRecipeThumb size={26} />
          : item.imageUrl
            ? <Image source={{ uri: item.imageUrl }} style={styles.recipeImagePhoto} resizeMode="cover" />
            : <Ionicons name="restaurant" size={32} color={C.primary} />
        }
      </View>
      <View style={styles.recipeInfo}>
        <Text style={styles.recipeName} numberOfLines={1}>{item.name}</Text>
        {item.description && (
          <Text style={styles.recipeDesc} numberOfLines={2}>{item.description}</Text>
        )}
        <View style={styles.recipeMeta}>
          <View style={styles.metaItem}>
            <Ionicons name="time-outline" size={14} color={C.textSecondary} />
            <Text style={styles.metaText}>{item.prepTimeMinutes + item.cookTimeMinutes}m</Text>
          </View>
          <View style={styles.metaItem}>
            <Ionicons name="flame-outline" size={14} color={C.textSecondary} />
            <Text style={styles.metaText}>
              {Math.round(item.nutritionalInfoPerServing.calories)} kcal
            </Text>
          </View>
          <View style={styles.metaItem}>
            <Ionicons name="people-outline" size={14} color={C.textSecondary} />
            <Text style={styles.metaText}>{item.servings} servings</Text>
          </View>
          {item.isAIGenerated && (
            <View style={styles.aiBadge}>
              <Ionicons name="sparkles" size={12} color={C.secondary} />
              <Text style={styles.aiBadgeText}>AI</Text>
            </View>
          )}
        </View>
        {item.tags && (
          <View style={styles.tagRow}>
            {item.tags.split(',').slice(0, 3).map(tag => (
              <View key={tag.trim()} style={styles.tag}>
                <Text style={styles.tagText}>{tag.trim()}</Text>
              </View>
            ))}
          </View>
        )}
      </View>
    </TouchableOpacity>
    );
  };

  const deckHeader = showDeck ? (
    <View style={styles.deckSection}>
      <View style={styles.deckHeader}>
        <Text style={styles.deckTitle}>Your saved recipes</Text>
        <Text style={styles.deckCount}>
          {savedRecipes.length} {savedRecipes.length === 1 ? 'card' : 'cards'}
        </Text>
      </View>
      <SavedRecipeDeck
        recipes={savedRecipes}
        onOpen={(r) => navigation.navigate('RecipeDetail', { recipeId: r.id })}
        onRemove={toggleSaved}
      />
    </View>
  ) : null;

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.searchContainer}>
        <Ionicons name="search" size={18} color={C.textSecondary} style={styles.searchIcon} />
        <TextInput
          ref={searchRef}
          style={styles.searchInput}
          placeholder="Search recipes..."
          placeholderTextColor={C.textHint}
          value={search}
          onChangeText={setSearch}
          returnKeyType="search"
        />
        {search.length > 0 && (
          <TouchableOpacity onPress={() => setSearch('')}>
            <Ionicons name="close-circle" size={18} color={C.textSecondary} />
          </TouchableOpacity>
        )}
      </View>

      <FlatList
        horizontal
        showsHorizontalScrollIndicator={false}
        data={FILTER_TAGS}
        keyExtractor={t => t}
        style={styles.tagFilter}
        contentContainerStyle={styles.tagFilterContent}
        renderItem={({ item: tag }) => (
          <TouchableOpacity
            style={[styles.filterChip, activeTags.has(tag) && styles.filterChipActive]}
            onPress={() => toggleTag(tag)}
          >
            <Text style={[styles.filterChipText, activeTags.has(tag) && styles.filterChipTextActive]}>
              {tag}
            </Text>
          </TouchableOpacity>
        )}
      />

      {isLoading ? (
        <ActivityIndicator size="large" color={C.primary} style={{ marginTop: Spacing.xl }} />
      ) : (
        <FlatList
          data={filteredRecipes}
          keyExtractor={r => r.id}
          contentContainerStyle={styles.list}
          ListHeaderComponent={deckHeader}
          refreshControl={
            <RefreshControl refreshing={isRefreshing} onRefresh={() => { setIsRefreshing(true); loadRecipes(); }} tintColor={C.primary} />
          }
          ListEmptyComponent={
            <View style={styles.empty}>
              <Ionicons name="book-outline" size={48} color={C.textHint} />
              <Text style={styles.emptyTitle}>No recipes found</Text>
              <Text style={styles.emptyText}>Try adjusting your search or filters</Text>
            </View>
          }
          ListFooterComponent={
            isLoadingMore ? (
              <View style={styles.footer}>
                <ActivityIndicator size="small" color={C.primary} />
                <Text style={styles.footerText}>Loading more recipes…</Text>
              </View>
            ) : hasMore ? (
              <TouchableOpacity style={styles.loadMoreButton} onPress={loadMore}>
                <Text style={styles.loadMoreText}>Load more recipes</Text>
              </TouchableOpacity>
            ) : recipes.length > 0 ? (
              <Text style={styles.footerDone}>That's everything for this search.</Text>
            ) : null
          }
          onEndReached={loadMore}
          onEndReachedThreshold={0.6}
          renderItem={renderRecipe}
        />
      )}
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: { flex: 1, backgroundColor: C.background },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: C.surface,
    marginHorizontal: Spacing.md,
    marginVertical: Spacing.sm,
    borderRadius: BorderRadius.lg,
    paddingHorizontal: Spacing.md,
    ...Shadows.sm,
  },
  searchIcon: { marginRight: Spacing.sm },
  searchInput: { flex: 1, height: 44, fontSize: FontSize.md, color: C.text },
  tagFilter: { flexGrow: 0, marginBottom: Spacing.sm },
  tagFilterContent: {
    paddingHorizontal: Spacing.md,
    paddingBottom: Spacing.sm,
    gap: Spacing.sm,
    alignItems: 'center',
  },
  filterChip: {
    borderWidth: 1,
    borderColor: C.divider,
    borderRadius: BorderRadius.full,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
    backgroundColor: C.surface,
  },
  filterChipActive: { backgroundColor: C.primary, borderColor: C.primary },
  filterChipText: { fontSize: FontSize.sm, color: C.textSecondary, fontWeight: FontWeight.medium },
  filterChipTextActive: { color: C.surface },
  list: { padding: Spacing.md, gap: Spacing.md, paddingBottom: Spacing.xl },
  recipeCard: {
    flexDirection: 'row',
    backgroundColor: C.surface,
    borderRadius: BorderRadius.lg,
    overflow: 'hidden',
    minHeight: 104,
    ...Shadows.sm,
  },
  recipeImage: {
    width: 96,
    // Stretch to whatever height the text column ends up being, rather than letting the
    // photo's own dimensions drive the card.
    alignSelf: 'stretch',
    backgroundColor: C.primaryLight,
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  // Absolute fill sidesteps percentage-height resolution entirely: the image can only ever
  // be as big as the column above.
  recipeImagePhoto: { ...StyleSheet.absoluteFill },
  // Extra room on the right so the save button never sits on top of a long title.
  recipeInfo: { flex: 1, padding: Spacing.md, paddingRight: Spacing.xl + Spacing.sm },
  saveButton: {
    position: 'absolute',
    top: Spacing.sm,
    right: Spacing.sm,
    zIndex: 1,
    padding: Spacing.xs,
  },
  deckSection: { marginBottom: Spacing.sm },
  deckHeader: {
    flexDirection: 'row',
    alignItems: 'baseline',
    justifyContent: 'space-between',
    paddingHorizontal: Spacing.xs,
  },
  deckTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text },
  deckCount: { fontSize: FontSize.sm, color: C.textSecondary },
  recipeName: { fontSize: FontSize.md, fontWeight: FontWeight.bold, color: C.text },
  recipeDesc: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2, lineHeight: 18 },
  recipeMeta: { flexDirection: 'row', alignItems: 'center', gap: Spacing.md, marginTop: Spacing.sm, flexWrap: 'wrap' },
  metaItem: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  metaText: { fontSize: FontSize.sm, color: C.textSecondary },
  aiBadge: { flexDirection: 'row', alignItems: 'center', gap: 2, backgroundColor: '#FFF3E0', paddingHorizontal: 6, paddingVertical: 2, borderRadius: BorderRadius.sm },
  aiBadgeText: { fontSize: FontSize.xs, color: C.secondary, fontWeight: FontWeight.semibold },
  tagRow: { flexDirection: 'row', gap: Spacing.xs, marginTop: Spacing.sm, flexWrap: 'wrap' },
  tag: { backgroundColor: C.primaryLight, paddingHorizontal: Spacing.sm, paddingVertical: 2, borderRadius: BorderRadius.sm },
  tagText: { fontSize: FontSize.xs, color: C.primaryDark, fontWeight: FontWeight.medium },
  footer: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: Spacing.sm, paddingVertical: Spacing.lg },
  footerText: { fontSize: FontSize.sm, color: C.textSecondary },
  footerDone: { textAlign: 'center', paddingVertical: Spacing.lg, fontSize: FontSize.sm, color: C.textHint },
  loadMoreButton: {
    alignSelf: 'center',
    borderWidth: 1,
    borderColor: C.divider,
    borderRadius: BorderRadius.full,
    paddingHorizontal: Spacing.lg,
    paddingVertical: Spacing.sm,
    marginVertical: Spacing.lg,
    backgroundColor: C.surface,
  },
  loadMoreText: { fontSize: FontSize.sm, color: C.text, fontWeight: FontWeight.medium },
  empty: { alignItems: 'center', paddingTop: Spacing.xxl, gap: Spacing.sm },
  emptyTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.semibold, color: C.text },
  emptyText: { fontSize: FontSize.md, color: C.textSecondary },
});
