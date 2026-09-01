import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import type { FoodItemDto } from '@/types';

interface FoodSearchResultRowProps {
  item: FoodItemDto;
  onSelect: (item: FoodItemDto) => void;
  isSelected?: boolean;
}

export function FoodSearchResultRow({ item, onSelect, isSelected = false }: FoodSearchResultRowProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const calories = Math.round(item.nutritionalInfo.calories);

  return (
    <TouchableOpacity
      style={[styles.container, isSelected && styles.selectedContainer]}
      onPress={() => onSelect(item)}
      activeOpacity={0.7}
    >
      <View style={styles.left}>
        <Text style={styles.name} numberOfLines={1}>
          {item.name}
        </Text>
        {item.brand && <Text style={styles.brand}>{item.brand}</Text>}
        <Text style={styles.serving}>
          {item.servingSize} {item.servingUnit}
        </Text>
      </View>
      <View style={styles.right}>
        <Text style={styles.calories}>{calories} kcal</Text>
        {isSelected ? (
          <Ionicons name="checkmark-circle" size={24} color={C.primary} />
        ) : (
          <Ionicons name="add-circle-outline" size={24} color={C.primary} />
        )}
      </View>
    </TouchableOpacity>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm + 2,
    borderBottomWidth: 1,
    borderBottomColor: C.divider,
    backgroundColor: C.surface,
  },
  selectedContainer: {
    backgroundColor: C.primaryLight,
  },
  left: {
    flex: 1,
    marginRight: Spacing.sm,
  },
  name: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.medium,
    color: C.text,
    marginBottom: 2,
  },
  brand: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
    marginBottom: 2,
  },
  serving: {
    fontSize: FontSize.sm,
    color: C.textHint,
  },
  right: {
    alignItems: 'flex-end',
    gap: Spacing.xs,
  },
  calories: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.semibold,
    color: C.text,
  },
});
