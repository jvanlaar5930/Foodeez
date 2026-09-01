import React, { useState } from 'react';
import {
  Alert,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { useAuthStore } from '@/store/authStore';
import { useProfileStore } from '@/store/profileStore';
import {
  ActivityLevel,
  DietaryGoal,
  Gender,
} from '@/types';
import {
  calculateBMR,
  calculateMacroTargets,
  calculateTDEE,
  formatCalories,
  formatMacro,
} from '@/utils/nutritionUtils';
import { BorderRadius, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';

const TOTAL_STEPS = 4;

interface ProfileData {
  heightCm: string;
  weightKg: string;
  age: string;
  gender: Gender | null;
  dietaryGoal: DietaryGoal | null;
  activityLevel: ActivityLevel | null;
}

const GOAL_OPTIONS = [
  {
    value: DietaryGoal.WeightLoss,
    emoji: '🔥',
    title: 'Lose Weight',
    description: 'Reduce body fat with a calorie deficit',
  },
  {
    value: DietaryGoal.WeightMaintenance,
    emoji: '⚖️',
    title: 'Maintain Weight',
    description: 'Keep your current weight stable',
  },
  {
    value: DietaryGoal.WeightGain,
    emoji: '📈',
    title: 'Gain Weight',
    description: 'Build mass with a calorie surplus',
  },
  {
    value: DietaryGoal.MuscleGain,
    emoji: '💪',
    title: 'Build Muscle',
    description: 'Maximize muscle growth with protein-rich plans',
  },
  {
    value: DietaryGoal.GeneralHealth,
    emoji: '🥗',
    title: 'Eat Healthier',
    description: 'Improve overall nutrition and wellness',
  },
];

const ACTIVITY_OPTIONS = [
  {
    value: ActivityLevel.Sedentary,
    title: 'Sedentary',
    description: 'Desk job, little or no exercise',
  },
  {
    value: ActivityLevel.LightlyActive,
    title: 'Lightly Active',
    description: 'Light exercise 1-3 days/week',
  },
  {
    value: ActivityLevel.ModeratelyActive,
    title: 'Moderately Active',
    description: 'Moderate exercise 3-5 days/week',
  },
  {
    value: ActivityLevel.VeryActive,
    title: 'Very Active',
    description: 'Hard exercise 6-7 days/week',
  },
  {
    value: ActivityLevel.ExtraActive,
    title: 'Extra Active',
    description: 'Very hard exercise + physical job',
  },
];

const GENDER_OPTIONS = [
  { value: Gender.Male, label: 'Male' },
  { value: Gender.Female, label: 'Female' },
  { value: Gender.Other, label: 'Other' },
  { value: Gender.PreferNotToSay, label: 'Prefer not to say' },
];

export function ProfileSetupScreen() {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const user = useAuthStore((state) => state.user);
  const { updateProfile, isLoading } = useProfileStore();

  const [step, setStep] = useState(1);
  const [data, setData] = useState<ProfileData>({
    heightCm: '',
    weightKg: '',
    age: '',
    gender: null,
    dietaryGoal: null,
    activityLevel: null,
  });

  const updateData = (field: keyof ProfileData, value: ProfileData[keyof ProfileData]) => {
    setData((prev) => ({ ...prev, [field]: value }));
  };

  const getCalorieTargets = () => {
    const weight = parseFloat(data.weightKg);
    const height = parseFloat(data.heightCm);
    const age = parseInt(data.age, 10);
    if (!weight || !height || !age || !data.gender || !data.activityLevel || !data.dietaryGoal) {
      return { calories: 2000, protein: 150, carbs: 250, fat: 67 };
    }
    const bmr = calculateBMR(weight, height, age, data.gender);
    let tdee = calculateTDEE(bmr, data.activityLevel);
    if (data.dietaryGoal === DietaryGoal.WeightLoss) tdee -= 500;
    if (data.dietaryGoal === DietaryGoal.WeightGain || data.dietaryGoal === DietaryGoal.MuscleGain) tdee += 300;
    const macros = calculateMacroTargets(tdee, data.dietaryGoal);
    return { calories: tdee, ...macros };
  };

  const validateStep = (): boolean => {
    switch (step) {
      case 1:
        if (!data.heightCm || !data.weightKg || !data.age || !data.gender) {
          Alert.alert('Missing Information', 'Please fill in all fields before continuing.');
          return false;
        }
        if (isNaN(parseFloat(data.heightCm)) || isNaN(parseFloat(data.weightKg)) || isNaN(parseInt(data.age))) {
          Alert.alert('Invalid Input', 'Please enter valid numbers for height, weight, and age.');
          return false;
        }
        return true;
      case 2:
        if (!data.dietaryGoal) {
          Alert.alert('Select a Goal', 'Please choose your dietary goal to continue.');
          return false;
        }
        return true;
      case 3:
        if (!data.activityLevel) {
          Alert.alert('Select Activity Level', 'Please choose your activity level to continue.');
          return false;
        }
        return true;
      default:
        return true;
    }
  };

  const handleNext = () => {
    if (!validateStep()) return;
    if (step < TOTAL_STEPS) {
      setStep((s) => s + 1);
    }
  };

  const handleBack = () => {
    if (step > 1) setStep((s) => s - 1);
  };

  const handleComplete = async () => {
    if (!user) return;
    const targets = getCalorieTargets();
    try {
      await updateProfile(user.id, {
        heightCm: parseFloat(data.heightCm),
        weightKg: parseFloat(data.weightKg),
        age: parseInt(data.age, 10),
        gender: data.gender ?? Gender.PreferNotToSay,
        dietaryGoal: data.dietaryGoal ?? DietaryGoal.GeneralHealth,
        activityLevel: data.activityLevel ?? ActivityLevel.ModeratelyActive,
      });
    } catch {
      Alert.alert('Error', 'Failed to save profile. Please try again.');
    }
  };

  const targets = getCalorieTargets();

  return (
    <SafeAreaView style={styles.safe}>
      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        {/* Step indicator */}
        <View style={styles.stepIndicator}>
          {Array.from({ length: TOTAL_STEPS }, (_, i) => (
            <View
              key={i}
              style={[
                styles.stepDot,
                step > i && styles.stepDotCompleted,
                step === i + 1 && styles.stepDotActive,
              ]}
            />
          ))}
        </View>
        <Text style={styles.stepLabel}>Step {step} of {TOTAL_STEPS}</Text>

        <ScrollView
          style={styles.scroll}
          contentContainerStyle={styles.content}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          {step === 1 && (
            <View>
              <Text style={styles.stepTitle}>Tell us about yourself</Text>
              <Text style={styles.stepSubtitle}>This helps us calculate your daily targets</Text>

              <Input
                label="Height (cm)"
                value={data.heightCm}
                onChangeText={(v) => updateData('heightCm', v)}
                keyboardType="decimal-pad"
                placeholder="e.g. 175"
              />
              <Input
                label="Weight (kg)"
                value={data.weightKg}
                onChangeText={(v) => updateData('weightKg', v)}
                keyboardType="decimal-pad"
                placeholder="e.g. 70"
              />
              <Input
                label="Age"
                value={data.age}
                onChangeText={(v) => updateData('age', v)}
                keyboardType="number-pad"
                placeholder="e.g. 28"
              />

              <Text style={styles.fieldLabel}>Gender</Text>
              <View style={styles.genderRow}>
                {GENDER_OPTIONS.map((opt) => (
                  <TouchableOpacity
                    key={opt.value}
                    style={[
                      styles.genderBtn,
                      data.gender === opt.value && styles.genderBtnActive,
                    ]}
                    onPress={() => updateData('gender', opt.value)}
                  >
                    <Text
                      style={[
                        styles.genderBtnText,
                        data.gender === opt.value && styles.genderBtnTextActive,
                      ]}
                    >
                      {opt.label}
                    </Text>
                  </TouchableOpacity>
                ))}
              </View>
            </View>
          )}

          {step === 2 && (
            <View>
              <Text style={styles.stepTitle}>What's your goal?</Text>
              <Text style={styles.stepSubtitle}>Choose the goal that best fits your vision</Text>
              <View style={styles.goalList}>
                {GOAL_OPTIONS.map((opt) => (
                  <TouchableOpacity
                    key={opt.value}
                    style={[
                      styles.goalCard,
                      data.dietaryGoal === opt.value && styles.goalCardActive,
                    ]}
                    onPress={() => updateData('dietaryGoal', opt.value)}
                    activeOpacity={0.8}
                  >
                    <Text style={styles.goalEmoji}>{opt.emoji}</Text>
                    <View style={styles.goalText}>
                      <Text
                        style={[
                          styles.goalTitle,
                          data.dietaryGoal === opt.value && styles.goalTitleActive,
                        ]}
                      >
                        {opt.title}
                      </Text>
                      <Text style={styles.goalDesc}>{opt.description}</Text>
                    </View>
                    {data.dietaryGoal === opt.value && (
                      <View style={styles.checkCircle}>
                        <Text style={styles.checkMark}>✓</Text>
                      </View>
                    )}
                  </TouchableOpacity>
                ))}
              </View>
            </View>
          )}

          {step === 3 && (
            <View>
              <Text style={styles.stepTitle}>How active are you?</Text>
              <Text style={styles.stepSubtitle}>Your activity level affects your daily calorie needs</Text>
              <View style={styles.goalList}>
                {ACTIVITY_OPTIONS.map((opt) => (
                  <TouchableOpacity
                    key={opt.value}
                    style={[
                      styles.goalCard,
                      data.activityLevel === opt.value && styles.goalCardActive,
                    ]}
                    onPress={() => updateData('activityLevel', opt.value)}
                    activeOpacity={0.8}
                  >
                    <View style={styles.goalText}>
                      <Text
                        style={[
                          styles.goalTitle,
                          data.activityLevel === opt.value && styles.goalTitleActive,
                        ]}
                      >
                        {opt.title}
                      </Text>
                      <Text style={styles.goalDesc}>{opt.description}</Text>
                    </View>
                    {data.activityLevel === opt.value && (
                      <View style={styles.checkCircle}>
                        <Text style={styles.checkMark}>✓</Text>
                      </View>
                    )}
                  </TouchableOpacity>
                ))}
              </View>
            </View>
          )}

          {step === 4 && (
            <View>
              <Text style={styles.stepTitle}>Your Daily Targets</Text>
              <Text style={styles.stepSubtitle}>
                These are your recommended daily targets based on your profile
              </Text>

              <View style={styles.targetCard}>
                <View style={styles.calorieRow}>
                  <Text style={styles.calorieValue}>{formatCalories(targets.calories)}</Text>
                  <Text style={styles.calorieLabel}>Daily Goal</Text>
                </View>
                <View style={styles.macroDivider} />
                <View style={styles.macroGrid}>
                  {[
                    { label: 'Protein', value: targets.protein, color: C.info },
                    { label: 'Carbs', value: targets.carbs, color: C.secondary },
                    { label: 'Fat', value: targets.fat, color: '#FFC107' },
                  ].map((macro) => (
                    <View key={macro.label} style={styles.macroItem}>
                      <Text style={[styles.macroValue, { color: macro.color }]}>
                        {formatMacro(macro.value)}
                      </Text>
                      <Text style={styles.macroLabel}>{macro.label}</Text>
                    </View>
                  ))}
                </View>
              </View>

              <View style={styles.noteCard}>
                <Text style={styles.noteText}>
                  You can always adjust these targets later in your profile settings.
                </Text>
              </View>
            </View>
          )}

          <View style={styles.navButtons}>
            {step > 1 && (
              <Button
                title="Back"
                variant="outline"
                size="lg"
                onPress={handleBack}
                style={{ flex: 1, marginRight: Spacing.sm }}
              />
            )}
            {step < TOTAL_STEPS ? (
              <Button
                title="Next"
                size="lg"
                onPress={handleNext}
                style={{ flex: step > 1 ? 1 : undefined, width: step === 1 ? '100%' : undefined }}
                fullWidth={step === 1}
              />
            ) : (
              <Button
                title="Complete Setup"
                size="lg"
                loading={isLoading}
                onPress={handleComplete}
                style={{ flex: 1 }}
              />
            )}
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: C.background,
  },
  flex: {
    flex: 1,
  },
  stepIndicator: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: Spacing.sm,
    paddingTop: Spacing.lg,
    paddingHorizontal: Spacing.xl,
  },
  stepDot: {
    width: 10,
    height: 10,
    borderRadius: 5,
    backgroundColor: C.divider,
  },
  stepDotActive: {
    backgroundColor: C.primary,
    width: 24,
  },
  stepDotCompleted: {
    backgroundColor: C.primaryDark,
  },
  stepLabel: {
    textAlign: 'center',
    fontSize: FontSize.sm,
    color: C.textSecondary,
    marginTop: Spacing.xs,
    marginBottom: Spacing.md,
  },
  scroll: {
    flex: 1,
  },
  content: {
    padding: Spacing.xl,
    flexGrow: 1,
  },
  stepTitle: {
    fontSize: FontSize.xxl,
    fontWeight: FontWeight.bold,
    color: C.text,
    marginBottom: Spacing.xs,
  },
  stepSubtitle: {
    fontSize: FontSize.md,
    color: C.textSecondary,
    marginBottom: Spacing.xl,
    lineHeight: 22,
  },
  fieldLabel: {
    fontSize: FontSize.sm,
    fontWeight: FontWeight.medium,
    color: C.text,
    marginBottom: Spacing.sm,
  },
  genderRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.sm,
    marginBottom: Spacing.lg,
  },
  genderBtn: {
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm,
    borderRadius: BorderRadius.full,
    borderWidth: 1.5,
    borderColor: C.divider,
    backgroundColor: C.surface,
  },
  genderBtnActive: {
    borderColor: C.primary,
    backgroundColor: C.primaryLight,
  },
  genderBtnText: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
    fontWeight: FontWeight.medium,
  },
  genderBtnTextActive: {
    color: C.primaryDark,
  },
  goalList: {
    gap: Spacing.sm,
  },
  goalCard: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: C.surface,
    borderRadius: BorderRadius.lg,
    padding: Spacing.md,
    borderWidth: 1.5,
    borderColor: C.divider,
    gap: Spacing.md,
  },
  goalCardActive: {
    borderColor: C.primary,
    backgroundColor: C.primaryLight,
  },
  goalEmoji: {
    fontSize: 28,
    width: 36,
    textAlign: 'center',
  },
  goalText: {
    flex: 1,
  },
  goalTitle: {
    fontSize: FontSize.md,
    fontWeight: FontWeight.semibold,
    color: C.text,
    marginBottom: 2,
  },
  goalTitleActive: {
    color: C.primaryDark,
  },
  goalDesc: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
    lineHeight: 18,
  },
  checkCircle: {
    width: 24,
    height: 24,
    borderRadius: 12,
    backgroundColor: C.primary,
    justifyContent: 'center',
    alignItems: 'center',
  },
  checkMark: {
    color: C.surface,
    fontSize: FontSize.sm,
    fontWeight: FontWeight.bold,
  },
  targetCard: {
    backgroundColor: C.surface,
    borderRadius: BorderRadius.lg,
    padding: Spacing.xl,
    alignItems: 'center',
    marginBottom: Spacing.md,
  },
  calorieRow: {
    alignItems: 'center',
    marginBottom: Spacing.lg,
  },
  calorieValue: {
    fontSize: 36,
    fontWeight: FontWeight.bold,
    color: C.primary,
  },
  calorieLabel: {
    fontSize: FontSize.md,
    color: C.textSecondary,
  },
  macroDivider: {
    width: '80%',
    height: 1,
    backgroundColor: C.divider,
    marginBottom: Spacing.lg,
  },
  macroGrid: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    width: '100%',
  },
  macroItem: {
    alignItems: 'center',
  },
  macroValue: {
    fontSize: FontSize.xxl,
    fontWeight: FontWeight.bold,
  },
  macroLabel: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
    marginTop: 2,
  },
  noteCard: {
    backgroundColor: C.primaryLight,
    borderRadius: BorderRadius.md,
    padding: Spacing.md,
    marginBottom: Spacing.lg,
  },
  noteText: {
    fontSize: FontSize.sm,
    color: C.primaryDark,
    lineHeight: 20,
    textAlign: 'center',
  },
  navButtons: {
    flexDirection: 'row',
    marginTop: Spacing.xl,
    gap: Spacing.sm,
  },
});
