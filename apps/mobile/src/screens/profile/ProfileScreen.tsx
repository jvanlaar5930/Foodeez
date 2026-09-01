import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Switch,
  TextInput,
  Alert,
  Modal,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';
import { format } from 'date-fns';
import { useAuthStore } from '@/store/authStore';
import { useProfileStore } from '@/store/profileStore';
import { ActivityLevel, DietaryGoal, UnitSystem } from '@/types';
import { Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';
import { useTheme, useThemedStyles, useThemeMode, type Palette } from '@/theme';
import { formatHeight, formatWeight } from '@/utils/units';
import { describeApiError } from '@/utils/apiError';
import type { ProfileStackParamList } from '@/navigation/types';

const GOAL_LABELS: Record<DietaryGoal, string> = {
  [DietaryGoal.WeightLoss]: 'Weight Loss',
  [DietaryGoal.WeightMaintenance]: 'Maintain Weight',
  [DietaryGoal.WeightGain]: 'Weight Gain',
  [DietaryGoal.MuscleGain]: 'Muscle Gain',
  [DietaryGoal.GeneralHealth]: 'General Health',
};

const ACTIVITY_LABELS: Record<ActivityLevel, string> = {
  [ActivityLevel.Sedentary]: 'Sedentary',
  [ActivityLevel.LightlyActive]: 'Lightly Active',
  [ActivityLevel.ModeratelyActive]: 'Moderately Active',
  [ActivityLevel.VeryActive]: 'Very Active',
  [ActivityLevel.ExtraActive]: 'Extra Active',
};

type ProfileNav = NativeStackNavigationProp<ProfileStackParamList, 'ProfileHome'>;

const THEME_OPTIONS: Array<{ value: 'light' | 'dark' | 'system'; label: string; icon: string }> = [
  { value: 'light', label: 'Light', icon: 'sunny-outline' },
  { value: 'dark', label: 'Dark', icon: 'moon-outline' },
  { value: 'system', label: 'System', icon: 'phone-portrait-outline' },
];

export function ProfileScreen() {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const navigation = useNavigation<ProfileNav>();
  const { mode, setMode } = useThemeMode();
  const { user, logout } = useAuthStore();
  const { profile, fetchProfile, updateProfile } = useProfileStore();
  const units = profile?.unitSystem ?? UnitSystem.Metric;
  const [weightReminderEnabled, setWeightReminderEnabled] = useState(true);
  const [trackingReminderEnabled, setTrackingReminderEnabled] = useState(true);
  const [showLogWeightModal, setShowLogWeightModal] = useState(false);
  const [weightInput, setWeightInput] = useState('');

  useEffect(() => {
    if (user?.id) fetchProfile(user.id);
  }, [user?.id]);

  // The unit system lives on the profile so every client agrees on it. Everything else in
  // the update has to be resent, since the endpoint replaces the whole profile.
  const handleUnitChange = async (next: UnitSystem) => {
    if (!user?.id || !profile || profile.unitSystem === next) return;
    try {
      await updateProfile(user.id, {
        heightCm: profile.heightCm,
        weightKg: profile.weightKg,
        targetWeightKg: profile.targetWeightKg,
        age: profile.age,
        gender: profile.gender,
        activityLevel: profile.activityLevel,
        dietaryGoal: profile.dietaryGoal,
        notes: profile.notes,
        darkMode: profile.darkMode,
        unitSystem: next,
      });
    } catch (err: unknown) {
      Alert.alert('Could not save', describeApiError(err, 'Please try again.'));
    }
  };

  const handleLogout = () => {
    Alert.alert('Sign Out', 'Are you sure you want to sign out?', [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Sign Out', style: 'destructive', onPress: logout },
    ]);
  };

  const handleLogWeight = () => {
    if (!weightInput || isNaN(parseFloat(weightInput))) {
      Alert.alert('Invalid Input', 'Please enter a valid weight.');
      return;
    }
    // TODO: call API to log weight entry
    Alert.alert('Weight Logged', `${weightInput} kg recorded for today.`);
    setWeightInput('');
    setShowLogWeightModal(false);
  };

  const initials = user ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase() : '??';

  const bmi = profile
    ? (profile.weightKg / Math.pow(profile.heightCm / 100, 2)).toFixed(1)
    : null;

  return (
    <SafeAreaView style={styles.container} edges={['top']}>
      <ScrollView contentContainerStyle={styles.scrollContent} showsVerticalScrollIndicator={false}>
        {/* Profile header */}
        <View style={styles.profileHeader}>
          <View style={styles.avatar}>
            <Text style={styles.avatarText}>{initials}</Text>
          </View>
          <Text style={styles.profileName}>{user?.firstName} {user?.lastName}</Text>
          <Text style={styles.profileEmail}>{user?.email}</Text>
          <TouchableOpacity style={styles.editButton} onPress={() => navigation.navigate('EditProfile')}>
            <Ionicons name="create-outline" size={16} color={C.primary} />
            <Text style={styles.editButtonText}>Edit Profile</Text>
          </TouchableOpacity>
        </View>

        {/* Stats */}
        {profile && (
          <View style={styles.statsGrid}>
            <View style={styles.statCard}>
              <Text style={styles.statValue}>{formatWeight(profile.weightKg, units)}</Text>
              <Text style={styles.statLabel}>Current Weight</Text>
            </View>
            <View style={styles.statCard}>
              <Text style={styles.statValue}>{profile.targetWeightKg ? formatWeight(profile.targetWeightKg, units) : '—'}</Text>
              <Text style={styles.statLabel}>Target Weight</Text>
            </View>
            <View style={styles.statCard}>
              <Text style={styles.statValue}>{profile.dailyCalorieTarget}</Text>
              <Text style={styles.statLabel}>Daily Calories</Text>
            </View>
            <View style={styles.statCard}>
              <Text style={styles.statValue}>{bmi ?? '—'}</Text>
              <Text style={styles.statLabel}>BMI</Text>
            </View>
          </View>
        )}

        {/* Goals */}
        {profile && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Your Goals</Text>
            <View style={styles.goalCard}>
              <View style={styles.goalRow}>
                <Ionicons name="flag-outline" size={18} color={C.primary} />
                <Text style={styles.goalLabel}>Dietary Goal</Text>
                <Text style={styles.goalValue}>{GOAL_LABELS[profile.dietaryGoal]}</Text>
              </View>
              <View style={styles.goalDivider} />
              <View style={styles.goalRow}>
                <Ionicons name="fitness-outline" size={18} color={C.primary} />
                <Text style={styles.goalLabel}>Activity Level</Text>
                <Text style={styles.goalValue}>{ACTIVITY_LABELS[profile.activityLevel]}</Text>
              </View>
              <View style={styles.goalDivider} />
              <View style={styles.goalRow}>
                <Ionicons name="body-outline" size={18} color={C.primary} />
                <Text style={styles.goalLabel}>Height</Text>
                <Text style={styles.goalValue}>{formatHeight(profile.heightCm, units)}</Text>
              </View>
            </View>
          </View>
        )}

        {/* Macro targets */}
        {profile && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Daily Macro Targets</Text>
            <View style={styles.macroRow}>
              <View style={[styles.macroChip, { backgroundColor: '#E3F2FD' }]}>
                <Text style={[styles.macroValue, { color: '#1565C0' }]}>{Math.round(profile.dailyProteinTargetG)}g</Text>
                <Text style={[styles.macroLabel, { color: '#1565C0' }]}>Protein</Text>
              </View>
              <View style={[styles.macroChip, { backgroundColor: '#FFF3E0' }]}>
                <Text style={[styles.macroValue, { color: '#E65100' }]}>{Math.round(profile.dailyCarbTargetG)}g</Text>
                <Text style={[styles.macroLabel, { color: '#E65100' }]}>Carbs</Text>
              </View>
              <View style={[styles.macroChip, { backgroundColor: '#FFFDE7' }]}>
                <Text style={[styles.macroValue, { color: '#F57F17' }]}>{Math.round(profile.dailyFatTargetG)}g</Text>
                <Text style={[styles.macroLabel, { color: '#F57F17' }]}>Fat</Text>
              </View>
            </View>
          </View>
        )}

        {/* Weight log */}
        <View style={styles.section}>
          <View style={styles.sectionHeaderRow}>
            <Text style={styles.sectionTitle}>Weight History</Text>
            <TouchableOpacity style={styles.logWeightButton} onPress={() => setShowLogWeightModal(true)}>
              <Ionicons name="add" size={16} color={C.surface} />
              <Text style={styles.logWeightButtonText}>Log Weight</Text>
            </TouchableOpacity>
          </View>
          <View style={styles.emptyHistory}>
            <Ionicons name="scale-outline" size={32} color={C.textHint} />
            <Text style={styles.emptyHistoryText}>No weight entries yet</Text>
          </View>
        </View>

        {/* Appearance */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Appearance</Text>
          <View style={styles.segmented}>
            {THEME_OPTIONS.map((opt) => (
              <TouchableOpacity
                key={opt.value}
                style={[styles.segment, mode === opt.value && styles.segmentActive]}
                onPress={() => setMode(opt.value)}
                accessibilityRole="button"
                accessibilityState={{ selected: mode === opt.value }}
              >
                <Ionicons
                  name={opt.icon as never}
                  size={16}
                  color={mode === opt.value ? '#FFFFFF' : C.textSecondary}
                />
                <Text style={[styles.segmentText, mode === opt.value && styles.segmentTextActive]}>
                  {opt.label}
                </Text>
              </TouchableOpacity>
            ))}
          </View>
        </View>

        {/* Units */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Units</Text>
          <View style={styles.segmented}>
            {[
              { value: UnitSystem.Metric, label: 'Metric', hint: 'kg, cm, ml' },
              { value: UnitSystem.US, label: 'US', hint: 'lb, ft/in, fl oz' },
            ].map((opt) => (
              <TouchableOpacity
                key={opt.value}
                style={[styles.segment, units === opt.value && styles.segmentActive]}
                onPress={() => handleUnitChange(opt.value)}
                accessibilityRole="button"
                accessibilityState={{ selected: units === opt.value }}
              >
                <Text style={[styles.segmentText, units === opt.value && styles.segmentTextActive]}>
                  {opt.label}
                </Text>
                <Text
                  style={[styles.segmentHint, units === opt.value && styles.segmentTextActive]}
                >
                  {opt.hint}
                </Text>
              </TouchableOpacity>
            ))}
          </View>
          <Text style={styles.settingDesc}>
            Applies across the app and on the web. Nutrition stays in grams and kcal either way.
          </Text>
        </View>

        {/* Notifications */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Notifications</Text>
          <View style={styles.settingCard}>
            <View style={styles.settingRow}>
              <View style={styles.settingInfo}>
                <Text style={styles.settingName}>Weekly Weight Check-in</Text>
                <Text style={styles.settingDesc}>Remind me to log my weight</Text>
              </View>
              <Switch
                value={weightReminderEnabled}
                onValueChange={setWeightReminderEnabled}
                trackColor={{ false: C.divider, true: C.primaryLight }}
                thumbColor={weightReminderEnabled ? C.primary : C.textHint}
              />
            </View>
            <View style={styles.settingDivider} />
            <View style={styles.settingRow}>
              <View style={styles.settingInfo}>
                <Text style={styles.settingName}>Meal Tracking Reminders</Text>
                <Text style={styles.settingDesc}>Alert me if I haven't logged meals</Text>
              </View>
              <Switch
                value={trackingReminderEnabled}
                onValueChange={setTrackingReminderEnabled}
                trackColor={{ false: C.divider, true: C.primaryLight }}
                thumbColor={trackingReminderEnabled ? C.primary : C.textHint}
              />
            </View>
          </View>
        </View>

        {/* Sign out */}
        <TouchableOpacity style={styles.signOutButton} onPress={handleLogout}>
          <Ionicons name="log-out-outline" size={20} color={C.error} />
          <Text style={styles.signOutText}>Sign Out</Text>
        </TouchableOpacity>
      </ScrollView>

      {/* Log weight modal */}
      <Modal visible={showLogWeightModal} transparent animationType="slide">
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>Log Today's Weight</Text>
            <TextInput
              style={styles.weightInput}
              placeholder="Enter weight in kg"
              keyboardType="decimal-pad"
              value={weightInput}
              onChangeText={setWeightInput}
              placeholderTextColor={C.textHint}
            />
            <Text style={styles.modalDate}>{format(new Date(), 'MMMM d, yyyy')}</Text>
            <View style={styles.modalActions}>
              <TouchableOpacity style={styles.modalCancel} onPress={() => setShowLogWeightModal(false)}>
                <Text style={styles.modalCancelText}>Cancel</Text>
              </TouchableOpacity>
              <TouchableOpacity style={styles.modalConfirm} onPress={handleLogWeight}>
                <Text style={styles.modalConfirmText}>Save</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  container: { flex: 1, backgroundColor: C.background },
  scrollContent: { paddingBottom: Spacing.xxl },
  profileHeader: {
    alignItems: 'center',
    backgroundColor: C.surface,
    paddingVertical: Spacing.xl,
    marginBottom: Spacing.md,
    ...Shadows.sm,
  },
  avatar: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: C.primary,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: Spacing.sm,
  },
  avatarText: { color: C.surface, fontSize: FontSize.xxl, fontWeight: FontWeight.bold },
  profileName: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: C.text },
  profileEmail: { fontSize: FontSize.md, color: C.textSecondary, marginTop: 2 },
  editButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    marginTop: Spacing.md,
    borderWidth: 1,
    borderColor: C.primary,
    borderRadius: BorderRadius.full,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
  },
  editButtonText: { color: C.primary, fontWeight: FontWeight.medium },
  statsGrid: { flexDirection: 'row', flexWrap: 'wrap', padding: Spacing.md, gap: Spacing.sm },
  statCard: {
    width: '47%',
    backgroundColor: C.surface,
    borderRadius: BorderRadius.lg,
    padding: Spacing.md,
    alignItems: 'center',
    ...Shadows.sm,
  },
  statValue: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: C.text },
  statLabel: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2 },
  section: { paddingHorizontal: Spacing.md, marginBottom: Spacing.lg },
  sectionHeaderRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: Spacing.md },
  sectionTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: C.text, marginBottom: Spacing.md },
  goalCard: { backgroundColor: C.surface, borderRadius: BorderRadius.lg, overflow: 'hidden', ...Shadows.sm },
  goalRow: { flexDirection: 'row', alignItems: 'center', padding: Spacing.md, gap: Spacing.sm },
  goalLabel: { flex: 1, fontSize: FontSize.md, color: C.textSecondary },
  goalValue: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: C.text },
  goalDivider: { height: 1, backgroundColor: C.divider, marginHorizontal: Spacing.md },
  macroRow: { flexDirection: 'row', gap: Spacing.md },
  macroChip: { flex: 1, borderRadius: BorderRadius.lg, padding: Spacing.md, alignItems: 'center' },
  macroValue: { fontSize: FontSize.xl, fontWeight: FontWeight.bold },
  macroLabel: { fontSize: FontSize.sm, fontWeight: FontWeight.medium },
  logWeightButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    backgroundColor: C.primary,
    borderRadius: BorderRadius.full,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
  },
  logWeightButtonText: { color: C.surface, fontWeight: FontWeight.medium, fontSize: FontSize.sm },
  emptyHistory: { backgroundColor: C.surface, borderRadius: BorderRadius.lg, padding: Spacing.xl, alignItems: 'center', gap: Spacing.sm, ...Shadows.sm },
  emptyHistoryText: { color: C.textSecondary },
  settingCard: { backgroundColor: C.surface, borderRadius: BorderRadius.lg, overflow: 'hidden', ...Shadows.sm },
  settingRow: { flexDirection: 'row', alignItems: 'center', padding: Spacing.md },
  settingInfo: { flex: 1 },
  settingName: { fontSize: FontSize.md, fontWeight: FontWeight.medium, color: C.text },
  settingDesc: { fontSize: FontSize.sm, color: C.textSecondary, marginTop: 2 },
  settingDivider: { height: 1, backgroundColor: C.divider, marginHorizontal: Spacing.md },
  segmented: {
    flexDirection: 'row',
    gap: Spacing.sm,
  },
  segment: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 2,
    paddingVertical: Spacing.sm,
    borderRadius: BorderRadius.md,
    borderWidth: 1,
    borderColor: C.divider,
    backgroundColor: C.surface,
  },
  segmentActive: { backgroundColor: C.primary, borderColor: C.primary },
  segmentText: { fontSize: FontSize.sm, color: C.textSecondary, fontWeight: FontWeight.medium },
  segmentHint: { fontSize: FontSize.xs, color: C.textHint },
  segmentTextActive: { color: '#FFFFFF' },
  signOutButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.sm,
    marginHorizontal: Spacing.md,
    marginTop: Spacing.sm,
    borderWidth: 2,
    borderColor: C.error,
    borderRadius: BorderRadius.lg,
    paddingVertical: Spacing.md,
  },
  signOutText: { color: C.error, fontWeight: FontWeight.semibold, fontSize: FontSize.md },
  modalOverlay: { flex: 1, backgroundColor: C.overlay, justifyContent: 'flex-end' },
  modalContent: { backgroundColor: C.surface, borderTopLeftRadius: BorderRadius.xl, borderTopRightRadius: BorderRadius.xl, padding: Spacing.xl },
  modalTitle: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: C.text, marginBottom: Spacing.md },
  weightInput: {
    borderWidth: 1,
    borderColor: C.divider,
    borderRadius: BorderRadius.lg,
    padding: Spacing.md,
    fontSize: FontSize.lg,
    color: C.text,
    marginBottom: Spacing.sm,
  },
  modalDate: { fontSize: FontSize.sm, color: C.textSecondary, marginBottom: Spacing.lg },
  modalActions: { flexDirection: 'row', gap: Spacing.md },
  modalCancel: { flex: 1, borderWidth: 1, borderColor: C.divider, borderRadius: BorderRadius.lg, paddingVertical: Spacing.md, alignItems: 'center' },
  modalCancelText: { color: C.textSecondary, fontWeight: FontWeight.semibold },
  modalConfirm: { flex: 1, backgroundColor: C.primary, borderRadius: BorderRadius.lg, paddingVertical: Spacing.md, alignItems: 'center' },
  modalConfirmText: { color: C.surface, fontWeight: FontWeight.semibold },
});
