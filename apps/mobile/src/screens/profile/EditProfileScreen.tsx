import React, { useMemo, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  KeyboardAvoidingView,
  Platform,
  Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { useAuthStore } from '@/store/authStore';
import { useProfileStore } from '@/store/profileStore';
import { ActivityLevel, DietaryGoal, Gender } from '@/types';
import { Spacing, FontSize, BorderRadius, FontWeight } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import { describeApiError } from '@/utils/apiError';

const GOALS: Array<{ value: DietaryGoal; label: string }> = [
  { value: DietaryGoal.WeightLoss, label: 'Weight Loss' },
  { value: DietaryGoal.WeightMaintenance, label: 'Maintain' },
  { value: DietaryGoal.WeightGain, label: 'Weight Gain' },
  { value: DietaryGoal.MuscleGain, label: 'Muscle Gain' },
  { value: DietaryGoal.GeneralHealth, label: 'General Health' },
];

const ACTIVITY: Array<{ value: ActivityLevel; label: string }> = [
  { value: ActivityLevel.Sedentary, label: 'Sedentary' },
  { value: ActivityLevel.LightlyActive, label: 'Lightly Active' },
  { value: ActivityLevel.ModeratelyActive, label: 'Moderately Active' },
  { value: ActivityLevel.VeryActive, label: 'Very Active' },
  { value: ActivityLevel.ExtraActive, label: 'Extra Active' },
];

const GENDERS: Array<{ value: Gender; label: string }> = [
  { value: Gender.Male, label: 'Male' },
  { value: Gender.Female, label: 'Female' },
  { value: Gender.Other, label: 'Other' },
];

interface FormErrors {
  heightCm?: string;
  weightKg?: string;
  targetWeightKg?: string;
  age?: string;
}

export function EditProfileScreen() {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const navigation = useNavigation();
  const { user } = useAuthStore();
  const { profile, updateProfile, isLoading } = useProfileStore();

  // Seed from whatever the profile already holds; the screen is only reachable once loaded.
  const [heightCm, setHeightCm] = useState(String(profile?.heightCm ?? ''));
  const [weightKg, setWeightKg] = useState(String(profile?.weightKg ?? ''));
  const [targetWeightKg, setTargetWeightKg] = useState(
    profile?.targetWeightKg != null ? String(profile.targetWeightKg) : '',
  );
  const [age, setAge] = useState(String(profile?.age ?? ''));
  const [gender, setGender] = useState<Gender>(profile?.gender ?? Gender.Other);
  const [activityLevel, setActivityLevel] = useState<ActivityLevel>(
    profile?.activityLevel ?? ActivityLevel.ModeratelyActive,
  );
  const [dietaryGoal, setDietaryGoal] = useState<DietaryGoal>(
    profile?.dietaryGoal ?? DietaryGoal.GeneralHealth,
  );
  const [notes, setNotes] = useState(profile?.notes ?? '');
  const [errors, setErrors] = useState<FormErrors>({});

  const validate = (): boolean => {
    const next: FormErrors = {};
    const h = parseFloat(heightCm);
    const w = parseFloat(weightKg);
    const a = parseInt(age, 10);
    const t = targetWeightKg.trim() ? parseFloat(targetWeightKg) : null;

    if (!heightCm.trim() || isNaN(h) || h < 50 || h > 260) {
      next.heightCm = 'Enter a height between 50 and 260 cm';
    }
    if (!weightKg.trim() || isNaN(w) || w < 20 || w > 500) {
      next.weightKg = 'Enter a weight between 20 and 500 kg';
    }
    if (t !== null && (isNaN(t) || t < 20 || t > 500)) {
      next.targetWeightKg = 'Enter a target between 20 and 500 kg, or leave it blank';
    }
    if (!age.trim() || isNaN(a) || a < 13 || a > 120) {
      next.age = 'Enter an age between 13 and 120';
    }

    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const handleSave = async () => {
    if (!user?.id) return;
    if (!validate()) return;

    try {
      await updateProfile(user.id, {
        heightCm: parseFloat(heightCm),
        weightKg: parseFloat(weightKg),
        targetWeightKg: targetWeightKg.trim() ? parseFloat(targetWeightKg) : undefined,
        age: parseInt(age, 10),
        gender,
        activityLevel,
        dietaryGoal,
        notes: notes.trim() || undefined,
        // Preserve the stored preference; appearance is managed separately on this platform.
        darkMode: profile?.darkMode,
      });
      navigation.goBack();
    } catch (err: unknown) {
      Alert.alert('Could not save', describeApiError(err, 'Please try again.'));
    }
  };

  const bmi = useMemo(() => {
    const h = parseFloat(heightCm) / 100;
    const w = parseFloat(weightKg);
    if (!h || !w || isNaN(h) || isNaN(w)) return null;
    return (w / (h * h)).toFixed(1);
  }, [heightCm, weightKg]);

  function Chips<T extends string>({
    options,
    value,
    onChange,
  }: {
    options: Array<{ value: T; label: string }>;
    value: T;
    onChange: (v: T) => void;
  }) {
    return (
      <View style={styles.chipRow}>
        {options.map((opt) => (
          <TouchableOpacity
            key={opt.value}
            style={[styles.chip, value === opt.value && styles.chipActive]}
            onPress={() => onChange(opt.value)}
          >
            <Text style={[styles.chipText, value === opt.value && styles.chipTextActive]}>
              {opt.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.container} edges={['bottom']}>
      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <ScrollView
          contentContainerStyle={styles.content}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <Text style={styles.sectionTitle}>Body</Text>
          <View style={styles.row}>
            <View style={styles.rowItem}>
              <Input
                label="Height (cm)"
                value={heightCm}
                onChangeText={(t) => {
                  setHeightCm(t);
                  if (errors.heightCm) setErrors((e) => ({ ...e, heightCm: undefined }));
                }}
                error={errors.heightCm}
                keyboardType="numeric"
                placeholder="175"
              />
            </View>
            <View style={styles.rowItem}>
              <Input
                label="Age"
                value={age}
                onChangeText={(t) => {
                  setAge(t);
                  if (errors.age) setErrors((e) => ({ ...e, age: undefined }));
                }}
                error={errors.age}
                keyboardType="numeric"
                placeholder="30"
              />
            </View>
          </View>

          <View style={styles.row}>
            <View style={styles.rowItem}>
              <Input
                label="Weight (kg)"
                value={weightKg}
                onChangeText={(t) => {
                  setWeightKg(t);
                  if (errors.weightKg) setErrors((e) => ({ ...e, weightKg: undefined }));
                }}
                error={errors.weightKg}
                keyboardType="numeric"
                placeholder="70"
              />
            </View>
            <View style={styles.rowItem}>
              <Input
                label="Target (kg)"
                value={targetWeightKg}
                onChangeText={(t) => {
                  setTargetWeightKg(t);
                  if (errors.targetWeightKg) {
                    setErrors((e) => ({ ...e, targetWeightKg: undefined }));
                  }
                }}
                error={errors.targetWeightKg}
                keyboardType="numeric"
                placeholder="Optional"
              />
            </View>
          </View>

          {bmi && (
            <Text style={styles.hint}>
              BMI at these numbers: <Text style={styles.hintStrong}>{bmi}</Text>
            </Text>
          )}

          <Text style={styles.sectionTitle}>Gender</Text>
          <Chips options={GENDERS} value={gender} onChange={setGender} />

          <Text style={styles.sectionTitle}>Activity Level</Text>
          <Chips options={ACTIVITY} value={activityLevel} onChange={setActivityLevel} />

          <Text style={styles.sectionTitle}>Dietary Goal</Text>
          <Chips options={GOALS} value={dietaryGoal} onChange={setDietaryGoal} />

          <Text style={styles.sectionTitle}>Notes</Text>
          <Input
            label="Allergies, preferences, anything else"
            value={notes}
            onChangeText={setNotes}
            placeholder="e.g. no shellfish, prefers spicy food"
            multiline
          />

          <Text style={styles.footnote}>
            Changing your body stats or goal recalculates your daily calorie and macro targets.
          </Text>

          <View style={styles.actions}>
            <Button
              title="Cancel"
              variant="outline"
              size="lg"
              fullWidth
              onPress={() => navigation.goBack()}
            />
            <Button
              title="Save Changes"
              size="lg"
              fullWidth
              loading={isLoading}
              onPress={handleSave}
            />
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) =>
  StyleSheet.create({
    container: { flex: 1, backgroundColor: C.background },
    flex: { flex: 1 },
    content: { padding: Spacing.lg, paddingBottom: Spacing.xxl },
    sectionTitle: {
      fontSize: FontSize.lg,
      fontWeight: FontWeight.bold,
      color: C.text,
      marginTop: Spacing.lg,
      marginBottom: Spacing.sm,
    },
    row: { flexDirection: 'row', gap: Spacing.md },
    rowItem: { flex: 1 },
    hint: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: -Spacing.xs },
    hintStrong: { color: C.text, fontWeight: FontWeight.semibold },
    chipRow: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.sm },
    chip: {
      borderWidth: 1,
      borderColor: C.divider,
      backgroundColor: C.surface,
      borderRadius: BorderRadius.full,
      paddingHorizontal: Spacing.md,
      paddingVertical: Spacing.sm,
    },
    chipActive: { backgroundColor: C.primary, borderColor: C.primary },
    chipText: { fontSize: FontSize.sm, color: C.textSecondary, fontWeight: FontWeight.medium },
    chipTextActive: { color: '#FFFFFF' },
    footnote: {
      fontSize: FontSize.sm,
      color: C.textSecondary,
      marginTop: Spacing.lg,
      lineHeight: 20,
    },
    actions: { gap: Spacing.sm, marginTop: Spacing.lg },
  });
