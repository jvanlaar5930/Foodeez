import React from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { FontSize, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

interface LoadingSpinnerProps {
  message?: string;
  size?: 'small' | 'large';
  color?: string;
}

export function LoadingSpinner({ message, size = 'large', color }: LoadingSpinnerProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const spinnerColor = color ?? C.primary;
  return (
    <View style={styles.container}>
      <ActivityIndicator size={size} color={spinnerColor} />
      {message && <Text style={styles.message}>{message}</Text>}
    </View>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: Spacing.xl,
  },
  message: {
    marginTop: Spacing.md,
    fontSize: FontSize.md,
    color: C.textSecondary,
    textAlign: 'center',
  },
});
