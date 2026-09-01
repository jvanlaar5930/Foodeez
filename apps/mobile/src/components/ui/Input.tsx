import React, { useState } from 'react';
import {
  StyleSheet,
  Text,
  TextInput,
  View,
  type TextInputProps,
} from 'react-native';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

interface InputProps extends TextInputProps {
  label?: string;
  error?: string;
  hint?: string;
}

export function Input({ label, error, hint, style, ...props }: InputProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const [isFocused, setIsFocused] = useState(false);

  return (
    <View style={styles.container}>
      {label && <Text style={styles.label}>{label}</Text>}
      <TextInput
        style={[
          styles.input,
          isFocused && styles.inputFocused,
          error ? styles.inputError : null,
          style,
        ]}
        placeholderTextColor={C.textHint}
        onFocus={() => setIsFocused(true)}
        onBlur={() => setIsFocused(false)}
        {...props}
      />
      {error ? (
        <Text style={styles.error}>{error}</Text>
      ) : hint ? (
        <Text style={styles.hint}>{hint}</Text>
      ) : null}
    </View>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: {
    marginBottom: Spacing.md,
  },
  label: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.medium,
    color: C.text,
    marginBottom: Spacing.xs,
  },
  input: {
    height: 48,
    borderWidth: 1.5,
    borderColor: C.divider,
    borderRadius: BorderRadius.md,
    paddingHorizontal: Spacing.md,
    fontSize: FontSize.md,
    color: C.text,
    backgroundColor: C.surface,
  },
  inputFocused: {
    borderColor: C.primary,
  },
  inputError: {
    borderColor: C.error,
  },
  error: {
    fontSize: FontSize.sm,
    color: C.error,
    marginTop: Spacing.xs,
  },
  hint: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
    marginTop: Spacing.xs,
  },
});
