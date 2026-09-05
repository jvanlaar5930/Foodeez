import { UnitSystem } from '../enums';

/**
 * Conversions between the two measurement systems.
 *
 * Scope note: this covers body measurements and food portions only. Macronutrients stay in
 * grams and energy stays in kcal under both systems - that is how nutrition is labelled in
 * the US as well, so "converting" them would make the numbers less recognisable, not more.
 */

const LB_PER_KG = 2.2046226218;
const CM_PER_INCH = 2.54;
const G_PER_OZ = 28.349523125;
const ML_PER_FL_OZ = 29.5735295625;

export const kgToLb = (kg: number): number => kg * LB_PER_KG;
export const lbToKg = (lb: number): number => lb / LB_PER_KG;
export const gToOz = (g: number): number => g / G_PER_OZ;
export const ozToG = (oz: number): number => oz * G_PER_OZ;
export const mlToFlOz = (ml: number): number => ml / ML_PER_FL_OZ;
export const flOzToMl = (flOz: number): number => flOz * ML_PER_FL_OZ;
export const cmToInches = (cm: number): number => cm / CM_PER_INCH;
export const inchesToCm = (inches: number): number => inches * CM_PER_INCH;

export function cmToFeetInches(cm: number): { feet: number; inches: number } {
  const totalInches = Math.round(cmToInches(cm));
  return { feet: Math.floor(totalInches / 12), inches: totalInches % 12 };
}

export function feetInchesToCm(feet: number, inches: number): number {
  return inchesToCm(feet * 12 + inches);
}

function round(value: number, places = 1): number {
  const factor = 10 ** places;
  return Math.round(value * factor) / factor;
}

/** Body weight, stored in kg. */
export function formatWeight(kg: number, system: UnitSystem, places = 1): string {
  return system === UnitSystem.Metric
    ? `${round(kg, places)} kg`
    : `${round(kgToLb(kg), places)} lb`;
}

/** Height, stored in cm. */
export function formatHeight(cm: number, system: UnitSystem): string {
  if (system === UnitSystem.Metric) return `${Math.round(cm)} cm`;
  const { feet, inches } = cmToFeetInches(cm);
  return `${feet}' ${inches}"`;
}

/** The unit label alone, for input field suffixes. */
export function weightUnitLabel(system: UnitSystem): string {
  return system === UnitSystem.Metric ? 'kg' : 'lb';
}

export function heightUnitLabel(system: UnitSystem): string {
  return system === UnitSystem.Metric ? 'cm' : 'ft/in';
}

/**
 * A food portion, whose stored unit varies by item. Only metric mass and volume units have a
 * customary equivalent worth showing; counts like "1 slice" or "2 cups" are already in the
 * reader's terms and pass through untouched.
 */
export function formatServing(amount: number, unit: string, system: UnitSystem): string {
  if (system === UnitSystem.Metric) return `${round(amount)} ${unit}`;

  switch (unit.trim().toLowerCase()) {
    case 'g':
    case 'gram':
    case 'grams':
      return `${round(gToOz(amount))} oz`;
    case 'kg':
      return `${round(kgToLb(amount))} lb`;
    case 'ml':
    case 'millilitre':
    case 'milliliter':
      return `${round(mlToFlOz(amount))} fl oz`;
    case 'l':
    case 'litre':
    case 'liter':
      return `${round(mlToFlOz(amount * 1000))} fl oz`;
    default:
      return `${round(amount)} ${unit}`;
  }
}

/** The denominators a cook measures with: thirds and eighths of a cup exist, sevenths do not. */
const FRACTION_DENOMINATORS = [2, 3, 4, 8];

/** How far an amount may sit from a fraction and still be written as one. */
const FRACTION_TOLERANCE = 0.02;

/**
 * An ingredient amount the way a recipe card writes it: the fraction a measuring cup has where
 * the number is one of those - "1/3", "1 1/2" - and otherwise the number to two decimals.
 *
 * Amounts reach us as floats, so a third of a cup arrives as 0.333333334. Printed raw that is
 * both unreadable and unmeasurable; the fraction is the number the cook actually reaches for.
 */
export function formatQuantity(value: number): string {
  if (!Number.isFinite(value)) return '';

  const sign = value < 0 ? '-' : '';
  const absolute = Math.abs(value);
  const whole = Math.floor(absolute);
  const remainder = absolute - whole;

  for (const denominator of FRACTION_DENOMINATORS) {
    const numerator = Math.round(remainder * denominator);

    // 0 and the denominator itself are whole numbers, not fractions - leave those to the
    // rounding below, which is what keeps 1.99 from reading as 2.
    if (numerator === 0 || numerator === denominator) continue;
    if (Math.abs(remainder - numerator / denominator) > FRACTION_TOLERANCE) continue;

    const fraction = `${numerator}/${denominator}`;
    return whole > 0 ? `${sign}${whole} ${fraction}` : `${sign}${fraction}`;
  }

  return `${sign}${round(absolute, 2)}`;
}
