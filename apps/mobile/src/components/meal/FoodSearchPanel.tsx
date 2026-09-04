import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { FoodSearchResultRow } from '@/components/meal/FoodSearchResultRow';
import { SearchBar } from '@/components/ui/SearchBar';
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import type { FoodItemDto } from '@/types';

interface FoodSearchPanelProps {
  query: string;
  onQueryChange: (text: string) => void;
  results: FoodItemDto[];
  isSearching: boolean;
  /** Whether a result is already in the meal, so the row can show it as picked. */
  isSelected: (foodItemId: string) => boolean;
  onSelect: (item: FoodItemDto) => void;
  /** Opens the homemade-food form, seeded with whatever was typed. */
  onAddCustom: () => void;
}

/**
 * Searching the food database, and the way out when nothing matches.
 *
 * A plain map rather than a nested FlatList: this sits inside the screen's ScrollView, and a
 * second vertical virtualised list there fights it for gestures - which is why the version
 * this replaces had `scrollEnabled={false}` on a FlatList, paying for virtualisation it had
 * disabled.
 */
export function FoodSearchPanel({
  query,
  onQueryChange,
  results,
  isSearching,
  isSelected,
  onSelect,
  onAddCustom,
}: FoodSearchPanelProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  const trimmed = query.trim();
  const showResults = trimmed.length > 0 || isSearching;

  return (
    <View>
      <View style={styles.searchWrap}>
        <SearchBar
          value={query}
          onChangeText={onQueryChange}
          placeholder="Search food items..."
          isSearching={isSearching}
        />
      </View>

      {showResults ? (
        <View style={styles.results}>
          <Text style={styles.label}>
            {isSearching ? 'Searching...' : `${results.length} results`}
          </Text>

          {results.map((item) => (
            <FoodSearchResultRow
              key={item.id}
              item={item}
              onSelect={onSelect}
              isSelected={isSelected(item.id)}
            />
          ))}

          {/* Homemade food will never be in a nutrition database, so offer to enter it. */}
          {!isSearching ? (
            <TouchableOpacity style={styles.addCustom} onPress={onAddCustom}>
              <Ionicons name="add-circle-outline" size={18} color={C.primary} />
              <Text style={styles.addCustomText}>
                {results.length === 0
                  ? `Can't find it? Add "${trimmed}" as a homemade food`
                  : 'None of these? Add a homemade food'}
              </Text>
            </TouchableOpacity>
          ) : null}
        </View>
      ) : null}
    </View>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    searchWrap: { paddingHorizontal: Spacing.md, paddingVertical: Spacing.sm },
    results: { paddingHorizontal: Spacing.md },
    label: {
      fontSize: FontSize.sm,
      fontWeight: FontWeight.semibold,
      color: C.textSecondary,
      marginBottom: Spacing.xs,
    },
    addCustom: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: Spacing.xs,
      paddingVertical: Spacing.sm,
    },
    addCustomText: { fontSize: FontSize.sm, color: C.primary, fontWeight: FontWeight.medium, flex: 1 },
  });
