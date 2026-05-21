import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import Svg, { Circle } from 'react-native-svg';
import { Colors, FontSize, FontWeight } from '@/constants/theme';

interface CalorieRingProps {
  consumed: number;
  target: number;
  size?: number;
  strokeWidth?: number;
}

export function CalorieRing({
  consumed,
  target,
  size = 180,
  strokeWidth = 16,
}: CalorieRingProps) {
  const radius = (size - strokeWidth) / 2;
  const circumference = 2 * Math.PI * radius;
  const percentage = target > 0 ? Math.min(1, consumed / target) : 0;
  const strokeDashoffset = circumference * (1 - percentage);
  const center = size / 2;

  const getColor = () => {
    const pct = target > 0 ? consumed / target : 0;
    if (pct >= 1) return Colors.error;
    if (pct >= 0.8) return Colors.warning;
    return Colors.primary;
  };

  const ringColor = getColor();
  const remaining = Math.max(0, target - consumed);

  return (
    <View style={styles.container}>
      <Svg width={size} height={size}>
        {/* Background circle */}
        <Circle
          cx={center}
          cy={center}
          r={radius}
          stroke={Colors.divider}
          strokeWidth={strokeWidth}
          fill="transparent"
        />
        {/* Progress circle */}
        <Circle
          cx={center}
          cy={center}
          r={radius}
          stroke={ringColor}
          strokeWidth={strokeWidth}
          fill="transparent"
          strokeDasharray={circumference}
          strokeDashoffset={strokeDashoffset}
          strokeLinecap="round"
          rotation="-90"
          origin={`${center}, ${center}`}
        />
      </Svg>
      <View style={[styles.centerContent, { width: size, height: size }]}>
        <Text style={styles.consumed}>{Math.round(consumed).toLocaleString()}</Text>
        <Text style={styles.unit}>kcal eaten</Text>
        <View style={styles.divider} />
        <Text style={styles.remaining}>{Math.round(remaining).toLocaleString()} left</Text>
        <Text style={styles.target}>of {Math.round(target).toLocaleString()}</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    position: 'relative',
    alignItems: 'center',
    justifyContent: 'center',
  },
  centerContent: {
    position: 'absolute',
    alignItems: 'center',
    justifyContent: 'center',
  },
  consumed: {
    fontSize: FontSize.xxl,
    fontWeight: FontWeight.bold,
    color: Colors.text,
  },
  unit: {
    fontSize: FontSize.sm,
    color: Colors.textSecondary,
    marginBottom: 4,
  },
  divider: {
    width: 40,
    height: 1,
    backgroundColor: Colors.divider,
    marginVertical: 4,
  },
  remaining: {
    fontSize: FontSize.lg,
    fontWeight: FontWeight.semibold,
    color: Colors.text,
  },
  target: {
    fontSize: FontSize.xs,
    color: Colors.textHint,
  },
});
