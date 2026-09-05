import React, { useEffect, useMemo, useRef, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { FontSize, FontWeight, Spacing, BorderRadius } from '@/constants/theme';
import { AIPanelDark, AIPanelLight, type AIPanelColors } from '@/constants/aiPanel';
import { AnalysisSuggestionList, AnalysisTagList } from '@/components/ai/AnalysisLists';
import { ScoreRing } from '@/components/ai/ScoreRing';
import { useTheme, useThemeMode, type Palette } from '@/theme';
import { getDayAnalysis, analyzeDayStream } from '@/services/aiService';
import { AIStreamError } from '@/services/aiStream';
import type { DayAnalysisDto, MealLogDto } from '@/types';

/**
 * How a day's meals stand against that day's targets, and what would round it out. Only
 * reads what is already stored when the day's meals change; generating a fresh one is
 * always the reader's own choice, since it spends an AI call.
 */
export function DayAnalysisCard({
  userId,
  date,
  dailyLogs,
  isToday,
}: {
  userId: string;
  date: string;
  dailyLogs: MealLogDto[];
  isToday: boolean;
}) {
  const C = useTheme();
  const { scheme } = useThemeMode();
  const AI = scheme === 'dark' ? AIPanelDark : AIPanelLight;
  const styles = useMemo(() => makeStyles(C, AI), [C, AI]);

  const [analysis, setAnalysis] = useState<DayAnalysisDto | null>(null);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [streamedText, setStreamedText] = useState('');
  const request = useRef<{ cancel: () => void } | null>(null);

  // Changes whenever the day's meals do - which is exactly when the server retires the
  // stored analysis, so it is also when this card should go back and ask what is on file.
  const daySignature = dailyLogs
    .map((log) => `${log.mealType}:${log.items.map((item) => `${item.foodItem.id}x${item.quantity}`).join(',')}`)
    .join('|');

  useEffect(() => {
    let cancelled = false;
    setAnalysis(null);
    setError(null);

    if (!userId || dailyLogs.length === 0) {
      return;
    }

    getDayAnalysis(userId, date)
      .then((result) => {
        if (!cancelled) setAnalysis(result);
      })
      .catch(() => {
        if (!cancelled) setAnalysis(null);
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId, date, daySignature, dailyLogs.length]);

  useEffect(() => () => request.current?.cancel(), []);

  if (dailyLogs.length === 0) {
    return null;
  }

  const dayLabel = isToday ? 'today' : 'this day';

  const runAnalysis = (refresh: boolean) => {
    if (isAnalyzing) {
      return;
    }

    setIsAnalyzing(true);
    setError(null);
    setStreamedText('');

    const onDelta = (text: string) => setStreamedText((prev) => prev + text);
    const streaming = analyzeDayStream(userId, date, refresh, onDelta);
    request.current = streaming;

    streaming
      .then(setAnalysis)
      .catch((err: unknown) => {
        setError(err instanceof AIStreamError ? err.message : 'The analysis could not be completed. Please try again.');
      })
      .finally(() => {
        request.current = null;
        setIsAnalyzing(false);
      });
  };

  const analyzedAt = analysis?.generatedAt
    ? new Date(analysis.generatedAt).toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })
    : null;

  return (
    <View style={styles.card}>
      <View style={styles.headerRow}>
        <Text style={styles.headerLabel}>AI Day Analysis</Text>
        {analysis && !isAnalyzing && (
          <TouchableOpacity onPress={() => runAnalysis(true)}>
            <Text style={styles.rerun}>Re-run</Text>
          </TouchableOpacity>
        )}
      </View>

      {isAnalyzing && (
        <Text style={styles.streamText}>
          {streamedText.length > 0 ? streamedText : `Reviewing ${dayLabel}'s meals...`}
        </Text>
      )}

      {!isAnalyzing && error && <Text style={styles.errorText}>{error}</Text>}

      {!isAnalyzing && !error && analysis && (
        <>
          <View style={styles.headRow}>
            <ScoreRing score={analysis.score} trackColor={AI.ringTrack} size="md" />
            <View style={styles.flex}>
              <Text style={styles.status}>{analysis.status}</Text>
              {analyzedAt && <Text style={styles.analyzedAt}>Analyzed at {analyzedAt}</Text>}
            </View>
          </View>

          <AnalysisTagList label="Still short on" items={analysis.gaps} panel={AI} />
          <AnalysisSuggestionList
            label={isToday ? 'What to have next' : 'What would have rounded it out'}
            items={analysis.recommendations}
            panel={AI}
          />
        </>
      )}

      {!isAnalyzing && !analysis && !error && (
        <TouchableOpacity style={styles.button} onPress={() => runAnalysis(false)}>
          {isAnalyzing ? (
            <ActivityIndicator size="small" color={C.secondary} />
          ) : (
            <Text style={styles.buttonIcon}>*</Text>
          )}
          <Text style={styles.buttonText}>Analyze {dayLabel}&apos;s meals</Text>
        </TouchableOpacity>
      )}
    </View>
  );
}

const makeStyles = (C: Palette, AI: AIPanelColors) =>
  StyleSheet.create({
    flex: { flex: 1 },
    card: {
      borderRadius: BorderRadius.lg,
      borderWidth: 1,
      borderColor: AI.panelBorder,
      backgroundColor: AI.panelBg,
      padding: Spacing.md,
      gap: Spacing.sm,
    },
    headerRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
    headerLabel: {
      fontSize: FontSize.xs,
      fontWeight: FontWeight.bold,
      color: AI.accentText,
      letterSpacing: 0.5,
      textTransform: 'uppercase',
    },
    rerun: { fontSize: FontSize.xs, fontWeight: FontWeight.semibold, color: AI.accentText },
    streamText: { fontSize: FontSize.sm, color: C.textSecondary, lineHeight: 20 },
    errorText: { fontSize: FontSize.sm, color: C.error },
    headRow: { flexDirection: 'row', alignItems: 'center', gap: Spacing.sm },
    status: { fontSize: FontSize.sm, color: C.text, lineHeight: 19 },
    analyzedAt: { fontSize: FontSize.xs, color: C.textSecondary, marginTop: 4 },
    button: {
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
  });
