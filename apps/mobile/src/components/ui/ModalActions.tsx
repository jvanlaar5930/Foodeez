import React from 'react';
import { ActivityIndicator, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { BorderRadius, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

interface ModalActionsProps {
  confirmLabel: string;
  onConfirm: () => void;
  cancelLabel?: string;
  onCancel: () => void;
  /** Shows a spinner in place of the confirm label, and blocks both buttons. */
  busy?: boolean;
  /** Blocks confirm without implying anything is in flight - an incomplete form. */
  confirmDisabled?: boolean;
  /** Paints confirm in the error colour, for a delete or a discard. */
  destructive?: boolean;
}

/**
 * The cancel/confirm pair at the foot of a dialog.
 *
 * Written out in seven modals, and only some of them disabled the buttons while a save was in
 * flight - so a double tap could send the same request twice.
 */
export function ModalActions({
  confirmLabel,
  onConfirm,
  cancelLabel = 'Cancel',
  onCancel,
  busy = false,
  confirmDisabled = false,
  destructive = false,
}: ModalActionsProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const blocked = busy || confirmDisabled;

  return (
    <View style={styles.row}>
      <TouchableOpacity
        style={[styles.cancel, busy && styles.disabled]}
        onPress={onCancel}
        disabled={busy}
        accessibilityRole="button"
      >
        <Text style={styles.cancelText}>{cancelLabel}</Text>
      </TouchableOpacity>

      <TouchableOpacity
        style={[styles.confirm, destructive && styles.confirmDestructive, blocked && styles.disabled]}
        onPress={onConfirm}
        disabled={blocked}
        accessibilityRole="button"
      >
        {busy ? (
          <ActivityIndicator size="small" color={C.onPrimary} />
        ) : (
          <Text style={styles.confirmText}>{confirmLabel}</Text>
        )}
      </TouchableOpacity>
    </View>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    row: { flexDirection: 'row', gap: Spacing.md },
    cancel: {
      flex: 1,
      borderWidth: 2,
      borderColor: C.divider,
      borderRadius: BorderRadius.lg,
      paddingVertical: Spacing.md,
      alignItems: 'center',
    },
    cancelText: { color: C.textSecondary, fontWeight: FontWeight.semibold },
    confirm: {
      flex: 1,
      backgroundColor: C.primary,
      borderRadius: BorderRadius.lg,
      paddingVertical: Spacing.md,
      alignItems: 'center',
      justifyContent: 'center',
    },
    confirmDestructive: { backgroundColor: C.error },
    confirmText: { color: C.onPrimary, fontWeight: FontWeight.semibold },
    disabled: { opacity: 0.5 },
  });
