import React from 'react';
import { StyleSheet, Text, View, type StyleProp, type ViewStyle } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { FontSize, FontWeight } from '@/constants/theme';
import { useTheme } from '@/theme';

/**
 * The stand-in picture for a recipe that came out of the advice tab.
 *
 * Drawn here rather than loaded as a file: it is the same speech-bubble icon and "Ask AI"
 * wording the tab itself carries, so a reader recognises where the recipe came from, and
 * drawing it keeps it sharp at every size with nothing to fetch.
 */
export function AiRecipeThumb({
  size = 32,
  style,
}: {
  /** Icon size; the label scales with it. */
  size?: number;
  style?: StyleProp<ViewStyle>;
}) {
  const C = useTheme();

  return (
    <View style={[styles.container, { backgroundColor: C.primary }, style]}>
      <Ionicons name="chatbubble-ellipses" size={size} color="#FFFFFF" />
      <Text style={[styles.label, { fontSize: size <= 32 ? FontSize.xs : FontSize.sm }]}>
        Ask AI
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 4,
  },
  label: {
    color: '#FFFFFF',
    fontWeight: FontWeight.semibold,
    letterSpacing: 0.5,
    textTransform: 'uppercase',
  },
});
