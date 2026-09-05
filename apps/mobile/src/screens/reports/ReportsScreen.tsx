import React, { useCallback, useEffect, useState } from 'react';
import { RefreshControl, ScrollView, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { subDays } from 'date-fns';
import { Card } from '@/components/ui/Card';
import { ErrorBanner } from '@/components/ui/ErrorBanner';
import { LoadingSpinner } from '@/components/ui/LoadingSpinner';
import { MacroProgress } from '@/screens/dashboard/components/MacroProgress';
import { DailyCalorieChart } from '@/components/reports/DailyCalorieChart';
import { mealService } from '@/services/mealService';
import { MEAL_TYPE_LABELS, type NutritionReport } from '@/types';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import { formatApiDate } from '@/utils/dateUtils';

/** The spans worth one tap. Anything longer stops reading as a daily chart. */
const RANGES = [
  { days: 7, label: '7 days' },
  { days: 30, label: '30 days' },
  { days: 90, label: '90 days' },
];

/** A macro's share of a day is its calories, not its weight: fat carries nine per gram. */
const CALORIES_PER_GRAM = { protein: 4, carbs: 4, fat: 9 };

export function ReportsScreen() {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);

  const [rangeDays, setRangeDays] = useState(30);
  const [report, setReport] = useState<NutritionReport | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setError(null);

    // The range ends today, so the last bar is the day being lived.
    const today = new Date();
    const startDate = formatApiDate(subDays(today, rangeDays - 1));

    try {
      setReport(await mealService.getReport(startDate, formatApiDate(today)));
    } catch {
      setError('We could not build your report. Pull down to try again.');
      setReport(null);
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, [rangeDays]);

  useEffect(() => {
    setIsLoading(true);
    void load();
  }, [load]);

  const onRefresh = useCallback(() => {
    setIsRefreshing(true);
    void load();
  }, [load]);

  const mealTypeCeiling = Math.max(1, ...(report?.byMealType ?? []).map((entry) => entry.calories));
  const hasData = (report?.daysLogged ?? 0) > 0;

  return (
    <ScrollView
      style={styles.screen}
      contentContainerStyle={styles.content}
      refreshControl={
        <RefreshControl refreshing={isRefreshing} onRefresh={onRefresh} colors={[C.primary]} />
      }
    >
      <View style={styles.ranges}>
        {RANGES.map((range) => (
          <TouchableOpacity
            key={range.days}
            style={[styles.rangeChip, rangeDays === range.days && styles.rangeChipActive]}
            onPress={() => setRangeDays(range.days)}
          >
            <Text
              style={[styles.rangeText, rangeDays === range.days && styles.rangeTextActive]}
            >
              {range.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      <ErrorBanner message={error} onDismiss={() => setError(null)} />

      {isLoading ? (
        <LoadingSpinner />
      ) : !report || !hasData ? (
        <Card style={styles.card}>
          <Text style={styles.emptyTitle}>Nothing to report yet</Text>
          <Text style={styles.emptyText}>
            Log a few meals and this screen fills in with how your days compare against your
            targets.
          </Text>
        </Card>
      ) : (
        <>
          <View style={styles.stats}>
            <Stat
              styles={styles}
              label="Avg calories"
              value={Math.round(report.averages.calories).toLocaleString()}
              unit="kcal / logged day"
              hint={report.targetCalories > 0 ? `Target ${report.targetCalories.toLocaleString()}` : ''}
            />
            <Stat
              styles={styles}
              label="Avg protein"
              value={Math.round(report.averages.protein).toLocaleString()}
              unit="g / logged day"
              hint={report.targetProtein > 0 ? `Target ${Math.round(report.targetProtein)} g` : ''}
            />
            <Stat
              styles={styles}
              label="Days logged"
              value={`${report.daysLogged}`}
              unit={`of ${report.daysInRange}`}
              hint={`${report.totalMeals} meals`}
            />
            <Stat
              styles={styles}
              label="Days on target"
              value={`${report.daysOnTarget}`}
              unit={`of ${report.daysLogged} logged`}
              hint="Within 10% of your goal"
            />
          </View>

          <Card style={styles.card}>
            <Text style={styles.cardTitle}>Calories per day</Text>
            <Text style={styles.cardHint}>
              The dashed line is your target. Days with nothing logged are left blank.
            </Text>
            <DailyCalorieChart days={report.days} target={report.targetCalories} />
          </Card>

          <Card style={styles.macroCard}>
            <Text style={styles.macroTitle}>An average logged day</Text>
            <MacroProgress
              protein={{ current: report.averages.protein, target: report.targetProtein }}
              carbs={{ current: report.averages.carbs, target: report.targetCarbs }}
              fat={{ current: report.averages.fat, target: report.targetFat }}
            />
            <Text style={styles.macroFootnote}>
              {shareOfCalories(report)} · {Math.round(report.averages.fiber)} g fibre a day
            </Text>
          </Card>

          {report.byMealType.length > 0 ? (
            <Card style={styles.card}>
              <Text style={styles.cardTitle}>Calories by meal</Text>
              <Text style={styles.cardHint}>Across the whole range.</Text>
              {report.byMealType.map((entry) => (
                <View key={entry.mealType} style={styles.mealRow}>
                  <Text style={styles.mealLabel}>{MEAL_TYPE_LABELS[entry.mealType]}</Text>
                  <View style={styles.mealTrack}>
                    <View
                      style={[
                        styles.mealFill,
                        { width: `${Math.round((entry.calories / mealTypeCeiling) * 100)}%` },
                      ]}
                    />
                  </View>
                  <Text style={styles.mealValue}>{Math.round(entry.calories).toLocaleString()}</Text>
                </View>
              ))}
            </Card>
          ) : null}

          {report.topFoods.length > 0 ? (
            <Card style={styles.card}>
              <Text style={styles.cardTitle}>Most logged foods</Text>
              <Text style={styles.cardHint}>What you actually ate most often.</Text>
              {report.topFoods.map((food) => (
                <View key={food.name} style={styles.foodRow}>
                  <Text style={styles.foodName} numberOfLines={1}>
                    {food.name}
                  </Text>
                  <Text style={styles.foodMeta}>
                    {food.timesLogged}× · {Math.round(food.calories).toLocaleString()} kcal
                  </Text>
                </View>
              ))}
            </Card>
          ) : null}
        </>
      )}
    </ScrollView>
  );
}

interface StatProps {
  styles: ReturnType<typeof makeStyles>;
  label: string;
  value: string;
  unit: string;
  hint?: string;
}

function Stat({ styles, label, value, unit, hint }: StatProps) {
  return (
    <Card style={styles.stat}>
      <Text style={styles.statLabel}>{label}</Text>
      <Text style={styles.statValue}>{value}</Text>
      <Text style={styles.statUnit}>{unit}</Text>
      {hint ? <Text style={styles.statHint}>{hint}</Text> : null}
    </Card>
  );
}

/** "38% carbs · 30% protein · 32% fat" for the average day, or nothing when there is no day. */
function shareOfCalories(report: NutritionReport): string {
  const { protein, carbs, fat } = report.averages;
  const parts = [
    { label: 'protein', calories: protein * CALORIES_PER_GRAM.protein },
    { label: 'carbs', calories: carbs * CALORIES_PER_GRAM.carbs },
    { label: 'fat', calories: fat * CALORIES_PER_GRAM.fat },
  ];
  const total = parts.reduce((running, part) => running + part.calories, 0);

  if (total <= 0) return 'No macros recorded';

  return parts
    .map((part) => `${Math.round((part.calories / total) * 100)}% ${part.label}`)
    .join(' · ');
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    screen: { flex: 1, backgroundColor: C.background },
    content: { padding: Spacing.md, gap: Spacing.md, paddingBottom: Spacing.xl },
    ranges: { flexDirection: 'row', gap: Spacing.sm },
    rangeChip: {
      borderWidth: 1,
      borderColor: C.divider,
      borderRadius: BorderRadius.full,
      paddingHorizontal: Spacing.md,
      minHeight: 32,
      justifyContent: 'center',
      backgroundColor: C.surface,
    },
    rangeChipActive: { backgroundColor: C.primary, borderColor: C.primary },
    rangeText: {
      fontSize: FontSize.sm,
      lineHeight: 18,
      color: C.textSecondary,
      fontWeight: FontWeight.medium,
    },
    rangeTextActive: { color: C.onPrimary },
    stats: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.md },
    // Two to a row, with the gap taken out of the pair rather than out of the screen.
    stat: { flexGrow: 1, flexBasis: '45%' },
    statLabel: { fontSize: FontSize.xs, color: C.textSecondary, fontWeight: FontWeight.medium },
    statValue: { fontSize: FontSize.xxl, fontWeight: FontWeight.bold, color: C.text },
    statUnit: { fontSize: FontSize.xs, color: C.textSecondary },
    statHint: { fontSize: FontSize.xs, color: C.textHint, marginTop: 2 },
    card: { gap: Spacing.xs },
    cardTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.semibold, color: C.text },
    cardHint: { fontSize: FontSize.xs, color: C.textSecondary, marginBottom: Spacing.sm },
    // MacroProgress brings its own padding and heading, so this card supplies neither.
    macroCard: { padding: 0, overflow: 'hidden' },
    macroTitle: {
      fontSize: FontSize.lg,
      fontWeight: FontWeight.semibold,
      color: C.text,
      paddingHorizontal: Spacing.md,
      paddingTop: Spacing.md,
    },
    macroFootnote: {
      fontSize: FontSize.xs,
      color: C.textSecondary,
      paddingHorizontal: Spacing.md,
      paddingBottom: Spacing.md,
    },
    mealRow: { flexDirection: 'row', alignItems: 'center', gap: Spacing.sm, paddingVertical: 4 },
    mealLabel: { fontSize: FontSize.sm, color: C.text, width: 96 },
    mealTrack: {
      flex: 1,
      height: 8,
      borderRadius: 4,
      backgroundColor: C.surfaceAlt,
      overflow: 'hidden',
    },
    mealFill: { height: 8, borderRadius: 4, backgroundColor: C.primary },
    mealValue: { fontSize: FontSize.xs, color: C.textSecondary, width: 56, textAlign: 'right' },
    foodRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      gap: Spacing.sm,
      paddingVertical: 6,
      borderTopWidth: StyleSheet.hairlineWidth,
      borderTopColor: C.divider,
    },
    foodName: { flex: 1, fontSize: FontSize.sm, color: C.text },
    foodMeta: { fontSize: FontSize.xs, color: C.textSecondary },
    emptyTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.semibold, color: C.text },
    emptyText: { fontSize: FontSize.sm, color: C.textSecondary, lineHeight: 20 },
  });
