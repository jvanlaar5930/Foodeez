import React from 'react';
import { ActivityIndicator, StyleSheet, TextInput, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { BorderRadius, FontSize, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

interface SearchBarProps {
  value: string;
  onChangeText: (text: string) => void;
  placeholder?: string;
  /** Replaces the clear button with a spinner while a search is in flight. */
  isSearching?: boolean;
  autoFocus?: boolean;
  onSubmit?: () => void;
}

/**
 * The search field, in the same shape on every screen that has one.
 *
 * Three copies existed. All three had the magnifier; only one had a clear button, which is
 * the control people actually reach for on a phone.
 */
export const SearchBar = React.forwardRef<TextInput, SearchBarProps>(function SearchBar(
  { value, onChangeText, placeholder = 'Search', isSearching = false, autoFocus, onSubmit },
  ref,
) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  return (
    <View style={styles.bar}>
      <Ionicons name="search" size={18} color={C.textSecondary} />
      <TextInput
        ref={ref}
        style={styles.input}
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        placeholderTextColor={C.textHint}
        autoFocus={autoFocus}
        returnKeyType="search"
        onSubmitEditing={onSubmit}
        autoCorrect={false}
        accessibilityLabel={placeholder}
      />
      {isSearching ? (
        <ActivityIndicator size="small" color={C.textSecondary} />
      ) : value.length > 0 ? (
        <TouchableOpacity
          onPress={() => onChangeText('')}
          accessibilityRole="button"
          accessibilityLabel="Clear search"
          hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
        >
          <Ionicons name="close-circle" size={18} color={C.textHint} />
        </TouchableOpacity>
      ) : null}
    </View>
  );
});

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    bar: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: Spacing.sm,
      backgroundColor: C.surfaceAlt,
      borderRadius: BorderRadius.lg,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.sm,
    },
    input: { flex: 1, fontSize: FontSize.md, color: C.text, paddingVertical: 2 },
  });
