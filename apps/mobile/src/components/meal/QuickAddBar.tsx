import React, { useCallback, useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { mealService } from '@/services/mealService';
import { mealTemplateService } from '@/services/mealTemplateService';
import { useAuthStore } from '@/store/authStore';
import { describeApiError } from '@/utils/apiError';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import type { MealTemplateDto, QuickAddResultDto, RecentMealDto } from '@/types';

interface Props {
  onApplied: (result: QuickAddResultDto) => void;
  /** Bumped by the screen after it saves a meal, so the new name appears in the list. */
  reloadToken?: number;
  onScanPress?: () => void;
}

/**
 * Describe a whole meal in one line instead of searching out its parts one at a time, with the
 * meals eaten often kept a tap away underneath.
 */
export function QuickAddBar({ onApplied, reloadToken = 0, onScanPress }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const user = useAuthStore((state) => state.user);

  const [description, setDescription] = useState('');
  const [isBusy, setIsBusy] = useState(false);
  const [savedMeals, setSavedMeals] = useState<MealTemplateDto[]>([]);
  const [recentMeals, setRecentMeals] = useState<RecentMealDto[]>([]);

  const loadLists = useCallback(async () => {
    try {
      const [saved, recent] = await Promise.all([
        mealTemplateService.list(),
        mealTemplateService.recent(),
      ]);
      setSavedMeals(saved);
      // A meal already saved under a name is offered as that name, so showing it here too
      // would put the same meal on screen twice under two different labels.
      setRecentMeals(recent.filter((meal) => !meal.isSaved));
    } catch {
      // A convenience on top of the box above; failing to load it is not worth an alert.
    }
  }, []);

  useEffect(() => {
    loadLists();
  }, [loadLists, reloadToken]);

  const run = async (work: () => Promise<QuickAddResultDto>) => {
    setIsBusy(true);
    try {
      const result = await work();

      if (result.items.length === 0) {
        Alert.alert(
          'Nothing recognised',
          result.note ?? 'No foods could be picked out of that. Try naming them more plainly.',
        );
        return;
      }

      onApplied(result);
      setDescription('');

      if (result.note) {
        Alert.alert('Added, with an assumption', result.note);
      }
    } catch (err: unknown) {
      // A timeout reads as "no response" here just like a dropped connection would - both
      // mean the answer never made it back, whatever the server ended up doing with it.
      Alert.alert(
        'Could not read that',
        describeApiError(err, 'Try again, or add the items by searching below.'),
      );
    } finally {
      setIsBusy(false);
    }
  };

  const submit = () => {
    const text = description.trim();
    if (!text || isBusy || !user) {
      return;
    }
    run(() => mealService.quickAdd(user.id, text));
  };

  /**
   * A recent meal already carries its items, so logging one again is a local operation -
   * there is nothing to ask the server for, and no reason to make the user wait for it.
   */
  const applyRecent = (meal: RecentMealDto) => {
    onApplied({
      items: meal.items.map((item) => ({
        foodItem: item.foodItem,
        quantity: item.quantity,
        unit: item.unit,
        source: 'Saved',
        confidence: 1,
      })),
      mealType: meal.mealType,
      fromSavedMeal: true,
    });
  };

  return (
    <View style={styles.container}>
      <TextInput
        style={styles.input}
        value={description}
        onChangeText={setDescription}
        placeholder="Describe the whole meal at once..."
        placeholderTextColor={C.textHint}
        editable={!isBusy}
        multiline
        textAlignVertical="top"
        returnKeyType="done"
        blurOnSubmit
        onSubmitEditing={submit}
      />

      {/* Stacked rather than squeezed beside the box, so both the text and the buttons
          get room - two full-width taps instead of two buttons fighting the input for
          width. */}
      <View style={styles.buttonStack}>
        {onScanPress && (
          <TouchableOpacity style={styles.scanButton} onPress={onScanPress} disabled={isBusy}>
            <Ionicons name="camera-outline" size={18} color={C.primary} />
            <Text style={styles.scanButtonText}>Scan a photo instead</Text>
          </TouchableOpacity>
        )}
        <TouchableOpacity
          style={[styles.addButton, (!description.trim() || isBusy) && styles.addButtonDisabled]}
          onPress={submit}
          disabled={!description.trim() || isBusy}
        >
          {isBusy ? (
            <ActivityIndicator size="small" color={C.onPrimary} />
          ) : (
            <Text style={styles.addButtonText}>Add to meal</Text>
          )}
        </TouchableOpacity>
      </View>

      {savedMeals.length > 0 && (
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.chipRow}
          keyboardShouldPersistTaps="handled"
        >
          {savedMeals.map((meal) => (
            <TouchableOpacity
              key={meal.id}
              style={styles.savedChip}
              disabled={isBusy}
              onPress={() => run(() => mealTemplateService.apply(meal.id))}
            >
              <Ionicons name="bookmark" size={12} color={C.primary} />
              <Text style={styles.savedChipText} numberOfLines={1}>
                {meal.name}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      )}

      {recentMeals.length > 0 && (
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.chipRow}
          keyboardShouldPersistTaps="handled"
        >
          {recentMeals.map((meal) => (
            <TouchableOpacity
              key={meal.mealLogId}
              style={styles.recentChip}
              onPress={() => applyRecent(meal)}
            >
              <Ionicons name="repeat" size={12} color={C.textSecondary} />
              <Text style={styles.recentChipText} numberOfLines={1}>
                {meal.summary}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      )}
    </View>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: {
    paddingHorizontal: Spacing.md,
    paddingTop: Spacing.sm,
    gap: Spacing.xs,
  },
  input: {
    minHeight: 90,
    maxHeight: 160,
    backgroundColor: C.surface,
    borderWidth: 1,
    borderColor: C.primary,
    borderRadius: BorderRadius.md,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
    fontSize: FontSize.md,
    color: C.text,
  },
  buttonStack: {
    gap: Spacing.xs,
  },
  scanButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.xs,
    height: 44,
    borderWidth: 1,
    borderColor: C.primary,
    borderRadius: BorderRadius.md,
  },
  scanButtonText: {
    color: C.primary,
    fontSize: FontSize.md,
    fontWeight: FontWeight.medium,
  },
  addButton: {
    height: 44,
    paddingHorizontal: Spacing.md,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: C.primary,
    borderRadius: BorderRadius.md,
  },
  addButtonDisabled: {
    opacity: 0.5,
  },
  addButtonText: {
    color: C.surface,
    fontSize: FontSize.md,
    fontWeight: FontWeight.semibold,
  },
  chipRow: {
    gap: Spacing.xs,
    paddingVertical: 2,
  },
  savedChip: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    maxWidth: 200,
    paddingHorizontal: Spacing.sm,
    paddingVertical: 6,
    borderRadius: BorderRadius.full,
    borderWidth: 1,
    borderColor: C.primary,
    backgroundColor: C.surface,
  },
  savedChipText: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.medium,
    color: C.primary,
    flexShrink: 1,
  },
  recentChip: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    maxWidth: 220,
    paddingHorizontal: Spacing.sm,
    paddingVertical: 6,
    borderRadius: BorderRadius.full,
    borderWidth: 1,
    borderColor: C.divider,
    backgroundColor: C.surface,
  },
  recentChipText: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
    flexShrink: 1,
  },
});
