import React, { useState } from 'react';
import {
  KeyboardAvoidingView,
  Modal,
  Platform,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useThemedStyles, type Palette } from '@/theme';

interface Props {
  visible: boolean;
  /** How many items are about to be saved, so the user can see they picked the right meal. */
  itemCount: number;
  isSaving: boolean;
  error: string | null;
  onClose: () => void;
  onSave: (name: string) => void;
}

/**
 * Names a meal so it can be logged again in one tap.
 *
 * A modal rather than Alert.prompt, which only exists on iOS - on Android that call does
 * nothing at all, so the button would have looked broken to half the users.
 */
export function SaveMealModal({ visible, itemCount, isSaving, error, onClose, onSave }: Props) {
  const styles = useThemedStyles(makeStyles);
  const [name, setName] = useState('');

  const close = () => {
    setName('');
    onClose();
  };

  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={close}>
      <KeyboardAvoidingView
        style={styles.backdrop}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <View style={styles.sheet}>
          <Text style={styles.title}>Save this meal</Text>
          <Text style={styles.subtitle}>
            {itemCount} item{itemCount === 1 ? '' : 's'}. Name it and it will be one tap away next
            time.
          </Text>

          <Input
            value={name}
            onChangeText={setName}
            placeholder="e.g. My turkey sandwich"
            maxLength={100}
            autoFocus
            returnKeyType="done"
            onSubmitEditing={() => name.trim() && onSave(name.trim())}
          />

          {error && <Text style={styles.error}>{error}</Text>}

          <View style={styles.actions}>
            <TouchableOpacity style={styles.cancel} onPress={close} disabled={isSaving}>
              <Text style={styles.cancelText}>Cancel</Text>
            </TouchableOpacity>
            <Button
              title="Save"
              loading={isSaving}
              disabled={!name.trim()}
              onPress={() => onSave(name.trim())}
            />
          </View>
        </View>
      </KeyboardAvoidingView>
    </Modal>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: C.overlay,
    justifyContent: 'center',
    padding: Spacing.lg,
  },
  sheet: {
    backgroundColor: C.surface,
    borderRadius: BorderRadius.lg,
    padding: Spacing.lg,
    gap: Spacing.sm,
  },
  title: {
    fontSize: FontSize.lg,
    fontWeight: FontWeight.bold,
    color: C.text,
  },
  subtitle: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
  },
  error: {
    fontSize: FontSize.sm,
    color: C.error,
  },
  actions: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'flex-end',
    gap: Spacing.md,
  },
  cancel: {
    paddingVertical: Spacing.sm,
    paddingHorizontal: Spacing.sm,
  },
  cancelText: {
    fontSize: FontSize.md,
    color: C.textSecondary,
  },
});
