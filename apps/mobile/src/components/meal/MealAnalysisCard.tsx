import React, { useMemo } from 'react';
import { ActivityIndicator, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import Svg, { Circle } from 'react-native-svg';
import { FontSize, FontWeight, Spacing, BorderRadius } from '@/constants/theme';
import { AIPanelDark, AIPanelLight, type AIPanelColors } from '@/constants/aiPanel';
import { useTheme, useThemeMode, type Palette } from '@/theme';
import { scoreColor } from '@/utils/analysisScore';
import type { MealAnalysisDto } from '@/types';

const RADIUS = 24;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

/**
 * The AI analysis panel shared by the add/edit meal screen and, in shape, the day analysis
 * card - a score ring, what is missing, and what would round the meal out.
 */
export function MealAnalysisCard({
  analysis,
  isAnalyzing,
  error,
  streamedText,
  analyzeLabel,
  onAnalyze,
}: {
  analysis: MealAnalysisDto | null;
  isAnalyzing: boolean;
  error: string | null;
  /** What the model has written so far this run, shown while there is no parsed result yet. */
  streamedText: string;
  analyzeLabel: string;
  onAnalyze: () => void;
}) {
  const C = useTheme();
  const { scheme } = useThemeMode();
  const AI = scheme === 'dark' ? AIPanelDark : AIPanelLight;
  const styles = useMemo(() => makeStyles(C, AI), [C, AI]);

  const analyzedOn = analysis?.generatedAt
    ? new Date(analysis.generatedAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
    : null;

  return (
    <View>
      <TouchableOpacity style={styles.button} disabled={isAnalyzing} onPress={onAnalyze}>
        {isAnalyzing ? (
          <ActivityIndicator size="small" color={C.secondary} />
        ) : (
          <Text style={styles.buttonIcon}>*</Text>
        )}
        <Text style={styles.buttonText}>{analyzeLabel}</Text>
      </TouchableOpacity>

      {(isAnalyzing || error) && (
        <View style={styles.panel}>
          {error ? (
            <Text style={styles.errorText}>{error}</Text>
          ) : (
            <Text style={styles.streamText}>{streamedText.length > 0 ? streamedText : 'Reading your meal...'}</Text>
          )}
        </View>
      )}

      {!isAnalyzing && analysis && (
        <View style={styles.panel}>
          <View style={styles.headRow}>
            <View style={styles.ringWrap}>
              <Svg width={56} height={56} viewBox="0 0 56 56">
                <Circle cx={28} cy={28} r={RADIUS} stroke={AI.ringTrack} strokeWidth={5} fill="none" />
                <Circle
                  cx={28}
                  cy={28}
                  r={RADIUS}
                  stroke={scoreColor(analysis.score)}
                  strokeWidth={5}
                  fill="none"
                  strokeLinecap="round"
                  strokeDasharray={`${(analysis.score / 100) * CIRCUMFERENCE} ${CIRCUMFERENCE}`}
                  rotation={-90}
                  origin="28, 28"
                />
              </Svg>
              <Text style={[styles.ringScore, { color: scoreColor(analysis.score) }]}>{analysis.score}</Text>
            </View>
            <View style={styles.flex}>
              <Text style={styles.headLabel}>Meal Score</Text>
              <Text style={styles.completeness}>{analysis.completeness}</Text>
              {analyzedOn && <Text style={styles.analyzedOn}>Analyzed {analyzedOn}</Text>}
            </View>
          </View>

          {analysis.missing.length > 0 && (
            <View style={styles.section}>
              <Text style={styles.sectionLabel}>Missing</Text>
              <View style={styles.tagRow}>
                {analysis.missing.map((item) => (
                  <View key={item} style={styles.missingTag}>
                    <Text style={styles.missingTagText}>{item}</Text>
                  </View>
                ))}
              </View>
            </View>
          )}

          {analysis.suggestions.length > 0 && (
            <View style={styles.section}>
              <Text style={styles.sectionLabel}>Suggestions</Text>
              {analysis.suggestions.map((suggestion) => (
                <Text key={suggestion} style={styles.suggestion}>
                  {'> '}
                  {suggestion}
                </Text>
              ))}
            </View>
          )}
        </View>
      )}
    </View>
  );
}

const makeStyles = (C: Palette, AI: AIPanelColors) =>
  StyleSheet.create({
    flex: { flex: 1 },
    button: {
      marginTop: Spacing.md,
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      gap: Spacing.xs,
      borderWidth: 1.5,
      borderStyle: 'dashed',
      borderColor: AI.buttonBorder,
      borderRadius: BorderRadius.lg,
      paddingVertical: Spacing.sm,
    },
    buttonIcon: { fontSize: FontSize.md, color: C.secondary },
    buttonText: { fontSize: FontSize.sm, fontWeight: FontWeight.semibold, color: AI.accentText },
    panel: {
      marginTop: Spacing.sm,
      borderRadius: BorderRadius.lg,
      borderWidth: 1,
      borderColor: AI.panelBorder,
      backgroundColor: AI.panelBg,
      padding: Spacing.md,
      gap: Spacing.sm,
    },
    errorText: { fontSize: FontSize.sm, color: C.error },
    streamText: { fontSize: FontSize.sm, color: C.textSecondary, lineHeight: 20 },
    headRow: { flexDirection: 'row', alignItems: 'center', gap: Spacing.sm },
    ringWrap: { width: 56, height: 56, alignItems: 'center', justifyContent: 'center' },
    ringScore: { position: 'absolute', fontSize: FontSize.sm, fontWeight: FontWeight.bold },
    headLabel: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.bold,
      color: AI.accentText,
      letterSpacing: 0.5,
      textTransform: 'uppercase',
    },
    completeness: { fontSize: FontSize.sm, color: C.text, lineHeight: 19, marginTop: 2 },
    analyzedOn: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 2 },
    section: { gap: 4 },
    sectionLabel: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.semibold,
      color: C.textSecondary,
      letterSpacing: 0.3,
      textTransform: 'uppercase',
    },
    tagRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 6 },
    missingTag: {
      borderRadius: BorderRadius.full,
      backgroundColor: AI.tagBg,
      paddingHorizontal: Spacing.sm,
      paddingVertical: 3,
    },
    missingTagText: { fontSize: FontSize.xs, fontWeight: FontWeight.medium, color: AI.tagText },
    suggestion: { fontSize: FontSize.sm, color: C.text, lineHeight: 19 },
  });
