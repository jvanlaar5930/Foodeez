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
import { RecipeDto } from '@/types';
import { Colors, Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';

type Props = NativeStackScreenProps<RecipesStackParamList, 'RecipesList'>;

const FILTER_TAGS = ['Vegetarian', 'Vegan', 'High-Protein', 'Low-Carb', 'Quick', 'Gluten-Free', 'Dairy-Free'];

export function RecipesScreen({ navigation, route }: Props) {
  const [recipes, setRecipes] = useState<RecipeDto[]>([]);
  const [filteredRecipes, setFilteredRecipes] = useState<RecipeDto[]>([]);
  const [search, setSearch] = useState('');
  const [activeTags, setActiveTags] = useState<Set<string>>(new Set());
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const searchRef = useRef<TextInput>(null);

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
    try {
      const data = query?.trim()
        ? await recipeService.searchRecipes(query.trim())
        : await recipeService.getRecipes();
      setRecipes(data);
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, []);

  // Initial load
  useEffect(() => {
    loadRecipes();
  }, []);

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

  const renderRecipe = ({ item }: { item: RecipeDto }) => (
    <TouchableOpacity
      style={styles.recipeCard}
      onPress={() => navigation.navigate('RecipeDetail', { recipeId: item.id })}
    >
      <View style={styles.recipeImage}>
        {item.imageUrl
          ? <Image source={{ uri: item.imageUrl }} style={styles.recipeImagePhoto} resizeMode="cover" />
          : <Ionicons name="restaurant" size={32} color={Colors.primary} />
        }
      </View>
      <View style={styles.recipeInfo}>
        <Text style={styles.recipeName}>{item.name}</Text>
        {item.description && (
          <Text style={styles.recipeDesc} numberOfLines={2}>{item.description}</Text>
        )}
        <View style={styles.recipeMeta}>
          <View style={styles.metaItem}>
            <Ionicons name="time-outline" size={14} color={Colors.textSecondary} />
            <Text style={styles.metaText}>{item.prepTimeMinutes + item.cookTimeMinutes}m</Text>
          </View>
          <View style={styles.metaItem}>
            <Ionicons name="flame-outline" size={14} color={Colors.textSecondary} />
            <Text style={styles.metaText}>
              {Math.round(item.nutritionalInfoPerServing.calories)} kcal
            </Text>
          </View>
          <View style={styles.metaItem}>
            <Ionicons name="people-outline" size={14} color={Colors.textSecondary} />
            <Text style={styles.metaText}>{item.servings} servings</Text>
          </View>
          {item.isAIGenerated && (
            <View style={styles.aiBadge}>
              <Ionicons name="sparkles" size={12} color={Colors.secondary} />
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

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.searchContainer}>
        <Ionicons name="search" size={18} color={Colors.textSecondary} style={styles.searchIcon} />
        <TextInput
          ref={searchRef}
          style={styles.searchInput}
          placeholder="Search recipes..."
          placeholderTextColor={Colors.textHint}
          value={search}
          onChangeText={setSearch}
          returnKeyType="search"
        />
        {search.length > 0 && (
          <TouchableOpacity onPress={() => setSearch('')}>
            <Ionicons name="close-circle" size={18} color={Colors.textSecondary} />
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
        <ActivityIndicator size="large" color={Colors.primary} style={{ marginTop: Spacing.xl }} />
      ) : (
        <FlatList
          data={filteredRecipes}
          keyExtractor={r => r.id}
          contentContainerStyle={styles.list}
          refreshControl={
            <RefreshControl refreshing={isRefreshing} onRefresh={() => { setIsRefreshing(true); loadRecipes(); }} tintColor={Colors.primary} />
          }
          ListEmptyComponent={
            <View style={styles.empty}>
              <Ionicons name="book-outline" size={48} color={Colors.textHint} />
              <Text style={styles.emptyTitle}>No recipes found</Text>
              <Text style={styles.emptyText}>Try adjusting your search or filters</Text>
            </View>
          }
          renderItem={renderRecipe}
        />
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: Colors.background },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.surface,
    marginHorizontal: Spacing.md,
    marginVertical: Spacing.sm,
    borderRadius: BorderRadius.lg,
    paddingHorizontal: Spacing.md,
    ...Shadows.sm,
  },
  searchIcon: { marginRight: Spacing.sm },
  searchInput: { flex: 1, height: 44, fontSize: FontSize.md, color: Colors.text },
  tagFilter: { maxHeight: 48 },
  tagFilterContent: { paddingHorizontal: Spacing.md, gap: Spacing.sm, alignItems: 'center' },
  filterChip: {
    borderWidth: 1,
    borderColor: Colors.divider,
    borderRadius: BorderRadius.full,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
    backgroundColor: Colors.surface,
  },
  filterChipActive: { backgroundColor: Colors.primary, borderColor: Colors.primary },
  filterChipText: { fontSize: FontSize.sm, color: Colors.textSecondary, fontWeight: FontWeight.medium },
  filterChipTextActive: { color: Colors.surface },
  list: { padding: Spacing.md, gap: Spacing.md, paddingBottom: Spacing.xl },
  recipeCard: {
    flexDirection: 'row',
    backgroundColor: Colors.surface,
    borderRadius: BorderRadius.lg,
    overflow: 'hidden',
    ...Shadows.sm,
  },
  recipeImage: {
    width: 88,
    backgroundColor: Colors.primaryLight,
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  recipeImagePhoto: {
    width: 88,
    height: '100%',
  },
  recipeInfo: { flex: 1, padding: Spacing.md },
  recipeName: { fontSize: FontSize.md, fontWeight: FontWeight.bold, color: Colors.text },
  recipeDesc: { fontSize: FontSize.sm, color: Colors.textSecondary, marginTop: 2, lineHeight: 18 },
  recipeMeta: { flexDirection: 'row', alignItems: 'center', gap: Spacing.md, marginTop: Spacing.sm, flexWrap: 'wrap' },
  metaItem: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  metaText: { fontSize: FontSize.sm, color: Colors.textSecondary },
  aiBadge: { flexDirection: 'row', alignItems: 'center', gap: 2, backgroundColor: '#FFF3E0', paddingHorizontal: 6, paddingVertical: 2, borderRadius: BorderRadius.sm },
  aiBadgeText: { fontSize: FontSize.xs, color: Colors.secondary, fontWeight: FontWeight.semibold },
  tagRow: { flexDirection: 'row', gap: Spacing.xs, marginTop: Spacing.sm, flexWrap: 'wrap' },
  tag: { backgroundColor: Colors.primaryLight, paddingHorizontal: Spacing.sm, paddingVertical: 2, borderRadius: BorderRadius.sm },
  tagText: { fontSize: FontSize.xs, color: Colors.primaryDark, fontWeight: FontWeight.medium },
  empty: { alignItems: 'center', paddingTop: Spacing.xxl, gap: Spacing.sm },
  emptyTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.semibold, color: Colors.text },
  emptyText: { fontSize: FontSize.md, color: Colors.textSecondary },
});
