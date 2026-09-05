import { describe, expect, it } from 'vitest';

import {
  GOAL_CALORIE_ADJUSTMENTS,
  MIN_DAILY_CALORIES,
  PROTEIN_G_PER_KG,
} from '../src/constants/nutrition';
import {
  calculateBMI,
  calculateBMR,
  calculateCalorieTarget,
  calculateMacroTargets,
  calculateTargets,
  calculateTDEE,
  formatNutritionValue,
  getBMICategory,
} from '../src/utils/nutrition';

/**
 * The server is the system of record for nutrition targets: it recalculates and stores them on
 * every profile save. These functions exist to preview what it is about to store, so the thing
 * actually worth testing is not "is the arithmetic self-consistent" but "does it still agree
 * with UserProfile.CalculateAndSetTargets()".
 *
 * `serverTargets` below is an independent transcription of that C# method. It is deliberately
 * written out longhand rather than importing anything, so that changing the implementation
 * cannot quietly change the expectation too. If this file starts failing, either the server
 * changed and this needs to follow it, or a client-side "improvement" has reintroduced the bug
 * where the setup screen previewed 175 g of protein and the server then stored 64 g.
 */
function serverTargets(p: {
  weightKg: number;
  heightCm: number;
  age: number;
  gender: string;
  activityLevel: string;
  dietaryGoal: string;
}) {
  const round = (v: number, d = 0) => {
    const f = 10 ** d;
    return Math.round(v * f) / f;
  };

  // Mifflin-St Jeor. Female, Other and PreferNotToSay all take the female branch.
  const bmr =
    p.gender === 'Male'
      ? 10 * p.weightKg + 6.25 * p.heightCm - 5 * p.age + 5
      : 10 * p.weightKg + 6.25 * p.heightCm - 5 * p.age - 161;

  const multiplier =
    ({
      Sedentary: 1.2,
      LightlyActive: 1.375,
      ModeratelyActive: 1.55,
      VeryActive: 1.725,
      ExtraActive: 1.9,
    } as Record<string, number>)[p.activityLevel] ?? 1.2;

  const tdee = bmr * multiplier;

  const adjusted =
    ({
      WeightLoss: tdee - 500,
      WeightGain: tdee + 300,
      MuscleGain: tdee + 300,
      WeightMaintenance: tdee,
      GeneralHealth: tdee,
    } as Record<string, number>)[p.dietaryGoal] ?? tdee;

  const calories = Math.max(adjusted, 1200);

  const proteinG = round(p.weightKg * (p.dietaryGoal === 'MuscleGain' ? 1.2 : 0.8), 1);
  const remaining = calories - proteinG * 4;

  return {
    calories: Math.round(calories),
    proteinG,
    carbsG: round((remaining * 0.55) / 4, 1),
    fatG: round((remaining * 0.35) / 9, 1),
  };
}

const GENDERS = ['Male', 'Female', 'Other', 'PreferNotToSay'];
const ACTIVITY_LEVELS = [
  'Sedentary',
  'LightlyActive',
  'ModeratelyActive',
  'VeryActive',
  'ExtraActive',
];
const GOALS = [
  'WeightLoss',
  'WeightMaintenance',
  'WeightGain',
  'MuscleGain',
  'GeneralHealth',
];

describe('calculateTargets matches the server', () => {
  it('agrees on a representative profile', () => {
    const profile = {
      weightKg: 80,
      heightCm: 180,
      age: 30,
      gender: 'Male',
      activityLevel: 'ModeratelyActive',
      dietaryGoal: 'WeightLoss',
    };

    // Spelled out rather than only compared to the oracle, so the regression this guards
    // against is legible: protein is 64 g from body weight, not 175 g from a share of calories.
    expect(calculateTargets(profile)).toEqual({
      calories: 2259,
      proteinG: 64,
      carbsG: 275.4,
      fatG: 77.9,
    });
  });

  it('agrees across every gender, activity level and goal', () => {
    for (const gender of GENDERS) {
      for (const activityLevel of ACTIVITY_LEVELS) {
        for (const dietaryGoal of GOALS) {
          const profile = { weightKg: 72.5, heightCm: 173, age: 34, gender, activityLevel, dietaryGoal };
          expect(calculateTargets(profile), `${gender}/${activityLevel}/${dietaryGoal}`)
            .toEqual(serverTargets(profile));
        }
      }
    }
  });

  it('agrees at the extremes of the input ranges the API accepts', () => {
    // UpdateProfileRequest allows height 50-300, weight 20-500, age 1-120.
    const edges = [
      { weightKg: 20, heightCm: 50, age: 1 },
      { weightKg: 500, heightCm: 300, age: 120 },
      { weightKg: 45, heightCm: 150, age: 70 },
      { weightKg: 20, heightCm: 50, age: 120 },
    ];

    for (const edge of edges) {
      const profile = {
        ...edge,
        gender: 'Female',
        activityLevel: 'Sedentary',
        dietaryGoal: 'WeightLoss',
      };
      expect(calculateTargets(profile), JSON.stringify(edge)).toEqual(serverTargets(profile));
    }
  });

  it('treats every non-Male gender with the female formula, as the server does', () => {
    const base = { weightKg: 70, heightCm: 170, age: 30, activityLevel: 'Sedentary', dietaryGoal: 'GeneralHealth' };

    const female = calculateTargets({ ...base, gender: 'Female' });
    expect(calculateTargets({ ...base, gender: 'Other' })).toEqual(female);
    expect(calculateTargets({ ...base, gender: 'PreferNotToSay' })).toEqual(female);
    expect(calculateTargets({ ...base, gender: 'Male' })).not.toEqual(female);
  });
});

