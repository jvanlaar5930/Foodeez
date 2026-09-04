import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

interface ScreenHeaderProps {
  title: string;
  subtitle?: string;
  /** Renders a back chevron to the left of the title. */
  onBack?: () => void;
  /** Anything the screen wants on the right - a filter button, an add button. */
  right?: React.ReactNode;
}

/**
 * The title row at the top of a screen, written out in five of them with slightly different
 * padding each time.
 */
export function ScreenHeader({ title, subtitle, onBack, right }: ScreenHeaderProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  return (
    <View style={styles.header}>
      {onBack ? (
        <TouchableOpacity
          onPress={onBack}
          accessibilityRole="button"
          accessibilityLabel="Go back"
          hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
        >
          <Ionicons name="chevron-back" size={24} color={C.text} />
        </TouchableOpacity>
      ) : null}

      <View style={styles.titles}>
        <Text style={styles.title} numberOfLines={1}>
          {title}
        </Text>
        {subtitle ? (
          <Text style={styles.subtitle} numberOfLines={1}>
            {subtitle}
          </Text>
        ) : null}
      </View>

      {right}
    </View>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: Spacing.sm,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.md,
      backgroundColor: C.surface,
      borderBottomWidth: 1,
      borderBottomColor: C.divider,
    },
    titles: { flex: 1 },
    title: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: C.text },
    subtitle: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2 },
  });
