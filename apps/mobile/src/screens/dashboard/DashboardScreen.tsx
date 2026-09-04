import React, { useCallback, useRef, useState } from 'react';
import { RefreshControl, ScrollView, StyleSheet, Text, TextInput, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { BottomTabNavigationProp } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import { CalorieRing } from './components/CalorieRing';
import { MacroProgress } from './components/MacroProgress';
import { Card } from '@/components/ui/Card';
import { MealSection } from '@/components/meal/MealSection';
import { useAuthStore } from '@/store/authStore';
import { useMealStore } from '@/store/mealStore';
import { useNutrition } from '@/hooks/useNutrition';
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import { formatApiDate, formatDisplayDate, getGreeting } from '@/utils/dateUtils';
import type { MainTabParamList } from '@/navigation/types';

type DashboardNav = BottomTabNavigationProp<MainTabParamList, 'Dashboard'>;

export function DashboardScreen() {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const navigation = useNavigation<DashboardNav>();
  const user = useAuthStore((state) => state.user);
  const dailyLogs = useMealStore((state) => state.dailyLogs);
  const nutritionSummary = useMealStore((state) => state.nutritionSummary);
  const isLoading = useMealStore((state) => state.isLoading);
  const refreshDay = useMealStore((state) => state.refreshDay);
  const { macrosSummary } = useNutrition();
  const [refreshing, setRefreshing] = useState(false);
  const [recipeSearch, setRecipeSearch] = useState('');
  const searchRef = useRef<TextInput>(null);

  const today = formatApiDate(new Date());
  const displayDate = formatDisplayDate(new Date());
  const greeting = getGreeting();

  const loadData = useCallback(async () => {
    if (!user) {
      return;
    }

    await refreshDay(user.id, today);
  }, [refreshDay, today, user]);

  useFocusEffect(
    useCallback(() => {
      loadData();
    }, [loadData]),
  );

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    await loadData();
    setRefreshing(false);
  }, [loadData]);

  const consumed = nutritionSummary?.totalCalories ?? 0;
  const target = nutritionSummary?.targetCalories ?? 2000;

  const goToRecipes = (query?: string) => {
    searchRef.current?.blur();
    navigation.navigate('Recipes', {
      screen: 'RecipesList',
      params: { initialSearch: query?.trim() ?? '' },
    });
    setRecipeSearch('');
  };

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <ScrollView
        style={styles.scroll}
        contentContainerStyle={styles.content}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={[C.primary]} />
        }
      >
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

        <TouchableOpacity
          activeOpacity={1}
          style={styles.searchBar}
          onPress={() => searchRef.current?.focus()}
        >
          <Ionicons name="search" size={20} color={C.primary} style={styles.searchIcon} />
          <TextInput
            ref={searchRef}
            style={styles.searchInput}
            placeholder="Search recipes to start meal prepping..."
            placeholderTextColor={C.textHint}
            value={recipeSearch}
            onChangeText={setRecipeSearch}
            returnKeyType="search"
            onSubmitEditing={() => goToRecipes(recipeSearch)}
          />
          {recipeSearch.length > 0 ? (
            <TouchableOpacity onPress={() => setRecipeSearch('')} hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}>
              <Ionicons name="close-circle" size={18} color={C.textSecondary} />
            </TouchableOpacity>
          ) : (
            <TouchableOpacity onPress={() => goToRecipes()} hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}>
              <Ionicons name="arrow-forward-circle" size={20} color={C.primary} />
            </TouchableOpacity>
          )}
        </TouchableOpacity>

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

        <View style={styles.quickActions}>
          <TouchableOpacity style={styles.quickAction} onPress={() => navigation.navigate('MealLog')}>
            <View style={[styles.quickActionIcon, { backgroundColor: C.primaryLight }]}>
              <Ionicons name="add" size={24} color={C.primary} />
            </View>
            <Text style={styles.quickActionLabel}>Log Meal</Text>
          </TouchableOpacity>

          <TouchableOpacity style={styles.quickAction} onPress={() => navigation.navigate('MealLog')}>
            <View style={[styles.quickActionIcon, { backgroundColor: '#FFF3E0' }]}>
              <Ionicons name="camera-outline" size={24} color={C.secondary} />
            </View>
            <Text style={styles.quickActionLabel}>Scan Food</Text>
          </TouchableOpacity>

          <TouchableOpacity style={styles.quickAction} onPress={() => navigation.navigate('MealPlan')}>
            <View style={[styles.quickActionIcon, { backgroundColor: '#E3F2FD' }]}>
              <Ionicons name="calendar-outline" size={24} color={C.info} />
            </View>
            <Text style={styles.quickActionLabel}>View Plan</Text>
          </TouchableOpacity>
        </View>

        <View style={styles.tipCard}>
          <View style={styles.tipHeader}>
            <Ionicons name="bulb-outline" size={20} color={C.onPrimary} />
            <Text style={styles.tipTitle}>AI Tip</Text>
          </View>
          <Text style={styles.tipText}>
            Eat more leafy greens for iron! Spinach and kale are excellent sources that boost energy
            and support red blood cell production.
          </Text>
        </View>

        <Text style={styles.sectionTitle}>Today's Meals</Text>
        {dailyLogs.length === 0 && !isLoading ? (
          <Card style={styles.emptyMeals}>
            <View style={styles.emptyMealsContent}>
              <Ionicons name="restaurant-outline" size={40} color={C.textHint} />
              <Text style={styles.emptyMealsText}>No meals logged today</Text>
              <Text style={styles.emptyMealsHint}>Tap "Log Meal" to get started</Text>
            </View>
          </Card>
        ) : (
          dailyLogs.map((log) => <MealSection key={log.id} mealLog={log} />)
        )}

        <View style={styles.bottomPadding} />
      </ScrollView>
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: C.background,
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
    color: C.textSecondary,
  },
  firstName: {
    fontSize: FontSize.xxl,
    fontWeight: FontWeight.bold,
    color: C.text,
  },
  date: {
    fontSize: FontSize.sm,
    color: C.textSecondary,
    marginTop: Spacing.xs,
  },
  avatarCircle: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: C.primary,
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatarText: {
    color: C.onPrimary,
    fontWeight: FontWeight.bold,
    fontSize: FontSize.md,
  },
  searchBar: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: C.surface,
    borderRadius: 16,
    paddingHorizontal: Spacing.md,
    paddingVertical: 2,
    marginBottom: Spacing.md,
    borderWidth: 1.5,
    borderColor: C.primaryLight,
    shadowColor: C.primary,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08,
    shadowRadius: 8,
    elevation: 3,
    minHeight: 52,
  },
  searchIcon: {
    marginRight: Spacing.sm,
  },
  searchInput: {
    flex: 1,
    fontSize: FontSize.md,
    color: C.text,
    paddingVertical: Spacing.sm,
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
    color: C.text,
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
    color: C.text,
    fontWeight: FontWeight.medium,
  },
  tipCard: {
    backgroundColor: C.primary,
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
    color: C.onPrimary,
    fontWeight: FontWeight.semibold,
    fontSize: FontSize.md,
  },
  tipText: {
    color: C.onPrimary,
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
    color: C.textSecondary,
  },
  emptyMealsHint: {
    fontSize: FontSize.sm,
    color: C.textHint,
  },
  bottomPadding: {
    height: Spacing.xl,
  },
});
