import React, { useState } from 'react';
import { LayoutChangeEvent, StyleSheet, Text, View } from 'react-native';
import Svg, { Line, Rect } from 'react-native-svg';
import { FontSize, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import type { NutritionReportDay } from '@/types';

interface DailyCalorieChartProps {
  days: NutritionReportDay[];
  /** The user's daily calorie goal. A dashed rule is drawn across at this height when set. */
  target: number;
  height?: number;
}

/**
 * Calories per day, one bar per day, with the target drawn across.
 *
 * Deliberately hand-drawn rather than a charting library: the app has no chart dependency and
 * this needs bars, a rule and nothing else. The width comes from a layout pass instead of a
 * guess, so ninety thin bars fill the card exactly as seven fat ones do.
 */
export function DailyCalorieChart({ days, target, height = 160 }: DailyCalorieChartProps) {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const [width, setWidth] = useState(0);

  const onLayout = (event: LayoutChangeEvent) => setWidth(event.nativeEvent.layout.width);

  // The tallest thing drawn, so a run of over-target days still fits inside the box.
  const ceiling = Math.max(target, ...days.map((day) => day.calories), 1);
  const slot = days.length > 0 && width > 0 ? width / days.length : 0;
  // Thin bars lose their gap entirely rather than disappearing into it.
  const gap = Math.min(2, slot * 0.25);
  const barWidth = Math.max(1, slot - gap);
  const targetY = target > 0 ? height - (target / ceiling) * height : null;

  return (
    <View>
      <View style={{ height }} onLayout={onLayout}>
        {slot > 0 ? (
          <Svg width={width} height={height}>
            {days.map((day, index) => {
              const barHeight = day.hasLogs ? (day.calories / ceiling) * height : 0;

              return (
                <Rect
                  key={day.date}
                  x={index * slot + gap / 2}
                  y={height - barHeight}
                  width={barWidth}
                  height={barHeight}
                  rx={barWidth > 4 ? 2 : 0}
                  fill={target > 0 && day.calories > target ? C.warning : C.primary}
                />
              );
            })}

            {targetY !== null ? (
              <Line
                x1={0}
                y1={targetY}
                x2={width}
                y2={targetY}
                stroke={C.textHint}
                strokeWidth={1}
                strokeDasharray="4 3"
              />
            ) : null}
          </Svg>
        ) : null}
      </View>

      {days.length > 0 ? (
        <View style={styles.axis}>
          <Text style={styles.axisLabel}>{shortDate(days[0].date)}</Text>
          <Text style={styles.axisLabel}>{shortDate(days[days.length - 1].date)}</Text>
        </View>
      ) : null}
    </View>
  );
}

/** "3 Sep" from an API date, without pulling the string through a Date and a timezone. */
function shortDate(value: string): string {
  const [, month, day] = value.split('-');
  const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

  return `${Number(day)} ${MONTHS[Number(month) - 1] ?? ''}`.trim();
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    axis: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      marginTop: Spacing.xs,
    },
    axisLabel: { fontSize: FontSize.xs, color: C.textSecondary },
  });
