import React from 'react';
import {
  KeyboardAvoidingView,
  Modal,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  TouchableWithoutFeedback,
  View,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { BorderRadius, FontSize, FontWeight, Shadows, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

interface ModalSheetProps {
  visible: boolean;
  onClose: () => void;
  title?: string;
  /**
   * `center` is a dialog floating in the middle - a confirm, a short form.
   * `bottom` is a sheet rising from the bottom edge, for anything longer or scrollable.
   */
  variant?: 'center' | 'bottom';
  /** Whether tapping the dimmed area behind closes it. Off for anything mid-save. */
  dismissOnBackdrop?: boolean;
  children: React.ReactNode;
  /** Rendered under the body, outside the scroll area, so buttons stay reachable. */
  footer?: React.ReactNode;
}

/**
 * The dialog every screen was rebuilding.
 *
 * Seven modals existed with their own overlay, panel, title row and close button, and five of
 * the overlay styles were character-identical. Beyond the repetition they had drifted in the
 * parts that are easy to leave out: some closed on a backdrop tap and some did not, only two
 * avoided the keyboard, and the Android back button dismissed some of them and not others.
 */
export function ModalSheet({
  visible,
  onClose,
  title,
  variant = 'center',
  dismissOnBackdrop = true,
  children,
  footer,
}: ModalSheetProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  return (
    <Modal
      visible={visible}
      transparent
      animationType={variant === 'bottom' ? 'slide' : 'fade'}
      // Android's hardware back button. Without this it dismisses the whole screen behind
      // the dialog, which is not what "back" means with a dialog open.
      onRequestClose={onClose}
    >
      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <TouchableWithoutFeedback onPress={dismissOnBackdrop ? onClose : undefined}>
          <View style={[styles.overlay, variant === 'bottom' && styles.overlayBottom]}>
            {/* Swallows taps on the panel itself so they do not reach the backdrop. */}
            <TouchableWithoutFeedback onPress={() => {}}>
              <View style={[styles.panel, variant === 'bottom' && styles.panelBottom]}>
                {title ? (
                  <View style={styles.header}>
                    <Text style={styles.title}>{title}</Text>
                    <TouchableOpacity
                      onPress={onClose}
                      accessibilityRole="button"
                      accessibilityLabel="Close"
                      hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                    >
                      <Ionicons name="close" size={22} color={C.textSecondary} />
                    </TouchableOpacity>
                  </View>
                ) : null}

                <ScrollView
                  style={styles.body}
                  contentContainerStyle={styles.bodyContent}
                  keyboardShouldPersistTaps="handled"
                >
                  {children}
                </ScrollView>

                {footer ? <View style={styles.footer}>{footer}</View> : null}
              </View>
            </TouchableWithoutFeedback>
          </View>
        </TouchableWithoutFeedback>
      </KeyboardAvoidingView>
    </Modal>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    flex: { flex: 1 },
    overlay: {
      flex: 1,
      backgroundColor: C.overlay,
      justifyContent: 'center',
      alignItems: 'center',
      padding: Spacing.xl,
    },
    overlayBottom: { justifyContent: 'flex-end', padding: 0 },
    panel: {
      width: '100%',
      maxHeight: '85%',
      backgroundColor: C.surface,
      borderRadius: BorderRadius.lg,
      padding: Spacing.lg,
      ...Shadows.md,
    },
    panelBottom: {
      maxHeight: '90%',
      borderBottomLeftRadius: 0,
      borderBottomRightRadius: 0,
      paddingBottom: Spacing.xl,
    },
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: Spacing.md,
    },
    title: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text, flex: 1 },
    // Grows to fit its content rather than filling the panel, so a short dialog stays short.
    body: { flexGrow: 0 },
    bodyContent: { gap: Spacing.sm },
    footer: { marginTop: Spacing.lg },
  });
