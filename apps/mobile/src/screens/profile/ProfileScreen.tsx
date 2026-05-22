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
import { Ionicons } from '@expo/vector-icons';
import { format } from 'date-fns';
import { useAuthStore } from '@/store/authStore';
import { useProfileStore } from '@/store/profileStore';
import { ActivityLevel, DietaryGoal } from '@/types';
import { Colors, Spacing, FontSize, BorderRadius, FontWeight, Shadows } from '@/constants/theme';

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

export function ProfileScreen() {
  const { user, logout } = useAuthStore();
  const { profile, fetchProfile } = useProfileStore();
  const [weightReminderEnabled, setWeightReminderEnabled] = useState(true);
  const [trackingReminderEnabled, setTrackingReminderEnabled] = useState(true);
  const [showLogWeightModal, setShowLogWeightModal] = useState(false);
  const [weightInput, setWeightInput] = useState('');

  useEffect(() => {
    if (user?.id) fetchProfile(user.id);
  }, [user?.id]);

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
          <TouchableOpacity style={styles.editButton}>
            <Ionicons name="create-outline" size={16} color={Colors.primary} />
            <Text style={styles.editButtonText}>Edit Profile</Text>
          </TouchableOpacity>
        </View>

        {/* Stats */}
        {profile && (
          <View style={styles.statsGrid}>
            <View style={styles.statCard}>
              <Text style={styles.statValue}>{profile.weightKg} kg</Text>
              <Text style={styles.statLabel}>Current Weight</Text>
            </View>
            <View style={styles.statCard}>
              <Text style={styles.statValue}>{profile.targetWeightKg ? `${profile.targetWeightKg} kg` : '—'}</Text>
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
                <Ionicons name="flag-outline" size={18} color={Colors.primary} />
                <Text style={styles.goalLabel}>Dietary Goal</Text>
                <Text style={styles.goalValue}>{GOAL_LABELS[profile.dietaryGoal]}</Text>
              </View>
              <View style={styles.goalDivider} />
              <View style={styles.goalRow}>
                <Ionicons name="fitness-outline" size={18} color={Colors.primary} />
                <Text style={styles.goalLabel}>Activity Level</Text>
                <Text style={styles.goalValue}>{ACTIVITY_LABELS[profile.activityLevel]}</Text>
              </View>
              <View style={styles.goalDivider} />
              <View style={styles.goalRow}>
                <Ionicons name="body-outline" size={18} color={Colors.primary} />
                <Text style={styles.goalLabel}>Height</Text>
                <Text style={styles.goalValue}>{profile.heightCm} cm</Text>
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
              <Ionicons name="add" size={16} color={Colors.surface} />
              <Text style={styles.logWeightButtonText}>Log Weight</Text>
            </TouchableOpacity>
          </View>
          <View style={styles.emptyHistory}>
            <Ionicons name="scale-outline" size={32} color={Colors.textHint} />
            <Text style={styles.emptyHistoryText}>No weight entries yet</Text>
          </View>
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
                trackColor={{ false: Colors.divider, true: Colors.primaryLight }}
                thumbColor={weightReminderEnabled ? Colors.primary : Colors.textHint}
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
                trackColor={{ false: Colors.divider, true: Colors.primaryLight }}
                thumbColor={trackingReminderEnabled ? Colors.primary : Colors.textHint}
              />
            </View>
          </View>
        </View>

        {/* Sign out */}
        <TouchableOpacity style={styles.signOutButton} onPress={handleLogout}>
          <Ionicons name="log-out-outline" size={20} color={Colors.error} />
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
              placeholderTextColor={Colors.textHint}
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

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: Colors.background },
  scrollContent: { paddingBottom: Spacing.xxl },
  profileHeader: {
    alignItems: 'center',
    backgroundColor: Colors.surface,
    paddingVertical: Spacing.xl,
    marginBottom: Spacing.md,
    ...Shadows.sm,
  },
  avatar: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: Colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: Spacing.sm,
  },
  avatarText: { color: Colors.surface, fontSize: FontSize.xxl, fontWeight: FontWeight.bold },
  profileName: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: Colors.text },
  profileEmail: { fontSize: FontSize.md, color: Colors.textSecondary, marginTop: 2 },
  editButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    marginTop: Spacing.md,
    borderWidth: 1,
    borderColor: Colors.primary,
    borderRadius: BorderRadius.full,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
  },
  editButtonText: { color: Colors.primary, fontWeight: FontWeight.medium },
  statsGrid: { flexDirection: 'row', flexWrap: 'wrap', padding: Spacing.md, gap: Spacing.sm },
  statCard: {
    width: '47%',
    backgroundColor: Colors.surface,
    borderRadius: BorderRadius.lg,
    padding: Spacing.md,
    alignItems: 'center',
    ...Shadows.sm,
  },
  statValue: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: Colors.text },
  statLabel: { fontSize: FontSize.sm, color: Colors.textSecondary, marginTop: 2 },
  section: { paddingHorizontal: Spacing.md, marginBottom: Spacing.lg },
  sectionHeaderRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: Spacing.md },
  sectionTitle: { fontSize: FontSize.lg, fontWeight: FontWeight.bold, color: Colors.text, marginBottom: Spacing.md },
  goalCard: { backgroundColor: Colors.surface, borderRadius: BorderRadius.lg, overflow: 'hidden', ...Shadows.sm },
  goalRow: { flexDirection: 'row', alignItems: 'center', padding: Spacing.md, gap: Spacing.sm },
  goalLabel: { flex: 1, fontSize: FontSize.md, color: Colors.textSecondary },
  goalValue: { fontSize: FontSize.md, fontWeight: FontWeight.semibold, color: Colors.text },
  goalDivider: { height: 1, backgroundColor: Colors.divider, marginHorizontal: Spacing.md },
  macroRow: { flexDirection: 'row', gap: Spacing.md },
  macroChip: { flex: 1, borderRadius: BorderRadius.lg, padding: Spacing.md, alignItems: 'center' },
  macroValue: { fontSize: FontSize.xl, fontWeight: FontWeight.bold },
  macroLabel: { fontSize: FontSize.sm, fontWeight: FontWeight.medium },
  logWeightButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    backgroundColor: Colors.primary,
    borderRadius: BorderRadius.full,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
  },
  logWeightButtonText: { color: Colors.surface, fontWeight: FontWeight.medium, fontSize: FontSize.sm },
  emptyHistory: { backgroundColor: Colors.surface, borderRadius: BorderRadius.lg, padding: Spacing.xl, alignItems: 'center', gap: Spacing.sm, ...Shadows.sm },
  emptyHistoryText: { color: Colors.textSecondary },
  settingCard: { backgroundColor: Colors.surface, borderRadius: BorderRadius.lg, overflow: 'hidden', ...Shadows.sm },
  settingRow: { flexDirection: 'row', alignItems: 'center', padding: Spacing.md },
  settingInfo: { flex: 1 },
  settingName: { fontSize: FontSize.md, fontWeight: FontWeight.medium, color: Colors.text },
  settingDesc: { fontSize: FontSize.sm, color: Colors.textSecondary, marginTop: 2 },
  settingDivider: { height: 1, backgroundColor: Colors.divider, marginHorizontal: Spacing.md },
  signOutButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.sm,
    marginHorizontal: Spacing.md,
    marginTop: Spacing.sm,
    borderWidth: 2,
    borderColor: Colors.error,
    borderRadius: BorderRadius.lg,
    paddingVertical: Spacing.md,
  },
  signOutText: { color: Colors.error, fontWeight: FontWeight.semibold, fontSize: FontSize.md },
  modalOverlay: { flex: 1, backgroundColor: Colors.overlay, justifyContent: 'flex-end' },
  modalContent: { backgroundColor: Colors.surface, borderTopLeftRadius: BorderRadius.xl, borderTopRightRadius: BorderRadius.xl, padding: Spacing.xl },
  modalTitle: { fontSize: FontSize.xl, fontWeight: FontWeight.bold, color: Colors.text, marginBottom: Spacing.md },
  weightInput: {
    borderWidth: 1,
    borderColor: Colors.divider,
    borderRadius: BorderRadius.lg,
    padding: Spacing.md,
    fontSize: FontSize.lg,
    color: Colors.text,
    marginBottom: Spacing.sm,
  },
  modalDate: { fontSize: FontSize.sm, color: Colors.textSecondary, marginBottom: Spacing.lg },
  modalActions: { flexDirection: 'row', gap: Spacing.md },
  modalCancel: { flex: 1, borderWidth: 1, borderColor: Colors.divider, borderRadius: BorderRadius.lg, paddingVertical: Spacing.md, alignItems: 'center' },
  modalCancelText: { color: Colors.textSecondary, fontWeight: FontWeight.semibold },
  modalConfirm: { flex: 1, backgroundColor: Colors.primary, borderRadius: BorderRadius.lg, paddingVertical: Spacing.md, alignItems: 'center' },
  modalConfirmText: { color: Colors.surface, fontWeight: FontWeight.semibold },
});
