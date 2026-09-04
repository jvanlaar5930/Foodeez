import React, { useCallback, useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  FlatList,
  Alert,
  TextInput,
  Image,
  ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { format, parseISO } from 'date-fns';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { MealPlanStackParamList } from '@/navigation/types';
import { useMealPlanStore } from '@/store/mealPlanStore';
import { entryLabel, loggedLabel } from '@/utils/mealPlanLabels';
import { LinkedRecipeCard } from '@/components/mealplan/LinkedRecipeCard';
import { ModalActions } from '@/components/ui/ModalActions';
import { ModalSheet } from '@/components/ui/ModalSheet';
import { useAuthStore } from '@/store/authStore';
import { mealService } from '@/services/mealService';
import { recipeService } from '@/services/recipeService';
import { AiRecipeThumb } from '@/components/recipe/AiRecipeThumb';
import {
  isAiRecipeImage,
  MEAL_TYPE_LABELS,
  MealLogDto,
  MealPlanEntryDto,
  MealType,
  ORDERED_MEAL_TYPES,
  RecipeDto,
} from '@/types';
import { MEAL_TYPE_ICONS } from '@/constants/mealTypes';
import { Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

type Props = NativeStackScreenProps<MealPlanStackParamList, 'CalendarDay'>;

/**
 * What to call a planned meal. AI-generated entries have no Recipe or FoodItem row behind
 * them, so both names are null and the meal's own name is in `notes` - without this every
 * generated slot read "Custom meal".
 */


export function CalendarDayScreen({ route, navigation }: Props) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const { date } = route.params;
  const parsedDate = parseISO(date);
  const user = useAuthStore((state) => state.user);
  const activePlan = useMealPlanStore((state) => state.activePlan);
  const ensurePlanFor = useMealPlanStore((state) => state.ensurePlanFor);
  const saveEntry = useMealPlanStore((state) => state.saveEntry);
  const removeEntry = useMealPlanStore((state) => state.removeEntry);
  const clearError = useMealPlanStore((state) => state.clearError);

  const [loggedLogs, setLoggedLogs] = useState<MealLogDto[]>([]);
  const [saving, setSaving] = useState(false);

  // A plain add/edit dialog for a plan slot - name and servings, no recipe search. That
  // matches what the web calendar's own slot dialog falls back to for anything that is not
  // a picked recipe, and keeps this screen from having to duplicate recipe search entirely.
  const [entryModalOpen, setEntryModalOpen] = useState(false);
  const [entryMealType, setEntryMealType] = useState<MealType>(MealType.Breakfast);
  const [entryName, setEntryName] = useState('');
  const [entryServings, setEntryServings] = useState('1');
  const [entryError, setEntryError] = useState<string | null>(null);
  /**
   * The recipe this slot is linked to. A meal the assistant planned has a whole recipe
   * written for it - method, ingredients, timings - and reading that is usually the reason
   * for opening the slot at all, so it is loaded rather than left as a name in a text box.
   *
   * `entryRecipeId` is kept apart from the loaded recipe so the link survives a save: the
   * request replaces the slot wholesale, and sending no recipe id would quietly unlink it.
   */
  const [entryRecipeId, setEntryRecipeId] = useState<string | undefined>(undefined);
  const [linkedRecipe, setLinkedRecipe] = useState<RecipeDto | null>(null);
  const [loadingRecipe, setLoadingRecipe] = useState(false);

  // `?.` on activePlan alone was not enough - `entries` does not exist on the payload, so
  // the optional chain resolved to undefined and .filter threw.
  const dayEntries = activePlan?.entriesByDate?.[date] ?? [];

  const getEntryForMeal = (mealType: MealType): MealPlanEntryDto | undefined =>
    dayEntries.find(e => e.mealType === mealType);

  const getLoggedForMeal = useCallback(
    (mealType: MealType): MealLogDto | undefined =>
      loggedLogs.find((l) => l.mealType === mealType && l.items.length > 0),
    [loggedLogs]
  );

  useEffect(() => {
    if (!user?.id) return;
    let cancelled = false;
    mealService
      .getDailyLogs(user.id, date)
      .then((logs) => { if (!cancelled) setLoggedLogs(logs); })
      .catch(() => { if (!cancelled) setLoggedLogs([]); });
    return () => { cancelled = true; };
  }, [user?.id, date]);

  const openEntryModal = (mealType: MealType) => {
    const entry = getEntryForMeal(mealType);
    setEntryMealType(mealType);
    setEntryName(entry ? entryLabel(entry) : '');
    setEntryServings(String(entry?.servings ?? 1));
    setEntryError(null);
    setEntryRecipeId(entry?.recipeId);
    setLinkedRecipe(null);
    clearError();
    setEntryModalOpen(true);

    if (entry?.recipeId) {
      setLoadingRecipe(true);
      recipeService
        .getRecipeById(entry.recipeId)
        // The slot still edits perfectly well without it, so a failed lookup just means
        // no recipe panel rather than a blocked dialog.
        .then((recipe) => setLinkedRecipe(recipe))
        .catch(() => setLinkedRecipe(null))
        .finally(() => setLoadingRecipe(false));
    }
  };

  const closeEntryModal = () => setEntryModalOpen(false);

  /** Renaming the meal means it is no longer the recipe that was linked to it. */
  const onEntryNameChange = (text: string) => {
    setEntryName(text);
    if (entryRecipeId && text.trim() !== (linkedRecipe?.name ?? '').trim()) {
      setEntryRecipeId(undefined);
      setLinkedRecipe(null);
    }
  };

  const openLinkedRecipe = () => {
    if (!entryRecipeId) return;
    setEntryModalOpen(false);
    // Crosses into the Recipes tab's own stack - see the note on openLoggedMeal below.
    navigation.getParent<any>()?.navigate('Recipes', {
      screen: 'RecipeDetail',
      params: { recipeId: entryRecipeId },
    });
  };

  const handleSaveEntry = async () => {
    if (!user?.id) return;
    const trimmed = entryName.trim();
    const servings = parseFloat(entryServings);

    if (!trimmed) {
      setEntryError('Give the meal a name.');
      return;
    }
    if (!(servings > 0)) {
      setEntryError('Servings must be greater than zero.');
      return;
    }

    setSaving(true);
    setEntryError(null);
    try {
      // A week with no plan yet has nothing to hang the entry on, so make one for it rather
      // than refusing the save - same fallback the web calendar uses.
      const plan = await ensurePlanFor(user.id, date, date, `Week of ${format(parsedDate, 'MMM d, yyyy')}`);
      const existing = getEntryForMeal(entryMealType);
      await saveEntry(user.id, plan.id, existing?.id ?? null, {
        entryDate: date,
        mealType: entryMealType,
        recipeId: entryRecipeId,
        // A linked recipe carries its own name; notes would only duplicate it.
        notes: entryRecipeId ? undefined : trimmed,
        servings,
      });
      setEntryModalOpen(false);
    } catch {
      // Read fresh rather than the `error` captured when this closure was created - the
      // store only sets it once the save actually fails, after this handler already started.
      setEntryError(useMealPlanStore.getState().error ?? 'That could not be saved. Please try again.');
      clearError();
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteEntry = (entry: MealPlanEntryDto) => {
    if (!user?.id || !activePlan) return;
    Alert.alert('Remove Meal', `Remove ${entryLabel(entry)} from this slot?`, [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Remove',
        style: 'destructive',
        onPress: async () => {
          try {
            await removeEntry(user.id, activePlan.id, entry.id);
          } catch {
            Alert.alert('Error', 'Could not remove that meal. Please try again.');
          }
        },
      },
    ]);
  };

  const openLoggedMeal = (log: MealLogDto) => {
    // Crosses from the Meal Plan tab's own stack into the Meal Log tab's - CalendarDay's own
    // navigation prop only knows MealPlanStackParamList, so this reaches up to the shared tab
    // navigator instead. The Meal Log tab is typed to accept these nested params (see
    // navigation/types.ts), so this is a real, if roundabout, navigation rather than a cast
    // papering over a mistake.
    navigation.getParent<any>()?.navigate('MealLog', { screen: 'AddMeal', params: { mealLog: log } });
  };

  const handleAISuggest = (mealType: MealType) => {
    Alert.alert('AI Suggestion', `AI will suggest a ${MEAL_TYPE_LABELS[mealType]} based on your goals.`, [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Get Suggestion', onPress: () => { /* TODO: call AI suggest */ } },
    ]);
  };

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()}>
          <Ionicons name="arrow-back" size={24} color={C.text} />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>{format(parsedDate, 'EEEE, MMMM d')}</Text>
        <View style={{ width: 24 }} />
      </View>

      <FlatList
        data={ORDERED_MEAL_TYPES}
        keyExtractor={item => item.toString()}
        contentContainerStyle={styles.list}
        renderItem={({ item: mealType }) => {
          const entry = getEntryForMeal(mealType);
          const log = entry ? undefined : getLoggedForMeal(mealType);
          return (
            <View style={styles.slotCard}>
              <View style={styles.slotHeader}>
                <View style={styles.slotTypeRow}>
                  <Ionicons name={MEAL_TYPE_ICONS[mealType]} size={18} color={C.primary} />
                  <Text style={styles.slotTypeName}>{MEAL_TYPE_LABELS[mealType]}</Text>
                </View>
              </View>

              {entry ? (
                <TouchableOpacity
                  style={styles.entryContent}
                  onPress={() => openEntryModal(mealType)}
                  accessibilityLabel={`Edit ${entryLabel(entry)}`}
                >
                  <View style={styles.entryInfo}>
                    <Text style={styles.entryName}>{entryLabel(entry)}</Text>
                    {entry.servings > 1 && (
                      <Text style={styles.entryServings}>{entry.servings} servings</Text>
                    )}
                    {entry.notes && entry.notes !== entryLabel(entry) && (
                      <Text style={styles.entryNotes}>{entry.notes}</Text>
                    )}
                  </View>
                  <TouchableOpacity
                    style={styles.deleteButton}
                    onPress={(e) => { e.stopPropagation(); handleDeleteEntry(entry); }}
                  >
                    <Ionicons name="trash-outline" size={18} color={C.error} />
                  </TouchableOpacity>
                </TouchableOpacity>
              ) : log ? (
                <TouchableOpacity
                  style={styles.entryContent}
                  onPress={() => openLoggedMeal(log)}
                  accessibilityLabel={`Edit logged ${loggedLabel(log)}`}
                >
                  <View style={styles.entryInfo}>
                    <Text style={styles.entryName}>{loggedLabel(log)}</Text>
                    <Text style={styles.entryLoggedTag}>Logged as eaten - tap to edit</Text>
                  </View>
                  <Ionicons name="restaurant" size={18} color={C.textSecondary} />
                </TouchableOpacity>
              ) : (
                <View style={styles.emptySlot}>
                  <TouchableOpacity style={styles.addButton} onPress={() => openEntryModal(mealType)}>
                    <Ionicons name="add" size={16} color={C.primary} />
                    <Text style={styles.addButtonText}>Add Meal</Text>
                  </TouchableOpacity>
                  <TouchableOpacity style={styles.aiButton} onPress={() => handleAISuggest(mealType)}>
                    <Ionicons name="sparkles" size={16} color={C.secondary} />
                    <Text style={styles.aiButtonText}>Ask AI</Text>
                  </TouchableOpacity>
                </View>
              )}
            </View>
          );
        }}
      />

      <ModalSheet
        visible={entryModalOpen}
        onClose={closeEntryModal}
        title={`${getEntryForMeal(entryMealType) ? 'Edit' : 'Add'} ${MEAL_TYPE_LABELS[entryMealType]}`}
        // A save is in flight; a stray tap on the backdrop should not throw it away.
        dismissOnBackdrop={!saving}
        footer={
          <ModalActions
            confirmLabel="Save"
            onConfirm={handleSaveEntry}
            onCancel={closeEntryModal}
            busy={saving}
          />
        }
      >
        <Text style={styles.modalLabel}>Meal</Text>
        <TextInput
          style={styles.modalInput}
          value={entryName}
          onChangeText={onEntryNameChange}
          placeholder="e.g. Grilled chicken salad"
          placeholderTextColor={C.textSecondary}
          accessibilityLabel="Meal"
        />

        {loadingRecipe ? <ActivityIndicator color={C.primary} style={styles.recipeLoading} /> : null}

        {!loadingRecipe && linkedRecipe ? (
          <LinkedRecipeCard recipe={linkedRecipe} onOpen={openLinkedRecipe} />
        ) : null}

        <Text style={styles.modalLabel}>Servings</Text>
        <TextInput
          style={styles.modalInputSmall}
          value={entryServings}
          onChangeText={setEntryServings}
          keyboardType="decimal-pad"
          accessibilityLabel="Servings"
        />

        {entryError ? <Text style={styles.modalError}>{entryError}</Text> : null}
      </ModalSheet>
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: { flex: 1, backgroundColor: C.background },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: Spacing.lg,
    paddingVertical: Spacing.md,
    backgroundColor: C.surface,
    borderBottomWidth: 1,
    borderBottomColor: C.divider,
  },
  headerTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text },
  list: { padding: Spacing.md, gap: Spacing.md },
  slotCard: {
    backgroundColor: C.surface,
    borderRadius: BorderRadius.lg,
    overflow: 'hidden',
    ...Shadows.sm,
  },
  slotHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: Spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: C.divider,
  },
  slotTypeRow: { flexDirection: 'row', alignItems: 'center', gap: Spacing.sm },
  slotTypeName: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
  entryContent: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: Spacing.md,
  },
  entryInfo: { flex: 1 },
  entryName: { fontSize: FontSize.md, fontWeight: FontWeight.medium, color: C.text },
  entryServings: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2 },
  entryNotes: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2, fontStyle: 'italic' },
  entryLoggedTag: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2, fontStyle: 'italic' },
  deleteButton: { padding: Spacing.sm },
  emptySlot: { flexDirection: 'row', gap: Spacing.sm, padding: Spacing.md },
  addButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    borderWidth: 1,
    borderColor: C.primary,
    borderRadius: BorderRadius.md,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
  },
  addButtonText: { color: C.primary, fontSize: FontSize.sm, fontWeight: FontWeight.medium },
  aiButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    borderWidth: 1,
    borderColor: C.secondary,
    borderRadius: BorderRadius.md,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
  },
  aiButtonText: { color: C.secondary, fontSize: FontSize.sm, fontWeight: FontWeight.medium },
  modalLabel: { fontSize: FontSize.sm, fontWeight: FontWeight.semibold, color: C.text, marginTop: Spacing.sm },
  modalInput: {
    borderWidth: 1,
    borderColor: C.divider,
    borderRadius: BorderRadius.md,
    paddingHorizontal: Spacing.sm,
    paddingVertical: Spacing.sm,
    fontSize: FontSize.sm,
    color: C.text,
    backgroundColor: C.background,
  },
  modalInputSmall: {
    width: 100,
    borderWidth: 1,
    borderColor: C.divider,
    borderRadius: BorderRadius.md,
    paddingHorizontal: Spacing.sm,
    paddingVertical: Spacing.sm,
    fontSize: FontSize.sm,
    color: C.text,
    backgroundColor: C.background,
  },
  modalError: { fontSize: FontSize.sm, color: C.error, marginTop: Spacing.sm },
  recipeLoading: { marginTop: Spacing.md },
  recipeCard: {
    marginTop: Spacing.md,
    borderWidth: 1,
    borderColor: C.divider,
    borderRadius: BorderRadius.md,
    overflow: 'hidden',
  },
  recipeRow: { flexDirection: 'row', gap: Spacing.sm, padding: Spacing.sm },
  recipeThumb: {
    width: 56,
    height: 56,
    borderRadius: BorderRadius.sm,
    overflow: 'hidden',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: C.background,
  },
  recipeThumbImage: { width: '100%', height: '100%' },
  recipeInfo: { flex: 1 },
  recipeName: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
  recipeDesc: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 2 },
  recipeMeta: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 4 },
  recipeLink: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.xs,
    paddingVertical: Spacing.sm,
    borderTopWidth: 1,
    borderTopColor: C.divider,
  },
  recipeLinkText: { color: C.primary, fontSize: FontSize.sm, fontWeight: FontWeight.semibold },
});