describe('calculateBMR', () => {
  it('uses Mifflin-St Jeor with the +5 male offset', () => {
    expect(calculateBMR(80, 180, 30, 'Male')).toBe(1780);
  });

  it('uses the -161 offset otherwise', () => {
    expect(calculateBMR(80, 180, 30, 'Female')).toBe(1614);
  });

  it('does not round, so the activity multiplier applies to the exact value', () => {
    // 10*70.3 + 6.25*172.7 + -5*29 + 5 = 1642.375
    expect(calculateBMR(70.3, 172.7, 29, 'Male')).toBeCloseTo(1642.375, 6);
  });
});

describe('calculateTDEE', () => {
  it('applies the activity multiplier', () => {
    expect(calculateTDEE(2000, 'ModeratelyActive')).toBe(3100);
  });

  it('falls back to sedentary for an unknown level rather than producing NaN', () => {
    expect(calculateTDEE(2000, 'not-a-level')).toBe(2400);
  });
});

describe('calculateCalorieTarget', () => {
  it('applies the goal adjustment', () => {
    expect(calculateCalorieTarget(2500, 'WeightLoss')).toBe(2000);
    expect(calculateCalorieTarget(2500, 'WeightMaintenance')).toBe(2500);
  });

  it('treats MuscleGain as +300, matching the server', () => {
    // Shared previously said +250 while the server said +300.
    expect(GOAL_CALORIE_ADJUSTMENTS.MuscleGain).toBe(300);
    expect(calculateCalorieTarget(2500, 'MuscleGain')).toBe(2800);
  });

  it('never recommends less than the safety floor', () => {
    expect(calculateCalorieTarget(1000, 'WeightLoss')).toBe(MIN_DAILY_CALORIES);
    expect(calculateCalorieTarget(1600, 'WeightLoss')).toBe(MIN_DAILY_CALORIES);
  });

  it('ignores an unknown goal rather than producing NaN', () => {
    expect(calculateCalorieTarget(2500, 'not-a-goal')).toBe(2500);
  });
});

describe('calculateMacroTargets', () => {
  it('derives protein from body weight, not from calories', () => {
    const light = calculateMacroTargets(2000, 'WeightLoss', 60);
    const heavy = calculateMacroTargets(2000, 'WeightLoss', 90);

    expect(light.proteinG).toBe(48);
    expect(heavy.proteinG).toBe(72);
  });

  it('raises protein per kg for muscle gain', () => {
    expect(PROTEIN_G_PER_KG.MuscleGain).toBeGreaterThan(PROTEIN_G_PER_KG.default);
    expect(calculateMacroTargets(3000, 'MuscleGain', 80).proteinG).toBe(96);
    expect(calculateMacroTargets(3000, 'WeightLoss', 80).proteinG).toBe(64);
  });

  it('leaves 10% of the post-protein calories unallocated, as the server does', () => {
    const calories = 2400;
    const { proteinG, carbsG, fatG } = calculateMacroTargets(calories, 'GeneralHealth', 70);

    const allocated = proteinG * 4 + carbsG * 4 + fatG * 9;
    const remaining = calories - proteinG * 4;

    // Carbs and fat are rounded to 0.1 g, which is worth up to ~1.3 kcal of slack here, so the
    // tolerance is about the rounding rather than about the split being approximate.
    expect(allocated).toBeGreaterThan(calories - remaining * 0.1 - 2);
    expect(allocated).toBeLessThan(calories - remaining * 0.1 + 2);
  });
});

describe('calculateBMI', () => {
  it('computes weight over height squared, to one decimal', () => {
    expect(calculateBMI(80, 180)).toBe(24.7);
    expect(calculateBMI(60, 165)).toBe(22);
  });
});

describe('getBMICategory', () => {
  it.each([
    [17, 'Underweight'],
    [18.5, 'Normal weight'],
    [24.9, 'Normal weight'],
    [25, 'Overweight'],
    [29.9, 'Overweight'],
    [30, 'Obese'],
  ])('classifies %s as %s', (bmi, expected) => {
    expect(getBMICategory(bmi)).toBe(expected);
  });
});

describe('formatNutritionValue', () => {
  it('leaves whole numbers alone and trims to one decimal otherwise', () => {
    expect(formatNutritionValue(42, 'g')).toBe('42 g');
    expect(formatNutritionValue(42.567, 'g')).toBe('42.6 g');
    expect(formatNutritionValue(2000, 'kcal')).toBe('2000 kcal');
  });

  it('rounds via toFixed, so an exact-half decimal follows the stored binary value', () => {
    // Not "half rounds up" and not "half rounds down" - it depends on how each value is held
    // in binary. 42.55 is really 42.5499..., while 42.75 is exact. Pinned because a displayed
    // total disagreeing with a hand calculation by 0.1 looks like an arithmetic bug.
    expect(formatNutritionValue(42.55, 'g')).toBe('42.5 g');
    expect(formatNutritionValue(42.65, 'g')).toBe('42.6 g');
    expect(formatNutritionValue(42.75, 'g')).toBe('42.8 g');
  });
});
