import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  Modal,
  ScrollView,
  TouchableOpacity,
  KeyboardAvoidingView,
  Platform,
  Alert,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { createFoodItem } from '@/services/foodItemService';
import { estimateNutrition } from '@/services/aiService';
import type { FoodItemDto } from '@/types';
import { Spacing, FontSize, BorderRadius, FontWeight } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import { describeApiError } from '@/utils/apiError';

const UNITS = ['g', 'ml', 'piece', 'slice', 'cup', 'tbsp', 'tsp', 'serving'];

interface Props {
  visible: boolean;
  /** Prefills the name from whatever the user was searching for. */
  initialName?: string;
  onClose: () => void;
  onCreated: (item: FoodItemDto) => void;
}

export function CustomFoodModal({ visible, initialName = '', onClose, onCreated }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  const [name, setName] = useState(initialName);
  const [servingSize, setServingSize] = useState('100');
  const [servingUnit, setServingUnit] = useState('g');
  const [calories, setCalories] = useState('');
  const [protein, setProtein] = useState('');
  const [carbs, setCarbs] = useState('');
  const [fat, setFat] = useState('');
  const [ingredients, setIngredients] = useState('');
  const [servings, setServings] = useState('1');
  const [isEstimating, setIsEstimating] = useState(false);
  const [estimateNote, setEstimateNote] = useState<string | null>(null);
  const [confidence, setConfidence] = useState<'low' | 'medium' | 'high' | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [errors, setErrors] = useState<{ name?: string; servingSize?: string; calories?: string }>({});

  // Re-seed the name whenever the sheet is reopened from a new search.
  const [lastInitial, setLastInitial] = useState(initialName);
  if (visible && initialName !== lastInitial) {
    setLastInitial(initialName);
    setName(initialName);
  }

  const num = (v: string) => {
    const n = parseFloat(v);
    return isNaN(n) ? 0 : n;
  };

  const validate = () => {
    const next: typeof errors = {};
    if (!name.trim()) next.name = 'Give it a name';
    if (num(servingSize) <= 0) next.servingSize = 'Must be more than zero';
    if (!calories.trim() || num(calories) < 0) next.calories = 'Enter calories per serving';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  // Nobody knows the macros of their own cooking, so the numbers get estimated from a plain
  // description of what went in. Everything stays editable afterwards.
  const handleEstimate = async () => {
    if (!name.trim()) {
      setErrors((e) => ({ ...e, name: 'Name the dish first' }));
      return;
    }

    setIsEstimating(true);
    setEstimateNote(null);
    try {
      const result = await estimateNutrition({
        name: name.trim(),
        ingredients: ingredients.trim() || undefined,
        servings: Math.max(1, parseInt(servings, 10) || 1),
        servingDescription: `${servingSize} ${servingUnit}`.trim(),
      });

      if (!result.succeeded) {
        setEstimateNote('Could not estimate this one. Enter whatever values you know.');
        return;
      }

      const n = result.perServing;
      setCalories(String(Math.round(n.calories)));
      setProtein(String(Math.round(n.protein)));
      setCarbs(String(Math.round(n.carbohydrates)));
      setFat(String(Math.round(n.fat)));
      if (result.servingSize > 0) {
        setServingSize(String(Math.round(result.servingSize)));
        if (result.servingUnit) setServingUnit(result.servingUnit);
      }
      setConfidence(result.confidence);
      setEstimateNote(result.assumptions ?? null);
      setErrors({});
    } catch (err: unknown) {
      setEstimateNote(describeApiError(err, 'Estimate unavailable. Enter values manually.'));
    } finally {
      setIsEstimating(false);
    }
  };

  const handleSave = async () => {
    if (!validate()) return;
    setIsSaving(true);
    try {
      const created = await createFoodItem({
        name: name.trim(),
        servingSize: num(servingSize),
        servingUnit,
        category: 'Homemade',
        calories: num(calories),
        protein: num(protein),
        carbohydrates: num(carbs),
        fat: num(fat),
        fiber: 0,
        sugar: 0,
        sodium: 0,
      });
      onCreated(created);
      reset();
    } catch (err: unknown) {
      Alert.alert('Could not save', describeApiError(err, 'Please try again.'));
    } finally {
      setIsSaving(false);
    }
  };

  const reset = () => {
    setServingSize('100');
    setServingUnit('g');
    setCalories('');
    setProtein('');
    setCarbs('');
    setFat('');
    setErrors({});
  };

  return (
    <Modal visible={visible} animationType="slide" transparent onRequestClose={onClose}>
      <View style={styles.backdrop}>
        <KeyboardAvoidingView
          style={styles.sheetWrap}
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        >
          <View style={styles.sheet}>
            <View style={styles.header}>
              <View style={styles.headerText}>
                <Text style={styles.title}>Add a homemade food</Text>
                <Text style={styles.subtitle}>
                  Saved to your account, so it shows up in search next time.
                </Text>
              </View>
              <TouchableOpacity onPress={onClose} accessibilityLabel="Close">
                <Ionicons name="close" size={24} color={C.textSecondary} />
              </TouchableOpacity>
            </View>

            <ScrollView
              contentContainerStyle={styles.content}
              keyboardShouldPersistTaps="handled"
              showsVerticalScrollIndicator={false}
            >
              <Input
                label="Name"
                value={name}
                onChangeText={setName}
                error={errors.name}
                placeholder="Nan's lasagne"
              />

              <View style={styles.row}>
                <View style={styles.rowItem}>
                  <Input
                    label="Serving size"
                    value={servingSize}
                    onChangeText={setServingSize}
                    error={errors.servingSize}
                    keyboardType="numeric"
                  />
                </View>
                <View style={styles.rowItem}>
                  <Text style={styles.label}>Unit</Text>
                  <ScrollView horizontal showsHorizontalScrollIndicator={false}>
                    <View style={styles.unitRow}>
                      {UNITS.map((u) => (
                        <TouchableOpacity
                          key={u}
                          style={[styles.unitChip, servingUnit === u && styles.unitChipActive]}
                          onPress={() => setServingUnit(u)}
                        >
                          <Text
                            style={[
                              styles.unitChipText,
                              servingUnit === u && styles.unitChipTextActive,
                            ]}
                          >
                            {u}
                          </Text>
                        </TouchableOpacity>
                      ))}
                    </View>
                  </ScrollView>
                </View>
              </View>

              <Text style={styles.sectionLabel}>What went into it?</Text>
              <Input
                label="Ingredients and rough amounts"
                value={ingredients}
                onChangeText={setIngredients}
                placeholder={'500g beef mince\n2 jars passata\n200g cheddar'}
                multiline
              />
              <View style={styles.row}>
                <View style={styles.rowItem}>
                  <Input
                    label="Servings in batch"
                    value={servings}
                    onChangeText={setServings}
                    keyboardType="numeric"
                  />
                </View>
                <View style={[styles.rowItem, styles.estimateButtonWrap]}>
                  <Button
                    title="Estimate"
                    variant="outline"
                    loading={isEstimating}
                    onPress={handleEstimate}
                  />
                </View>
              </View>
              <Text style={styles.hint}>
                Rough amounts are fine, and you can skip this and type the numbers yourself.
              </Text>

              {estimateNote !== null && (
                <View style={styles.estimateNote}>
                  {confidence !== null && (
                    <Text style={styles.estimateConfidence}>
                      {confidence === 'high'
                        ? 'Confident — amounts were specific.'
                        : confidence === 'medium'
                          ? 'Reasonable — some amounts assumed.'
                          : 'Rough — add amounts for a closer estimate.'}
                    </Text>
                  )}
                  <Text style={styles.estimateNoteText}>{estimateNote}</Text>
                </View>
              )}

              <Text style={styles.sectionLabel}>Nutrition per serving</Text>
              <View style={styles.row}>
                <View style={styles.rowItem}>
                  <Input
                    label="Calories"
                    value={calories}
                    onChangeText={setCalories}
                    error={errors.calories}
                    keyboardType="numeric"
                  />
                </View>
                <View style={styles.rowItem}>
                  <Input
                    label="Protein (g)"
                    value={protein}
                    onChangeText={setProtein}
                    keyboardType="numeric"
                  />
                </View>
              </View>
              <View style={styles.row}>
                <View style={styles.rowItem}>
                  <Input
                    label="Carbs (g)"
                    value={carbs}
                    onChangeText={setCarbs}
                    keyboardType="numeric"
                  />
                </View>
                <View style={styles.rowItem}>
                  <Input label="Fat (g)" value={fat} onChangeText={setFat} keyboardType="numeric" />
                </View>
              </View>

              <Text style={styles.hint}>
                Estimates are a starting point, not a lab result — correct anything that looks
                off. Only calories are required; blanks count as zero.
              </Text>

              <Button
                title="Save & add"
                size="lg"
                fullWidth
                loading={isSaving}
                onPress={handleSave}
              />
            </ScrollView>
          </View>
        </KeyboardAvoidingView>
      </View>
    </Modal>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    backdrop: { flex: 1, backgroundColor: C.overlay, justifyContent: 'flex-end' },
    sheetWrap: { maxHeight: '90%' },
    sheet: {
      backgroundColor: C.background,
      borderTopLeftRadius: BorderRadius.xl,
      borderTopRightRadius: BorderRadius.xl,
      paddingTop: Spacing.lg,
    },
    header: {
      flexDirection: 'row',
      alignItems: 'flex-start',
      gap: Spacing.md,
      paddingHorizontal: Spacing.lg,
      paddingBottom: Spacing.md,
    },
    headerText: { flex: 1 },
    title: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: C.text },
    subtitle: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2 },
    content: { paddingHorizontal: Spacing.lg, paddingBottom: Spacing.xxl, gap: Spacing.xs },
    row: { flexDirection: 'row', gap: Spacing.md },
    rowItem: { flex: 1 },
    label: {
      fontSize: FontSize.sm,
      fontWeight: FontWeight.medium,
      color: C.text,
      marginBottom: Spacing.xs,
    },
    unitRow: { flexDirection: 'row', gap: Spacing.xs, paddingBottom: Spacing.sm },
    unitChip: {
      borderWidth: 1,
      borderColor: C.divider,
      backgroundColor: C.surface,
      borderRadius: BorderRadius.full,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.xs,
    },
    unitChipActive: { backgroundColor: C.primary, borderColor: C.primary },
    unitChipText: { fontSize: FontSize.sm, color: C.textSecondary },
    unitChipTextActive: { color: '#FFFFFF', fontWeight: FontWeight.semibold },
    sectionLabel: {
      fontSize: FontSize.md,
      fontWeight: FontWeight.semibold,
      color: C.text,
      marginTop: Spacing.md,
      marginBottom: Spacing.xs,
    },
    estimateButtonWrap: { justifyContent: 'flex-end', paddingBottom: Spacing.md },
    estimateNote: {
      backgroundColor: C.surface,
      borderRadius: BorderRadius.md,
      padding: Spacing.md,
      marginBottom: Spacing.sm,
      gap: 2,
    },
    estimateConfidence: {
      fontSize: FontSize.sm,
      fontWeight: FontWeight.semibold,
      color: C.text,
    },
    estimateNoteText: { fontSize: FontSize.sm, color: C.textSecondary, lineHeight: 18 },
    hint: {
      fontSize: FontSize.sm,
      color: C.textSecondary,
      marginBottom: Spacing.md,
      lineHeight: 18,
    },
  });
