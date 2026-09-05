import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { BorderRadius, FontSize, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

interface ErrorBannerProps {
  /** Nothing renders when this is null, so a caller can pass a store's error straight in. */
  message: string | null | undefined;
  /** Shows a dismiss button. Usually a store's clearError. */
  onDismiss?: () => void;
}

/**
 * A failure worth reading, in the same shape everywhere.
 *
 * Every screen drew its own, and several drew none at all - the store recorded an error that
 * nothing on screen ever showed.
 */
export function ErrorBanner({ message, onDismiss }: ErrorBannerProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  if (!message) return null;

  return (
    <View style={styles.banner} accessibilityRole="alert">
      <Ionicons name="alert-circle-outline" size={18} color={C.error} />
      <Text style={styles.text}>{message}</Text>
      {onDismiss ? (
        <TouchableOpacity
          onPress={onDismiss}
          accessibilityRole="button"
          accessibilityLabel="Dismiss"
          hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
        >
          <Ionicons name="close" size={16} color={C.error} />
        </TouchableOpacity>
      ) : null}
    </View>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    banner: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: Spacing.sm,
      marginHorizontal: Spacing.md,
      marginBottom: Spacing.sm,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.sm,
      borderRadius: BorderRadius.md,
      borderWidth: 1,
      borderColor: C.error,
      backgroundColor: C.surface,
    },
    text: { flex: 1, color: C.error, fontSize: FontSize.sm },
  });
