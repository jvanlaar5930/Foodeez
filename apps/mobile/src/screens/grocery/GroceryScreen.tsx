import React, { useEffect, useMemo, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Modal,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { addDays, format, startOfWeek } from 'date-fns';
import { useGroceryStore } from '@/store/groceryStore';
import { formatApiDate, parseApiDate } from '@/utils/dateUtils';
import { GROCERY_CATEGORIES, type GroceryItemDto } from '@/types';
import { BorderRadius, FontSize, FontWeight, Shadows, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

type RangeMode = 'day' | 'week';

/** The dates a mode covers, from any day inside it. Weeks run Monday to Sunday. */
function rangeFrom(date: Date, mode: RangeMode): { startDate: string; endDate: string } {
  if (mode === 'day') {
    const day = formatApiDate(date);
    return { startDate: day, endDate: day };
  }

  const monday = startOfWeek(date, { weekStartsOn: 1 });
  return { startDate: formatApiDate(monday), endDate: formatApiDate(addDays(monday, 6)) };
}

export function GroceryScreen() {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  // A week is what people shop for, so that is where the tab opens.
  const [mode, setMode] = useState<RangeMode>('week');
  const [range, setRange] = useState(() => rangeFrom(new Date(), 'week'));
  const [newItem, setNewItem] = useState('');
  const [newQuantity, setNewQuantity] = useState('');
  /** The line being swapped, if any. Held whole so cancelling leaves the list untouched. */
  const [editing, setEditing] = useState<GroceryItemDto | null>(null);
  const [editName, setEditName] = useState('');
  const [editQuantity, setEditQuantity] = useState('');

  const {
    list,
    plannedMealCount,
    isLoading,
    isGenerating,
    generationText,
    error,
    load,
    generate,
    cancelGenerate,
    addItem,
    updateItem,
    setChecked,
    removeItem,
  } = useGroceryStore();

  useEffect(() => {
    void load(range.startDate, range.endDate);
  }, [range.startDate, range.endDate]);

  /** Aisle order, so the list is walked rather than hunted through. */
  const groups = useMemo(() => {
    const items = list?.items ?? [];
    return GROCERY_CATEGORIES.map((category) => ({
      category,
      items: items.filter((item) => item.category === category),
    })).filter((group) => group.items.length > 0);
  }, [list]);

  const remaining = (list?.items ?? []).filter((item) => !item.isChecked).length;
  const total = list?.items.length ?? 0;

  const shift = (direction: number) => {
    const step = mode === 'week' ? 7 : 1;
    setRange(rangeFrom(addDays(parseApiDate(range.startDate), direction * step), mode));
  };

  const switchMode = (next: RangeMode) => {
    if (next === mode) return;
    setMode(next);
    // Keep the day they were looking at: a week means the week containing it.
    setRange(rangeFrom(parseApiDate(range.startDate), next));
  };

  const onAdd = () => {
    const name = newItem.trim();
    if (name.length === 0) return;

    void addItem({ name, quantity: newQuantity.trim(), category: 'Other' });
    setNewItem('');
    setNewQuantity('');
  };

  const startSwap = (item: GroceryItemDto) => {
    setEditing(item);
    setEditName(item.name);
    setEditQuantity(item.quantity);
  };

  const saveSwap = () => {
    const name = editName.trim();
    if (!editing || name.length === 0) return;

    void updateItem(editing.id, {
      name,
      quantity: editQuantity.trim(),
      category: editing.category,
      isChecked: editing.isChecked,
    });
    setEditing(null);
  };

  const confirmRemove = (item: GroceryItemDto) => {
    Alert.alert('Remove item', `Remove "${item.name}" from the list?`, [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Remove', style: 'destructive', onPress: () => void removeItem(item.id) },
    ]);
  };

  const rangeLabel =
    range.startDate === range.endDate
      ? format(parseApiDate(range.startDate), 'EEE d MMM')
      : `${format(parseApiDate(range.startDate), 'EEE d')} - ${format(parseApiDate(range.endDate), 'EEE d MMM')}`;

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.title}>Grocery list</Text>
        <Text style={styles.subtitle}>Everything your planned meals need, in one shop</Text>

        <View style={styles.rangeCard}>
          <View style={styles.rangeRow}>
            <TouchableOpacity onPress={() => shift(-1)} accessibilityLabel="Previous">
              <Ionicons name="chevron-back" size={22} color={C.textSecondary} />
            </TouchableOpacity>
            <Text style={styles.rangeLabel}>{rangeLabel}</Text>
            <TouchableOpacity onPress={() => shift(1)} accessibilityLabel="Next">
              <Ionicons name="chevron-forward" size={22} color={C.textSecondary} />
            </TouchableOpacity>
          </View>

          <View style={styles.modeSwitch}>
            {(['day', 'week'] as RangeMode[]).map((option) => (
              <TouchableOpacity
                key={option}
                style={[styles.modeButton, option === mode && styles.modeButtonActive]}
                onPress={() => switchMode(option)}
              >
                <Text style={[styles.modeText, option === mode && styles.modeTextActive]}>
                  {option === 'day' ? 'Day' : 'Week'}
                </Text>
              </TouchableOpacity>
            ))}
          </View>
        </View>

        {isGenerating ? (
          <View style={styles.card}>
            <View style={styles.cardHeader}>
              <Text style={styles.cardLabel}>WORKING OUT THE SHOP</Text>
              <TouchableOpacity onPress={cancelGenerate}>
                <Text style={styles.linkText}>Stop</Text>
              </TouchableOpacity>
            </View>
            <Text style={styles.streamText}>
              {generationText.length > 0 ? generationText : 'Reading your planned meals...'}
            </Text>
          </View>
        ) : isLoading ? (
          <ActivityIndicator color={C.primary} style={styles.loader} />
        ) : plannedMealCount === 0 ? (
          // Nothing planned: a shopping list cannot be conjured from an empty calendar.
          <View style={styles.emptyCard}>
            <Text style={styles.emptyEmoji}>🛒</Text>
            <Text style={styles.emptyTitle}>No meals planned for these dates</Text>
            <Text style={styles.emptyText}>
              Plan some meals on the calendar - or ask for a few in the Ask AI tab - and the list
              will follow.
            </Text>
          </View>
        ) : !list ? (
          <View style={styles.emptyCard}>
            <Text style={styles.emptyEmoji}>🛒</Text>
            <Text style={styles.emptyTitle}>
              {plannedMealCount} {plannedMealCount === 1 ? 'meal' : 'meals'} planned
            </Text>
            <Text style={styles.emptyText}>
              Pull them together into one list, with the amounts added up and the aisles in order.
            </Text>
            <TouchableOpacity
              style={styles.primaryButton}
              onPress={() => void generate(range.startDate, range.endDate)}
            >
              <Text style={styles.primaryButtonText}>Build my list</Text>
            </TouchableOpacity>
          </View>
        ) : (
          <>
            <View style={styles.progressRow}>
              <Text style={styles.progressText}>
                {total - remaining} of {total} in the trolley
              </Text>
              <TouchableOpacity onPress={() => void generate(range.startDate, range.endDate, true)}>
                <Text style={styles.linkText}>Rebuild</Text>
              </TouchableOpacity>
            </View>

            {/* The plan moved on. The list is not replaced: it may be half shopped. */}
            {list.isStale ? (
              <TouchableOpacity
                style={styles.staleBanner}
                onPress={() => void generate(range.startDate, range.endDate, true)}
              >
                <Text style={styles.staleText}>
                  Your meal plan has changed since this list was made. Tap to rebuild it.
                </Text>
              </TouchableOpacity>
            ) : null}

            {groups.map((group) => (
              <View key={group.category} style={styles.card}>
                <Text style={styles.cardLabel}>{group.category.toUpperCase()}</Text>

                {group.items.map((item) => (
                  <View key={item.id} style={styles.itemRow}>
                    <TouchableOpacity
                      style={styles.checkbox}
                      onPress={() => void setChecked(item.id, !item.isChecked)}
                      accessibilityLabel={`Got ${item.name}`}
                    >
                      <Ionicons
                        name={item.isChecked ? 'checkbox' : 'square-outline'}
                        size={22}
                        color={item.isChecked ? C.primary : C.textHint}
                      />
                    </TouchableOpacity>

                    {/* Tapping the line is the swap: rename it, change the amount. */}
                    <TouchableOpacity
                      style={styles.itemText}
                      onPress={() => startSwap(item)}
                      accessibilityLabel={`Swap or edit ${item.name}`}
                    >
                      <Text style={[styles.itemName, item.isChecked && styles.itemNameChecked]}>
                        {item.name}
                        {item.quantity ? (
                          <Text style={styles.itemQuantity}> · {item.quantity}</Text>
                        ) : null}
                      </Text>
                      {item.source ? (
                        <Text style={styles.itemSource} numberOfLines={1}>
                          {item.source}
                        </Text>
                      ) : null}
                    </TouchableOpacity>

                    <TouchableOpacity
                      onPress={() => confirmRemove(item)}
                      accessibilityLabel={`Remove ${item.name}`}
                    >
                      <Ionicons name="close" size={18} color={C.textHint} />
                    </TouchableOpacity>
                  </View>
                ))}
              </View>
            ))}

            <View style={styles.card}>
              <View style={styles.addRow}>
                <TextInput
                  style={styles.addInput}
                  value={newItem}
                  onChangeText={setNewItem}
                  placeholder="Add something else..."
                  placeholderTextColor={C.textHint}
                  onSubmitEditing={onAdd}
                />
                <TextInput
                  style={styles.addQuantity}
                  value={newQuantity}
                  onChangeText={setNewQuantity}
                  placeholder="Amount"
                  placeholderTextColor={C.textHint}
                  onSubmitEditing={onAdd}
                />
                <TouchableOpacity
                  style={[styles.addButton, newItem.trim().length === 0 && styles.addDisabled]}
                  disabled={newItem.trim().length === 0}
                  onPress={onAdd}
                >
                  <Ionicons name="add" size={20} color={C.onPrimary} />
                </TouchableOpacity>
              </View>
              <Text style={styles.hint}>
                Anything you add or change by hand is kept when the list is rebuilt.
              </Text>
            </View>
          </>
        )}

        {error ? <Text style={styles.error}>{error}</Text> : null}
      </ScrollView>

      <Modal
        visible={editing !== null}
        animationType="slide"
        transparent
        onRequestClose={() => setEditing(null)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>Swap this item</Text>
            <Text style={styles.emptyText}>
              Change it for whatever the shop actually had. Edited items are kept when the list
              is rebuilt.
            </Text>

            <TextInput
              style={styles.addInput}
              value={editName}
              onChangeText={setEditName}
              placeholder="Item"
              placeholderTextColor={C.textHint}
            />
            <TextInput
              style={styles.addInput}
              value={editQuantity}
              onChangeText={setEditQuantity}
              placeholder="Amount"
              placeholderTextColor={C.textHint}
            />

            <View style={styles.modalActions}>
              <TouchableOpacity style={styles.modalCancel} onPress={() => setEditing(null)}>
                <Text style={styles.modalCancelText}>Cancel</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.modalConfirm, editName.trim().length === 0 && styles.addDisabled]}
                disabled={editName.trim().length === 0}
                onPress={saveSwap}
              >
                <Text style={styles.modalConfirmText}>Save</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    container: { flex: 1, backgroundColor: C.background },
    content: { padding: Spacing.md, gap: Spacing.md, paddingBottom: Spacing.xxl },
    title: { fontSize: FontSize.xxl, fontWeight: FontWeight.bold, color: C.text },
    subtitle: { fontSize: FontSize.md, color: C.textSecondary, marginTop: -Spacing.sm },
    rangeCard: {
      backgroundColor: C.surface,
      borderRadius: BorderRadius.lg,
      padding: Spacing.md,
      gap: Spacing.md,
      ...Shadows.sm,
    },
    rangeRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
    rangeLabel: { fontSize: FontSize.lg, fontWeight: FontWeight.semibold, color: C.text },
    modeSwitch: {
      flexDirection: 'row',
      backgroundColor: C.surfaceAlt,
      borderRadius: BorderRadius.lg,
      padding: 2,
    },
    modeButton: {
      flex: 1,
      paddingVertical: Spacing.sm,
      borderRadius: BorderRadius.md,
      alignItems: 'center',
    },
    modeButtonActive: { backgroundColor: C.surface, ...Shadows.sm },
    modeText: { fontSize: FontSize.md, color: C.textSecondary, fontWeight: FontWeight.semibold },
    modeTextActive: { color: C.primary },
    card: {
      backgroundColor: C.surface,
      borderRadius: BorderRadius.lg,
      padding: Spacing.md,
      ...Shadows.sm,
    },
    cardHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
    cardLabel: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.bold,
      color: C.textSecondary,
      letterSpacing: 0.5,
      marginBottom: Spacing.xs,
    },
    streamText: { fontSize: FontSize.md, color: C.text, lineHeight: 21 },
    loader: { marginTop: Spacing.xl },
    emptyCard: {
      backgroundColor: C.surface,
      borderRadius: BorderRadius.lg,
      padding: Spacing.xl,
      alignItems: 'center',
      gap: Spacing.sm,
      ...Shadows.sm,
    },
    emptyEmoji: { fontSize: 36 },
    emptyTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text },
    emptyText: {
      fontSize: FontSize.md,
      color: C.textSecondary,
      textAlign: 'center',
      lineHeight: 20,
    },
    primaryButton: {
      marginTop: Spacing.sm,
      backgroundColor: C.primary,
      borderRadius: BorderRadius.lg,
      paddingHorizontal: Spacing.xl,
      paddingVertical: Spacing.md,
    },
    primaryButtonText: { color: C.onPrimary, fontWeight: FontWeight.semibold, fontSize: FontSize.md },
    progressRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
    progressText: { fontSize: FontSize.md, color: C.textSecondary },
    linkText: { fontSize: FontSize.md, color: C.primary, fontWeight: FontWeight.semibold },
    staleBanner: {
      backgroundColor: C.secondary,
      borderRadius: BorderRadius.lg,
      padding: Spacing.md,
    },
    staleText: { color: '#3B2A00', fontSize: FontSize.md },
    itemRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: Spacing.sm,
      paddingVertical: Spacing.sm,
      borderBottomWidth: StyleSheet.hairlineWidth,
      borderBottomColor: C.divider,
    },
    checkbox: { padding: 2 },
    itemText: { flex: 1 },
    itemName: { fontSize: FontSize.md, color: C.text, fontWeight: FontWeight.medium },
    itemNameChecked: { color: C.textHint, textDecorationLine: 'line-through' },
    itemQuantity: { color: C.textSecondary, fontWeight: FontWeight.regular },
    itemSource: { fontSize: FontSize.sm, color: C.textHint, marginTop: 2 },
    addRow: { flexDirection: 'row', alignItems: 'center', gap: Spacing.sm },
    addInput: {
      flex: 1,
      borderWidth: 1,
      borderColor: C.divider,
      borderRadius: BorderRadius.lg,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.sm,
      fontSize: FontSize.md,
      color: C.text,
      backgroundColor: C.background,
    },
    addQuantity: {
      width: 88,
      borderWidth: 1,
      borderColor: C.divider,
      borderRadius: BorderRadius.lg,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.sm,
      fontSize: FontSize.md,
      color: C.text,
      backgroundColor: C.background,
    },
    addButton: {
      width: 40,
      height: 40,
      borderRadius: BorderRadius.full,
      backgroundColor: C.primary,
      alignItems: 'center',
      justifyContent: 'center',
    },
    addDisabled: { opacity: 0.4 },
    hint: { marginTop: Spacing.sm, fontSize: FontSize.sm, color: C.textHint },
    error: { color: C.error, fontSize: FontSize.md },
    modalOverlay: {
      flex: 1,
      backgroundColor: C.overlay,
      justifyContent: 'center',
      padding: Spacing.xl,
    },
    modalContent: {
      backgroundColor: C.surface,
      borderRadius: BorderRadius.xl,
      padding: Spacing.xl,
      gap: Spacing.md,
    },
    modalTitle: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: C.text },
    modalActions: { flexDirection: 'row', gap: Spacing.md, marginTop: Spacing.sm },
    modalCancel: {
      flex: 1,
      borderWidth: 2,
      borderColor: C.divider,
      borderRadius: BorderRadius.lg,
      paddingVertical: Spacing.md,
      alignItems: 'center',
    },
    modalCancelText: { color: C.textSecondary, fontWeight: FontWeight.semibold },
    modalConfirm: {
      flex: 1,
      backgroundColor: C.primary,
      borderRadius: BorderRadius.lg,
      paddingVertical: Spacing.md,
      alignItems: 'center',
    },
    modalConfirmText: { color: C.onPrimary, fontWeight: FontWeight.semibold },
  });
