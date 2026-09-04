import { describe, expect, it } from 'vitest';

import { scorePillClass, scoreStroke, scoreTextClass } from './analysisScore';

/**
 * The same score is coloured in three places, and mobile keeps a parallel copy of this file
 * (its own comment says "Matches apps/web/src/utils/analysisScore.ts"). Phase 2 moves this
 * into @foodeez/shared and deletes the fork; these pin the thresholds so the move cannot
 * quietly shift where amber becomes green.
 */

const BANDS = [
  { name: 'good', scores: [75, 80, 99, 100] },
  { name: 'middling', scores: [50, 60, 74] },
  { name: 'poor', scores: [0, 25, 49] },
];

describe('scoreStroke', () => {
  it('is green at 75 and above', () => {
    for (const score of BANDS[0].scores) expect(scoreStroke(score)).toBe('#16a34a');
  });

  it('is amber from 50 to 74', () => {
    for (const score of BANDS[1].scores) expect(scoreStroke(score)).toBe('#f59e0b');
  });

  it('is red below 50', () => {
    for (const score of BANDS[2].scores) expect(scoreStroke(score)).toBe('#ef4444');
  });

  it('switches exactly at the boundaries, not one either side', () => {
    expect(scoreStroke(74)).not.toBe(scoreStroke(75));
    expect(scoreStroke(49)).not.toBe(scoreStroke(50));
    expect(scoreStroke(75)).toBe(scoreStroke(76));
    expect(scoreStroke(50)).toBe(scoreStroke(51));
  });
});

describe('all three helpers agree on the band', () => {
  it.each([
    [100, 'green'],
    [75, 'green'],
    [74, 'amber'],
    [50, 'amber'],
    [49, 'red'],
    [0, 'red'],
  ])('score %i is in the %s band everywhere', (score, band) => {
    const expected = { green: 'green', amber: 'amber', red: 'red' }[band as string]!;

    expect(scoreTextClass(score)).toContain(expected);
    expect(scorePillClass(score)).toContain(expected);
  });

  it('gives each band a distinct colour in every helper', () => {
    const scores = [80, 60, 20];
    for (const fn of [scoreStroke, scoreTextClass, scorePillClass]) {
      expect(new Set(scores.map(fn)).size).toBe(3);
    }
  });
});

describe('out-of-range scores', () => {
  it('does not crash, and treats anything above the top band as good', () => {
    expect(scoreStroke(140)).toBe('#16a34a');
  });

  it('treats a negative score as poor rather than throwing', () => {
    expect(scoreStroke(-10)).toBe('#ef4444');
    expect(scoreTextClass(-10)).toContain('red');
  });
});
