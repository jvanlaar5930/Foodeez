import React, { useCallback, useState } from 'react';
import { Alert, RefreshControl, ScrollView, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';
import { addDays, subDays } from 'date-fns';
import { MealSection } from '@/components/meal/MealSection';
import { DayAnalysisCard } from '@/components/meal/DayAnalysisCard';
import { EmptyState } from '@/components/ui/EmptyState';
import { useAuthStore } from '@/store/authStore';
import { useMealStore } from '@/store/mealStore';
import { FontSize, FontWeight, Spacing } from '@/constants/theme';
import { useTheme, useThemedStyles, type Palette } from '@/theme';
import { formatApiDate, formatDisplayDate, isToday, parseApiDate } from '@/utils/dateUtils';
import type { MealLogStackParamList } from '@/navigation/types';
import type { MealLogDto } from '@/types';

type MealLogNav = NativeStackNavigationProp<MealLogStackParamList, 'MealLogHome'>;

export function MealLogScreen() {
  const C = useTheme();
  const styles = useThemedStyles(makeStyles);
  const navigation = useNavigation<MealLogNav>();
  const user = useAuthStore((state) => state.user);
  const {
    dailyLogs,
    selectedDate,
    isLoading,
    setSelectedDate,
    refreshDay,
    deleteMealLog,
  } = useMealStore();
  const [refreshing, setRefreshing] = useState(false);

  const currentDate = parseApiDate(selectedDate);

  const loadData = useCallback(async () => {
    if (!user) {
      return;
    }

    await refreshDay(user.id, selectedDate);
  }, [refreshDay, selectedDate, user]);

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

  const goToPrevDay = () => {
    const newDate = subDays(currentDate, 1);
    setSelectedDate(formatApiDate(newDate));
  };

  const goToNextDay = () => {
    const newDate = addDays(currentDate, 1);
    if (newDate <= new Date()) {
      setSelectedDate(formatApiDate(newDate));
    }
  };

  const goToToday = () => {
    setSelectedDate(formatApiDate(new Date()));
  };

  const handleEditMeal = (mealLog: MealLogDto) => {
    navigation.navigate('AddMeal', { mealLog });
  };

  const handleDeleteMeal = (mealLog: MealLogDto) => {
    if (!user) {
      return;
    }

    Alert.alert('Delete Meal', `Delete ${mealLog.items.length} item meal log?`, [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Delete',
        style: 'destructive',
        onPress: async () => {
          await deleteMealLog(mealLog.id, user.id);
        },
      },
    ]);
  };

  const displayDate = isToday(currentDate) ? 'Today' : formatDisplayDate(currentDate);
  const canGoNext = !isToday(currentDate);

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <View style={styles.dateNav}>
        <TouchableOpacity style={styles.dateNavBtn} onPress={goToPrevDay}>
          <Ionicons name="chevron-back" size={24} color={C.text} />
        </TouchableOpacity>
        <TouchableOpacity style={styles.dateCenter} onPress={goToToday}>
          <Text style={styles.dateText}>{displayDate}</Text>
          {!isToday(currentDate) && <Text style={styles.dateSub}>Tap to go to today</Text>}
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.dateNavBtn, !canGoNext && styles.dateNavBtnDisabled]}
          onPress={goToNextDay}
          disabled={!canGoNext}
        >
          <Ionicons
            name="chevron-forward"
            size={24}
            color={canGoNext ? C.text : C.textHint}
          />
        </TouchableOpacity>
      </View>

      <ScrollView
        style={styles.scroll}
        contentContainerStyle={styles.content}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={[C.primary]} />
        }
      >
        {dailyLogs.length === 0 && !isLoading ? (
          <EmptyState
            icon="restaurant-outline"
            title="No meals logged"
            description="Start tracking your nutrition by logging your first meal of the day."
            actionLabel="Log a Meal"
            onAction={() => navigation.navigate('AddMeal', {})}
          />
        ) : (
          dailyLogs.map((log) => (
            <MealSection
              key={log.id}
              mealLog={log}
              onEditMeal={handleEditMeal}
              onDeleteMeal={handleDeleteMeal}
            />
          ))
        )}

        {user && dailyLogs.length > 0 && (
          <View style={styles.dayAnalysis}>
            <DayAnalysisCard
              userId={user.id}
              date={selectedDate}
              dailyLogs={dailyLogs}
              isToday={isToday(currentDate)}
            />
          </View>
        )}

        <View style={styles.bottomPadding} />
      </ScrollView>

      <View style={styles.fabContainer}>
        <TouchableOpacity
          style={[styles.fab, styles.fabSecondary]}
          onPress={() => navigation.navigate('FoodScan')}
        >
          <Ionicons name="camera-outline" size={22} color={C.secondary} />
        </TouchableOpacity>
        <TouchableOpacity style={styles.fab} onPress={() => navigation.navigate('AddMeal', {})}>
          <Ionicons name="add" size={28} color={C.onPrimary} />
        </TouchableOpacity>
      </View>
    </SafeAreaView>
  );
}

const makeStyles = (C: Palette) => StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: C.background,
  },
  dateNav: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: C.surface,
    paddingVertical: Spacing.sm,
    paddingHorizontal: Spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: C.divider,
  },
  dateNavBtn: {
    width: 44,
    height: 44,
    justifyContent: 'center',
    alignItems: 'center',
  },
  dateNavBtnDisabled: {
    opacity: 0.3,
  },
  dateCenter: {
    flex: 1,
    alignItems: 'center',
  },
  dateText: {
    fontSize: FontSize.lg,
    fontWeight: FontWeight.semibold,
    color: C.text,
  },
  dateSub: {
    fontSize: FontSize.xs,
    color: C.primary,
    marginTop: 2,
  },
  scroll: {
    flex: 1,
  },
  content: {
    padding: Spacing.md,
    paddingBottom: 100,
    flexGrow: 1,
  },
  fabContainer: {
    position: 'absolute',
    right: Spacing.lg,
    bottom: Spacing.xl,
    flexDirection: 'row',
    gap: Spacing.sm,
    alignItems: 'flex-end',
  },
  fab: {
    width: 60,
    height: 60,
    borderRadius: 30,
    backgroundColor: C.primary,
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 3 },
    shadowOpacity: 0.2,
    shadowRadius: 6,
    elevation: 6,
  },
  fabSecondary: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: C.surface,
    borderWidth: 1.5,
    borderColor: C.secondary,
  },
  dayAnalysis: {
    marginTop: Spacing.md,
  },
  bottomPadding: {
    height: 80,
  },
});
