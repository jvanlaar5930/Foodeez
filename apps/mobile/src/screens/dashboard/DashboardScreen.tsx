import React, { useCallback, useEffect, useState } from 'react';
import {
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import type { BottomTabNavigationProp } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import { CalorieRing } from './components/CalorieRing';
import { MacroProgress } from './components/MacroProgress';
import { Card } from '@/components/ui/Card';
import { MealSection } from '@/components/meal/MealSection';
import { useAuthStore } from '@/store/authStore';
import { useMealStore } from '@/store/mealStore';
import { useNutrition } from '@/hooks/useNutrition';
import { Colors, FontSize, FontWeight, Spacing } from '@/constants/theme';
import { formatDisplayDate, formatApiDate, getGreeting } from '@/utils/dateUtils';
import type { MainTabParamList } from '@/navigation/types';

type DashboardNav = BottomTabNavigationProp<MainTabParamList, 'Dashboard'>;

export function DashboardScreen() {
  const navigation = useNavigation<DashboardNav>();
  const user = useAuthStore((state) => state.user);
  const { dailyLogs, nutritionSummary, isLoading, fetchDailyLogs, fetchNutritionSummary } =
    useMealStore();
  const { macrosSummary } = useNutrition();
  const [refreshing, setRefreshing] = useState(false);

  const today = formatApiDate(new Date());
  const displayDate = formatDisplayDate(new Date());
  const greeting = getGreeting();

  const loadData = useCallback(async () => {
    if (!user) return;
    await Promise.all([
      fetchDailyLogs(user.id, today),
      fetchNutritionSummary(user.id, today),
    ]);
  }, [user, today, fetchDailyLogs, fetchNutritionSummary]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    await loadData();
    setRefreshing(false);
  }, [loadData]);

  const consumed = nutritionSummary?.totalCalories ?? 0;
  const target = nutritionSummary?.targetCalories ?? 2000;

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <ScrollView
        style={styles.scroll}
        contentContainerStyle={styles.content}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={[Colors.primary]} />
        }
      >
        {/* Header */}
        <View style={styles.header}>
          <View>
            <Text style={styles.greeting}>
              Good {greeting},{'\n'}
              <Text style={styles.firstName}>{user?.firstName ?? 'there'}!</Text>
            </Text>
            <Text style={styles.date}>{displayDate}</Text>
          </View>
          <TouchableOpacity style={styles.avatarCircle}>
            <Text style={styles.avatarText}>
              {user?.firstName?.[0] ?? '?'}
              {user?.lastName?.[0] ?? ''}
            </Text>
          </TouchableOpacity>
        </View>

        {/* Calorie Ring */}
        <Card style={styles.ringCard}>
          <Text style={styles.sectionTitle}>Daily Calories</Text>
          <View style={styles.ringContainer}>
            <CalorieRing consumed={consumed} target={target} />
          </View>
          <MacroProgress
            protein={macrosSummary.protein}
            carbs={macrosSummary.carbs}
            fat={macrosSummary.fat}
          />
        </Card>

        {/* Quick Actions */}
        <View style={styles.quickActions}>
          <TouchableOpacity
            style={styles.quickAction}
            onPress={() => navigation.navigate('MealLog')}
          >
            <View style={[styles.quickActionIcon, { backgroundColor: Colors.primaryLight }]}>
              <Ionicons name="add" size={24} color={Colors.primary} />
            </View>
            <Text style={styles.quickActionLabel}>Log Meal</Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={styles.quickAction}
            onPress={() => navigation.navigate('MealLog')}
          >
            <View style={[styles.quickActionIcon, { backgroundColor: '#FFF3E0' }]}>
              <Ionicons name="camera-outline" size={24} color={Colors.secondary} />
            </View>
            <Text style={styles.quickActionLabel}>Scan Food</Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={styles.quickAction}
            onPress={() => navigation.navigate('MealPlan')}
          >
            <View style={[styles.quickActionIcon, { backgroundColor: '#E3F2FD' }]}>
              <Ionicons name="calendar-outline" size={24} color={Colors.info} />
            </View>
            <Text style={styles.quickActionLabel}>View Plan</Text>
          </TouchableOpacity>
        </View>

        {/* AI Tip */}
        <View style={styles.tipCard}>
          <View style={styles.tipHeader}>
            <Ionicons name="bulb-outline" size={20} color={Colors.surface} />
            <Text style={styles.tipTitle}>AI Tip</Text>
          </View>
          <Text style={styles.tipText}>
            Eat more leafy greens for iron! Spinach and kale are excellent sources that boost energy
            and support red blood cell production.
          </Text>
        </View>

        {/* Today's Meals */}
        <Text style={styles.sectionTitle}>Today's Meals</Text>
        {dailyLogs.length === 0 ? (
          <Card style={styles.emptyMeals}>
            <View style={styles.emptyMealsContent}>
              <Ionicons name="restaurant-outline" size={40} color={Colors.textHint} />
              <Text style={styles.emptyMealsText}>No meals logged today</Text>
              <Text style={styles.emptyMealsHint}>Tap "Log Meal" to get started</Text>
            </View>
          </Card>
        ) : (
          dailyLogs.map((log) => (
            <MealSection key={log.id} mealLog={log} />
          ))
        )}

        <View style={styles.bottomPadding} />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: Colors.background,
  },
  scroll: {
    flex: 1,
  },
  content: {
    padding: Spacing.md,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: Spacing.lg,
  },
  greeting: {
    fontSize: FontSize.lg,
    color: Colors.textSecondary,
  },
  firstName: {
    fontSize: FontSize.xxl,
    fontWeight: FontWeight.bold,
    color: Colors.text,
  },
  date: {
    fontSize: FontSize.sm,
    color: Colors.textSecondary,
    marginTop: Spacing.xs,
  },
  avatarCircle: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: Colors.primary,
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatarText: {
    color: Colors.surface,
    fontWeight: FontWeight.bold,
    fontSize: FontSize.md,
  },
  ringCard: {
    marginBottom: Spacing.md,
    padding: 0,
    overflow: 'hidden',
  },
  ringContainer: {
    alignItems: 'center',
    paddingVertical: Spacing.lg,
  },
  sectionTitle: {
    fontSize: FontSize.lg,
    fontWeight: FontWeight.semibold,
    color: Colors.text,
    padding: Spacing.md,
    paddingBottom: Spacing.sm,
  },
  quickActions: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    marginBottom: Spacing.md,
  },
  quickAction: {
    alignItems: 'center',
    flex: 1,
    gap: Spacing.xs,
  },
  quickActionIcon: {
    width: 60,
    height: 60,
    borderRadius: 16,
    justifyContent: 'center',
    alignItems: 'center',
  },
  quickActionLabel: {
    fontSize: FontSize.sm,
    color: Colors.text,
    fontWeight: FontWeight.medium,
  },
  tipCard: {
    backgroundColor: Colors.primary,
    borderRadius: 12,
    padding: Spacing.md,
    marginBottom: Spacing.md,
  },
  tipHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    marginBottom: Spacing.xs,
  },
  tipTitle: {
    color: Colors.surface,
    fontWeight: FontWeight.semibold,
    fontSize: FontSize.md,
  },
  tipText: {
    color: Colors.surface,
    fontSize: FontSize.sm,
    lineHeight: 20,
    opacity: 0.9,
  },
  emptyMeals: {
    marginBottom: Spacing.md,
  },
  emptyMealsContent: {
    alignItems: 'center',
    paddingVertical: Spacing.xl,
    gap: Spacing.sm,
  },
  emptyMealsText: {
    fontSize: FontSize.lg,
    fontWeight: FontWeight.medium,
    color: Colors.textSecondary,
  },
  emptyMealsHint: {
    fontSize: FontSize.sm,
    color: Colors.textHint,
  },
  bottomPadding: {
    height: Spacing.xl,
  },
});
