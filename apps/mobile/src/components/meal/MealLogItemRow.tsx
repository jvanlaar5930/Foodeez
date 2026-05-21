import React, { useRef } from 'react';
import {
  Alert,
  Animated,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { Colors, FontSize, FontWeight, Spacing } from '@/constants/theme';
import type { MealLogItemDto } from '@/types';

interface MealLogItemRowProps {
  item: MealLogItemDto;
  onDelete?: () => void;
}

export function MealLogItemRow({ item, onDelete }: MealLogItemRowProps) {
  const translateX = useRef(new Animated.Value(0)).current;

  const calories = Math.round(item.nutritionalInfo.calories);
  const protein = Math.round(item.nutritionalInfo.protein);
  const carbs = Math.round(item.nutritionalInfo.carbohydrates);
  const fat = Math.round(item.nutritionalInfo.fat);

  const handleLongPress = () => {
    if (!onDelete) return;
    Alert.alert(
      'Remove Item',
      `Remove "${item.foodItem.name}" from this meal?`,
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Remove', style: 'destructive', onPress: onDelete },
      ],
    );
  };

  return (
    <TouchableOpacity
      style={styles.container}
      onLongPress={handleLongPress}
      activeOpacity={0.7}
    >
      <View style={styles.left}>
        <Text style={styles.foodName} numberOfLines={1}>
          {item.foodItem.name}
        </Text>
        <Text style={styles.serving}>
          {item.quantity} {item.unit}
          {item.foodItem.brand ? ` · ${item.foodItem.brand}` : ''}
        </Text>
        <View style={styles.macroRow}>
          <Text style={styles.macroText}>P: {protein}g</Text>
          <Text style={styles.macroDivider}>·</Text>
          <Text style={styles.macroText}>C: {carbs}g</Text>
          <Text style={styles.macroDivider}>·</Text>
          <Text style={styles.macroText}>F: {fat}g</Text>
        </View>
      </View>
      <View style={styles.right}>
        <Text style={styles.calories}>{calories}</Text>
        <Text style={styles.kcalLabel}>kcal</Text>
        {onDelete && (
          <TouchableOpacity onPress={handleLongPress} style={styles.deleteBtn} hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}>
            <Ionicons name="trash-outline" size={16} color={Colors.textHint} />
          </TouchableOpacity>
        )}
      </View>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: Colors.divider,
  },
  left: {
    flex: 1,
    marginRight: Spacing.sm,
  },
  foodName: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.medium,
    color: Colors.text,
    marginBottom: 2,
  },
  serving: {
    fontSize: FontSize.sm,
    color: Colors.textSecondary,
    marginBottom: 4,
  },
  macroRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  macroText: {
    fontSize: FontSize.xs,
    color: Colors.textHint,
  },
  macroDivider: {
    fontSize: FontSize.xs,
    color: Colors.textHint,
  },
  right: {
    alignItems: 'flex-end',
    minWidth: 52,
  },
  calories: {
    fontSize: FontSize.lg,
    fontWeight: FontWeight.semibold,
    color: Colors.text,
  },
  kcalLabel: {
    fontSize: FontSize.xs,
    color: Colors.textSecondary,
  },
  deleteBtn: {
    marginTop: Spacing.xs,
    padding: 2,
  },
});
