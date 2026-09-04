import React, { useMemo } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import Svg, { Circle } from 'react-native-svg';
import { FontSize, FontWeight } from '@/constants/theme';
import { scoreColor } from '@foodeez/shared';

interface ScoreRingProps {
  /** 0-100. Anything outside that is clamped rather than drawn as an over-full ring. */
  score: number;
  /** The colour of the unfilled remainder, from the active AI panel palette. */
  trackColor: string;
  size?: 'sm' | 'md';
}

/**
 * The score ring the meal and day analysis cards both draw.
 *
 * Two copies existed, each with its own radius and its own hand-derived circumference - the
 * kind of constant that stops matching the moment someone adjusts the radius. Here the
 * circumference is derived from the radius, so it cannot disagree.
 */
export function ScoreRing({ score, trackColor, size = 'sm' }: ScoreRingProps) {
  const { box, center, radius, font } = size === 'md'
    ? { box: 64, center: 32, radius: 28, font: FontSize.md }
    : { box: 56, center: 28, radius: 24, font: FontSize.sm };

  const clamped = Math.min(100, Math.max(0, score));
  const circumference = 2 * Math.PI * radius;
  const color = scoreColor(clamped);
  const styles = useMemo(() => makeStyles(box), [box]);

  return (
    <View style={styles.wrap}>
      <Svg width={box} height={box} viewBox={`0 0 ${box} ${box}`}>
        <Circle cx={center} cy={center} r={radius} stroke={trackColor} strokeWidth={5} fill="none" />
        <Circle
          cx={center}
          cy={center}
          r={radius}
          stroke={color}
          strokeWidth={5}
          fill="none"
          strokeLinecap="round"
          strokeDasharray={`${(clamped / 100) * circumference} ${circumference}`}
          rotation={-90}
          origin={`${center}, ${center}`}
        />
      </Svg>
      <Text style={[styles.score, { color, fontSize: font }]}>{clamped}</Text>
    </View>
  );
}

const makeStyles = (box: number) =>
  StyleSheet.create({
    wrap: { width: box, height: box, alignItems: 'center', justifyContent: 'center' },
    score: { position: 'absolute', fontWeight: FontWeight.bold },
  });
