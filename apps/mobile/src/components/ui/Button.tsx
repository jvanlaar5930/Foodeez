import React from 'react';
import {
  ActivityIndicator,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
  type TouchableOpacityProps,
} from 'react-native';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
type ButtonSize = 'sm' | 'md' | 'lg';

interface ButtonProps extends Omit<TouchableOpacityProps, 'style'> {
  title: string;
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  leftIcon?: React.ReactNode;
  fullWidth?: boolean;
}

export function Button({
  title,
  variant = 'primary',
  size = 'md',
  loading = false,
  disabled = false,
  leftIcon,
  fullWidth = false,
  onPress,
  ...props
}: ButtonProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const isDisabled = disabled || loading;

  return (
    <TouchableOpacity
      style={[
        styles.base,
        styles[variant],
        styles[size],
        fullWidth && styles.fullWidth,
        isDisabled && styles.disabled,
      ]}
      onPress={onPress}
      disabled={isDisabled}
      activeOpacity={0.8}
      {...props}
    >
      {loading ? (
        <ActivityIndicator
          size="small"
          color={variant === 'outline' || variant === 'ghost' ? C.primary : C.surface}
        />
      ) : (
        <View style={styles.content}>
          {leftIcon && <View style={styles.iconWrapper}>{leftIcon}</View>}
          <Text style={[styles.text, styles[`${variant}Text`], styles[`${size}Text`]]}>
            {title}
          </Text>
        </View>
      )}
    </TouchableOpacity>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  base: {
    borderRadius: BorderRadius.md,
    alignItems: 'center',
    justifyContent: 'center',
    flexDirection: 'row',
  },
  content: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
  },
  iconWrapper: {
    marginRight: Spacing.xs,
  },
  fullWidth: {
    width: '100%',
  },
  disabled: {
    opacity: 0.5,
  },

  // Variants
  primary: {
    backgroundColor: C.primary,
  },
  secondary: {
    backgroundColor: C.secondary,
  },
  outline: {
    backgroundColor: 'transparent',
    borderWidth: 1.5,
    borderColor: C.primary,
  },
  ghost: {
    backgroundColor: 'transparent',
  },
  danger: {
    backgroundColor: C.error,
  },

  // Sizes
  sm: {
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
    minHeight: 36,
  },
  md: {
    paddingHorizontal: Spacing.lg,
    paddingVertical: Spacing.sm + 2,
    minHeight: 48,
  },
  lg: {
    paddingHorizontal: Spacing.xl,
    paddingVertical: Spacing.md,
    minHeight: 56,
  },

  // Text styles
  text: {
    fontWeight: FontWeight.semibold,
    textAlign: 'center',
  },
  primaryText: {
    color: C.surface,
  },
  secondaryText: {
    color: C.surface,
  },
  outlineText: {
    color: C.primary,
  },
  ghostText: {
    color: C.primary,
  },
  dangerText: {
    color: C.surface,
  },
  smText: {
    fontSize: FontSize.sm,
  },
  mdText: {
    fontSize: FontSize.md,
  },
  lgText: {
    fontSize: FontSize.lg,
  },
});
