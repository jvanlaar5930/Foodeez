import { ActivityLevel, DietaryGoal, Gender } from '@/types';

import {
  calculateBMI,
  calculateBMR,
  calculateCalorieTarget,
  calculateMacroTargets,
  calculateTargets,
  calculateTDEE,
  clampPercentage,
  formatCalories,
  formatMacro,
} from './nutritionUtils';

/**
 * These functions preview the targets the server is about to store, so what matters is that
 * they still agree with UserProfile.CalculateAndSetTargets(). They previously did not: this
 * file used a share-of-calories split while the server used grams per kilogram, so the setup
 * screen showed 175 g of protein for a profile the server then saved with 64 g.
 *
 * `serverTargets` is an independent transcription of the C# method, written longhand so that
 * changing the implementation cannot quietly change the expectation with it. The same oracle
 * guards @foodeez/shared, which this module is a temporary copy of - Phase 2 deletes this half
 * of the file once mobile can import shared, and these tests are what makes that safe.
 */
function serverTargets(p: {
  weightKg: number;
  heightCm: number;
  age: number;
  gender: Gender;
  activityLevel: ActivityLevel;
  dietaryGoal: DietaryGoal;
}) {
  const round = (v: number, d = 0) => {
    const f = 10 ** d;
    return Math.round(v * f) / f;
  };

  const bmr =
    p.gender === Gender.Male
      ? 10 * p.weightKg + 6.25 * p.heightCm - 5 * p.age + 5
      : 10 * p.weightKg + 6.25 * p.heightCm - 5 * p.age - 161;

  const multiplier: Record<string, number> = {
    [ActivityLevel.Sedentary]: 1.2,
    [ActivityLevel.LightlyActive]: 1.375,
    [ActivityLevel.ModeratelyActive]: 1.55,
    [ActivityLevel.VeryActive]: 1.725,
    [ActivityLevel.ExtraActive]: 1.9,
  };

  const tdee = bmr * multiplier[p.activityLevel];

  const adjustment: Record<string, number> = {
    [DietaryGoal.WeightLoss]: -500,
    [DietaryGoal.WeightMaintenance]: 0,
    [DietaryGoal.WeightGain]: 300,
    [DietaryGoal.MuscleGain]: 300,
    [DietaryGoal.GeneralHealth]: 0,
  };

  const calories = Math.max(tdee + adjustment[p.dietaryGoal], 1200);
  const protein = round(p.weightKg * (p.dietaryGoal === DietaryGoal.MuscleGain ? 1.2 : 0.8), 1);
  const remaining = calories - protein * 4;

  return {
    calories: Math.round(calories),
    protein,
    carbs: round((remaining * 0.55) / 4, 1),
    fat: round((remaining * 0.35) / 9, 1),
  };
}

describe('calculateTargets matches the server', () => {
  it('agrees on a representative profile', () => {
    expect(
      calculateTargets({
        weightKg: 80,
        heightCm: 180,
        age: 30,
        gender: Gender.Male,
        activityLevel: ActivityLevel.ModeratelyActive,
        dietaryGoal: DietaryGoal.WeightLoss,
      }),
    ).toEqual({ calories: 2259, protein: 64, carbs: 275.4, fat: 77.9 });
  });

  it('agrees across every gender, activity level and goal', () => {
    for (const gender of Object.values(Gender)) {
      for (const activityLevel of Object.values(ActivityLevel)) {
        for (const dietaryGoal of Object.values(DietaryGoal)) {
          const profile = {
            weightKg: 72.5,
            heightCm: 173,
            age: 34,
            gender,
            activityLevel,
            dietaryGoal,
          };

          expect(calculateTargets(profile)).toEqual(serverTargets(profile));
        }
      }
    }
  });

  it('treats every non-Male gender with the female formula', () => {
    const base = {
      weightKg: 70,
      heightCm: 170,
      age: 30,
      activityLevel: ActivityLevel.Sedentary,
      dietaryGoal: DietaryGoal.GeneralHealth,
    };

    const female = calculateTargets({ ...base, gender: Gender.Female });
    expect(calculateTargets({ ...base, gender: Gender.Other })).toEqual(female);
    expect(calculateTargets({ ...base, gender: Gender.PreferNotToSay })).toEqual(female);
    expect(calculateTargets({ ...base, gender: Gender.Male })).not.toEqual(female);
  });
});

describe('calculateBMR', () => {
  it('does not round, so the multiplier applies to the exact value', () => {
    expect(calculateBMR(70.3, 172.7, 29, Gender.Male)).toBeCloseTo(1642.375, 6);
  });
});

describe('calculateCalorieTarget', () => {
  it('treats MuscleGain as +300, as the server does', () => {
    // ProfileSetupScreen previously hardcoded this inline and shared said +250.
    expect(calculateCalorieTarget(2500, DietaryGoal.MuscleGain)).toBe(2800);
  });

  it('never recommends less than 1200 kcal', () => {
    expect(calculateCalorieTarget(1000, DietaryGoal.WeightLoss)).toBe(1200);
  });
});

describe('calculateMacroTargets', () => {
  it('derives protein from body weight rather than from calories', () => {
    expect(calculateMacroTargets(2000, DietaryGoal.WeightLoss, 60).protein).toBe(48);
    expect(calculateMacroTargets(2000, DietaryGoal.WeightLoss, 90).protein).toBe(72);
  });

  it('raises protein per kg for muscle gain', () => {
    expect(calculateMacroTargets(3000, DietaryGoal.MuscleGain, 80).protein).toBe(96);
    expect(calculateMacroTargets(3000, DietaryGoal.WeightLoss, 80).protein).toBe(64);
  });
});

describe('calculateTDEE', () => {
  it('applies the activity multiplier', () => {
    expect(calculateTDEE(2000, ActivityLevel.ModeratelyActive)).toBe(3100);
  });
});

describe('display helpers', () => {
  it('formats calories with a thousands separator', () => {
    expect(formatCalories(2259)).toBe('2,259 kcal');
    expect(formatCalories(2259.4)).toBe('2,259 kcal');
  });

  it('formats macros as whole grams', () => {
    expect(formatMacro(77.9)).toBe('78g');
  });

  it('computes BMI to one decimal', () => {
    expect(calculateBMI(80, 180)).toBe(24.7);
  });

  it('clamps percentages into 0-100', () => {
    expect(clampPercentage(-5)).toBe(0);
    expect(clampPercentage(140)).toBe(100);
    expect(clampPercentage(63)).toBe(63);
  });
});
