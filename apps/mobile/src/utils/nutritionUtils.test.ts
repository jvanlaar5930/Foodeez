import { calculateTargets } from '@foodeez/shared';
import { ActivityLevel, DietaryGoal, Gender } from '@/types';

import { clampPercentage, formatCalories, formatMacro } from './nutritionUtils';

/**
 * The target calculations this file used to own now live in `@foodeez/shared`, where they are
 * checked against a longhand transcription of the server's own method. What is left here is
 * the display formatting, plus one test that the app really is reaching the shared
 * calculation - the point of Phase 2 was that mobile had its own drifted copy, so "is it
 * wired up" is the thing worth asserting from this side.
 */

describe('shared targets are reachable from this app', () => {
    it('previews the same figures the server will store', () => {
        const targets = calculateTargets({
            weightKg: 80,
            heightCm: 180,
            age: 30,
            gender: Gender.Male,
            activityLevel: ActivityLevel.ModeratelyActive,
            dietaryGoal: DietaryGoal.WeightLoss,
        });

        // Protein from body weight (80 * 0.8), not from a share of calories - the drift that
        // previously showed 175 g here and saved 64 g on the server.
        expect(targets).toEqual({ calories: 2259, proteinG: 64, carbsG: 275.4, fatG: 77.9 });
    });

    it('accepts this app\'s enums directly, so the two definitions really are one', () => {
        expect(Gender.Male).toBe('Male');
        expect(ActivityLevel.LightlyActive).toBe('LightlyActive');
        expect(DietaryGoal.WeightLoss).toBe('WeightLoss');
    });
});

describe('display helpers', () => {
    it('formats calories with a thousands separator', () => {
        expect(formatCalories(2259)).toBe('2,259 kcal');
        expect(formatCalories(2259.4)).toBe('2,259 kcal');
        expect(formatCalories(0)).toBe('0 kcal');
    });

    it('formats macros as whole grams', () => {
        expect(formatMacro(77.9)).toBe('78g');
        expect(formatMacro(0)).toBe('0g');
    });

    it('clamps percentages into 0-100', () => {
        expect(clampPercentage(-5)).toBe(0);
        expect(clampPercentage(140)).toBe(100);
        expect(clampPercentage(63)).toBe(63);
        expect(clampPercentage(0)).toBe(0);
        expect(clampPercentage(100)).toBe(100);
    });
});
