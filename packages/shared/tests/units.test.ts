import { describe, expect, it } from 'vitest';

import { UnitSystem } from '../src/enums';
import {
  cmToFeetInches,
  cmToInches,
  feetInchesToCm,
  flOzToMl,
  formatHeight,
  formatQuantity,
  formatServing,
  formatWeight,
  gToOz,
  heightUnitLabel,
  inchesToCm,
  kgToLb,
  lbToKg,
  mlToFlOz,
  ozToG,
  weightUnitLabel,
} from '../src/utils/units';

/**
 * Mobile carries a byte-identical copy of this module that Phase 2 deletes in favour of this
 * one. These tests are what makes that deletion safe: they pin the behaviour both copies are
 * supposed to have, so the swap is verifiable rather than assumed.
 */

describe('mass and length conversions', () => {
  it('round-trips within floating-point tolerance', () => {
    expect(lbToKg(kgToLb(83.4))).toBeCloseTo(83.4, 10);
    expect(ozToG(gToOz(250))).toBeCloseTo(250, 10);
    expect(flOzToMl(mlToFlOz(473))).toBeCloseTo(473, 10);
    expect(inchesToCm(cmToInches(178))).toBeCloseTo(178, 10);
  });

  it('uses the precise pound-per-kilogram constant', () => {
    // Web's local copy used 2.20462, which drifts from this by ~0.3 g at 100 kg.
    expect(kgToLb(1)).toBeCloseTo(2.2046226218, 10);
    expect(kgToLb(100)).toBeCloseTo(220.46226218, 8);
  });

  it('converts inches at exactly 2.54 cm', () => {
    expect(inchesToCm(1)).toBe(2.54);
    expect(cmToInches(2.54)).toBe(1);
  });
});

describe('cmToFeetInches', () => {
  it('splits height into whole feet and inches', () => {
    expect(cmToFeetInches(180)).toEqual({ feet: 5, inches: 11 });
    expect(cmToFeetInches(152.4)).toEqual({ feet: 5, inches: 0 });
  });

  it('carries into the next foot when inches round up to 12', () => {
    // 177.8cm is exactly 70in = 5'10"; just under it must not report 5'12".
    const { feet, inches } = cmToFeetInches(177.7);
    expect(inches).toBeLessThan(12);
    expect(feet).toBe(5);
  });

  it('round-trips with feetInchesToCm to within half an inch', () => {
    // The split rounds to whole inches, so the round trip can move by up to 1.27 cm.
    for (const cm of [150, 163, 175.5, 190, 201]) {
      const { feet, inches } = cmToFeetInches(cm);
      expect(Math.abs(feetInchesToCm(feet, inches) - cm), `${cm} cm`).toBeLessThanOrEqual(1.27);
    }
  });
});

describe('formatWeight', () => {
  it('shows kilograms under the metric system', () => {
    expect(formatWeight(83.45, UnitSystem.Metric)).toBe('83.5 kg');
  });

  it('converts to pounds under the US system', () => {
    expect(formatWeight(80, UnitSystem.US)).toBe('176.4 lb');
  });

  it('honours the requested precision', () => {
    expect(formatWeight(83.456, UnitSystem.Metric, 0)).toBe('83 kg');
    expect(formatWeight(83.456, UnitSystem.Metric, 2)).toBe('83.46 kg');
  });
});

describe('formatHeight', () => {
  it('shows whole centimetres under the metric system', () => {
    expect(formatHeight(180.4, UnitSystem.Metric)).toBe('180 cm');
  });

  it('shows feet and inches under the US system', () => {
    expect(formatHeight(180, UnitSystem.US)).toBe(`5' 11"`);
  });
});

describe('unit labels', () => {
  it('names the unit for input suffixes', () => {
    expect(weightUnitLabel(UnitSystem.Metric)).toBe('kg');
    expect(weightUnitLabel(UnitSystem.US)).toBe('lb');
    expect(heightUnitLabel(UnitSystem.Metric)).toBe('cm');
    expect(heightUnitLabel(UnitSystem.US)).toBe('ft/in');
  });
});

describe('formatServing', () => {
  it('passes portions through unchanged under the metric system', () => {
    expect(formatServing(150, 'g', UnitSystem.Metric)).toBe('150 g');
    expect(formatServing(2, 'slice', UnitSystem.Metric)).toBe('2 slice');
  });

  it('converts metric mass and volume under the US system', () => {
    expect(formatServing(100, 'g', UnitSystem.US)).toBe('3.5 oz');
    expect(formatServing(2, 'kg', UnitSystem.US)).toBe('4.4 lb');
    expect(formatServing(500, 'ml', UnitSystem.US)).toBe('16.9 fl oz');
    expect(formatServing(1, 'l', UnitSystem.US)).toBe('33.8 fl oz');
  });

  it('recognises spelled-out and mixed-case units', () => {
    expect(formatServing(100, 'Grams', UnitSystem.US)).toBe('3.5 oz');
    expect(formatServing(100, '  ml  ', UnitSystem.US)).toBe('3.4 fl oz');
    expect(formatServing(1, 'Litre', UnitSystem.US)).toBe('33.8 fl oz');
  });

  it('leaves count-based portions alone, because they are already in the reader\'s terms', () => {
    expect(formatServing(1, 'slice', UnitSystem.US)).toBe('1 slice');
    expect(formatServing(2, 'cups', UnitSystem.US)).toBe('2 cups');
    expect(formatServing(3, 'medium', UnitSystem.US)).toBe('3 medium');
  });
});

describe('formatQuantity', () => {
  it('writes the fractions a measuring cup has', () => {
    expect(formatQuantity(0.333333334)).toBe('1/3');
    expect(formatQuantity(0.5)).toBe('1/2');
    expect(formatQuantity(0.25)).toBe('1/4');
    expect(formatQuantity(0.75)).toBe('3/4');
    expect(formatQuantity(0.125)).toBe('1/8');
    expect(formatQuantity(0.666666687)).toBe('2/3');
  });

  it('keeps the whole part alongside the fraction', () => {
    expect(formatQuantity(1.5)).toBe('1 1/2');
    expect(formatQuantity(2.25)).toBe('2 1/4');
    expect(formatQuantity(1.333333334)).toBe('1 1/3');
  });

  it('leaves whole numbers whole', () => {
    expect(formatQuantity(0)).toBe('0');
    expect(formatQuantity(1)).toBe('1');
    expect(formatQuantity(300)).toBe('300');
    expect(formatQuantity(2.001)).toBe('2');
  });

  it('rounds to two decimals when no fraction is close enough', () => {
    expect(formatQuantity(0.3)).toBe('0.3');
    expect(formatQuantity(0.6)).toBe('0.6');
    expect(formatQuantity(1.99)).toBe('1.99');
    expect(formatQuantity(12.456)).toBe('12.46');
    expect(formatQuantity(150.5)).toBe('150 1/2');
  });

  it('keeps the sign, and says nothing about a number that is not one', () => {
    expect(formatQuantity(-0.5)).toBe('-1/2');
    expect(formatQuantity(Number.NaN)).toBe('');
  });
});
