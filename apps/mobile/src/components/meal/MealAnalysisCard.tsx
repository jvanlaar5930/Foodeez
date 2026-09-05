import React, { useMemo } from 'react';
import { ActivityIndicator, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { FontSize, FontWeight, Spacing, BorderRadius } from '@/constants/theme';
import { AIPanelDark, AIPanelLight, type AIPanelColors } from '@/constants/aiPanel';
import { AnalysisSuggestionList, AnalysisTagList } from '@/components/ai/AnalysisLists';
import { ScoreRing } from '@/components/ai/ScoreRing';
import { useTheme, useThemeMode, type Palette } from '@/theme';
import type { MealAnalysisDto } from '@/types';

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
            <ScoreRing score={analysis.score} trackColor={AI.ringTrack} />
            <View style={styles.flex}>
              <Text style={styles.headLabel}>Meal Score</Text>
              <Text style={styles.completeness}>{analysis.completeness}</Text>
              {analyzedOn && <Text style={styles.analyzedOn}>Analyzed {analyzedOn}</Text>}
            </View>
          </View>

          <AnalysisTagList label="Missing" items={analysis.missing} panel={AI} />
          <AnalysisSuggestionList label="Suggestions" items={analysis.suggestions} panel={AI} />
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
    headLabel: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.bold,
      color: AI.accentText,
      letterSpacing: 0.5,
      textTransform: 'uppercase',
    },
    completeness: { fontSize: FontSize.sm, color: C.text, lineHeight: 19, marginTop: 2 },
    analyzedOn: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 2 },
  });
