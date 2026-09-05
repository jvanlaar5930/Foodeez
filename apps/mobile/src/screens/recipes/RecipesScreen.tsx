import React, { useEffect, useState, useCallback, useMemo, useRef } from 'react';
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
import { Ionicons } from '@expo/vector-icons';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { RecipesStackParamList } from '@/navigation/types';
import { recipeService } from '@/services/recipeService';
import { useSavedRecipeStore } from '@/store/savedRecipeStore';
import { SavedRecipeDeck } from '@/components/recipe/SavedRecipeDeck';
import { DEFAULT_RECIPE_FILTER_TAGS, isAiRecipeImage, RecipeDto } from '@/types';
import { AiRecipeThumb } from '@/components/recipe/AiRecipeThumb';
import { Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';
import { LoadingSpinner } from '@/components/ui/LoadingSpinner';
import { SearchBar } from '@/components/ui/SearchBar';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

type Props = NativeStackScreenProps<RecipesStackParamList, 'RecipesList'>;

export function RecipesScreen({ navigation, route }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  /** Lets the search effect do the initial load without waiting out its debounce. */
  const isFirstLoad = useRef(true);
  const [recipes, setRecipes] = useState<RecipeDto[]>([]);
  const [search, setSearch] = useState('');
  const [activeTags, setActiveTags] = useState<Set<string>>(new Set());
  // Administrator-managed. The built-in list shows until the server answers with theirs.
  const [filterTags, setFilterTags] = useState<string[]>([...DEFAULT_RECIPE_FILTER_TAGS]);
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

  // Saves can be made from the detail screen, so re-read them on the way back in.
  // The first fetch is unconditional; later ones only matter once something has loaded.
  useFocusEffect(
    useCallback(() => {
      if (!savedHasLoaded) fetchSaved();
    }, [savedHasLoaded, fetchSaved])
  );

  /**
   * The server search, debounced - and the first load too.
   *
   * There used to be a separate "initial load" effect as well, so opening the screen fired
   * two identical requests for page one: this effect runs on mount like any other. It is the
   * only one now, and skips the wait on that first pass so the list is not empty for 400ms
   * before anything arrives.
   */
  useEffect(() => {
    if (isFirstLoad.current) {
      isFirstLoad.current = false;
      loadRecipes(search);
      return;
    }

    const timer = setTimeout(() => {
      setIsLoading(true);
      loadRecipes(search);
    }, 400);

    return () => clearTimeout(timer);
  }, [search, loadRecipes]);

  /**
   * The pills an administrator has configured, read once per mount. A tag they have since
   * removed is dropped from the selection as well, or the list would stay filtered by
   * something the screen no longer offers a way to switch off.
   */
  useEffect(() => {
    let cancelled = false;

    void recipeService.getFilterTags().then((tags) => {
      if (cancelled) return;

      setFilterTags(tags);
      setActiveTags((prev) => {
        const offered = new Set(tags.map((t) => t.toLowerCase()));
        const kept = Array.from(prev).filter((t) => offered.has(t.toLowerCase()));
        return kept.length === prev.size ? prev : new Set(kept);
      });
    });

    return () => {
      cancelled = true;
    };
  }, []);

  /**
   * The tag filter on top of the server's results. Derived, not stored: keeping it in state
   * behind a third effect meant every search landed twice - once with the new recipes and the
   * old filtered list, then again once the effect caught up.
   */
  const filteredRecipes = useMemo(() => {
    const tags = Array.from(activeTags);
    if (tags.length === 0) return recipes;

    return recipes.filter((r) => tags.every((t) => r.tags?.toLowerCase().includes(t.toLowerCase())));
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
    // A plain View, not a SafeAreaView: this screen sits under the stack's "Recipes" header,
    // which has already taken the status bar's height out. Insetting again put a band of
    // background between the header and the search box.
    <View style={styles.container}>
      <View style={styles.searchContainer}>
        <SearchBar
          ref={searchRef}
          value={search}
          onChangeText={setSearch}
          placeholder="Search recipes..."
        />
      </View>

      <FlatList
        horizontal
        showsHorizontalScrollIndicator={false}
        data={filterTags}
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
        <LoadingSpinner />
      ) : (
        <FlatList
          data={filteredRecipes}
          keyExtractor={r => r.id}
          // Takes the column's leftover height instead of asking for all of its content's,
          // which is what pushed the chip row above into being shrunk.
          style={styles.results}
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
    </View>
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
  /**
   * A horizontal list inherits `flexShrink: 1` from ScrollView, and the results list below it
   * asks for more height than the column has - so this row was being squeezed, and the chips
   * came out with their tops and bottoms sliced off. It has one job and a fixed height for
   * it: never grow, never shrink.
   */
  tagFilter: { flexGrow: 0, flexShrink: 0, marginBottom: Spacing.sm },
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
    // Android lays a 14sp label out taller than the padding implies, so the pill gets a
    // height of its own rather than one derived from the text inside it.
    minHeight: 32,
    justifyContent: 'center',
    backgroundColor: C.surface,
  },
  filterChipActive: { backgroundColor: C.primary, borderColor: C.primary },
  filterChipText: {
    fontSize: FontSize.sm,
    lineHeight: 18,
    color: C.textSecondary,
    fontWeight: FontWeight.medium,
  },
  filterChipTextActive: { color: C.surface },
  results: { flex: 1 },
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
