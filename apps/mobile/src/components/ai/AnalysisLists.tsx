import React, { useMemo } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import type { AIPanelColors } from '@/constants/aiPanel';
import { useTheme, type Palette } from '@/theme';

/**
 * The pill list under an analysis - what the meal is short on.
 *
 * The heading is the caller's: "Missing" for one meal, "Still short on" for a whole day.
 */
export function AnalysisTagList({
  label,
  items,
  panel,
}: {
  label: string;
  items: string[];
  panel: AIPanelColors;
}) {
  const C = useTheme();
  const styles = useMemo(() => makeStyles(C, panel), [C, panel]);

  if (items.length === 0) return null;

  return (
    <View style={styles.section}>
      <Text style={styles.label}>{label}</Text>
      <View style={styles.tagRow}>
        {items.map((item) => (
          <View key={item} style={styles.tag}>
            <Text style={styles.tagText}>{item}</Text>
          </View>
        ))}
      </View>
    </View>
  );
}

/** The bullet list beside it - what to eat to round things out. */
export function AnalysisSuggestionList({
  label,
  items,
  panel,
}: {
  label: string;
  items: string[];
  panel: AIPanelColors;
}) {
  const C = useTheme();
  const styles = useMemo(() => makeStyles(C, panel), [C, panel]);

  if (items.length === 0) return null;

  return (
    <View style={styles.section}>
      <Text style={styles.label}>{label}</Text>
      {items.map((item) => (
        <Text key={item} style={styles.suggestion}>
          {'> '}
          {item}
        </Text>
      ))}
    </View>
  );
}

const makeStyles = (C: Palette, panel: AIPanelColors) =>
  StyleSheet.create({
    section: { gap: 4 },
    label: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.semibold,
      color: C.textSecondary,
      letterSpacing: 0.3,
      textTransform: 'uppercase',
    },
    tagRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 6 },
    tag: {
      borderRadius: BorderRadius.full,
      backgroundColor: panel.tagBg,
      paddingHorizontal: Spacing.sm,
      paddingVertical: 3,
    },
    tagText: { fontSize: FontSize.xs, fontWeight: FontWeight.medium, color: panel.tagText },
    suggestion: { fontSize: FontSize.sm, color: C.text, lineHeight: 19 },
  });
