import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';
import { FontSize, Shadows } from '@/constants/theme';
import { useTheme } from '@/theme';
import { DashboardScreen } from '@/screens/dashboard/DashboardScreen';
import { ReportsScreen } from '@/screens/reports/ReportsScreen';
import { MealLogScreen } from '@/screens/meal-log/MealLogScreen';
import { AddMealScreen } from '@/screens/meal-log/AddMealScreen';
import { FoodScanScreen } from '@/screens/meal-log/FoodScanScreen';
import { MealPlanScreen } from '@/screens/meal-plan/MealPlanScreen';
import { CalendarDayScreen } from '@/screens/meal-plan/CalendarDayScreen';
import { GroceryScreen } from '@/screens/grocery/GroceryScreen';
import { AdviceScreen } from '@/screens/advice/AdviceScreen';
import { RecipesScreen } from '@/screens/recipes/RecipesScreen';
import { RecipeDetailScreen } from '@/screens/recipes/RecipeDetailScreen';
import { ProfileScreen } from '@/screens/profile/ProfileScreen';
import { EditProfileScreen } from '@/screens/profile/EditProfileScreen';
import type {
  DashboardStackParamList,
  MainTabParamList,
  MealLogStackParamList,
  MealPlanStackParamList,
  RecipesStackParamList,
  ProfileStackParamList,
} from './types';

const Tab = createBottomTabNavigator<MainTabParamList>();
const MealLogStack = createNativeStackNavigator<MealLogStackParamList>();
const MealPlanStack = createNativeStackNavigator<MealPlanStackParamList>();
const RecipesStack = createNativeStackNavigator<RecipesStackParamList>();
const ProfileStack = createNativeStackNavigator<ProfileStackParamList>();
const DashboardStack = createNativeStackNavigator<DashboardStackParamList>();

function DashboardNavigator() {
  const C = useTheme();
  return (
    <DashboardStack.Navigator
      screenOptions={{
        headerStyle: { backgroundColor: C.surface },
        headerTintColor: C.text,
        headerTitleStyle: { fontWeight: '600', fontSize: FontSize.lg },
      }}
    >
      <DashboardStack.Screen
        name="DashboardHome"
        component={DashboardScreen}
        options={{ headerShown: false }}
      />
      <DashboardStack.Screen
        name="Reports"
        component={ReportsScreen}
        options={{ title: 'Reports' }}
      />
    </DashboardStack.Navigator>
  );
}

function ProfileNavigator() {
  const C = useTheme();
  return (
    <ProfileStack.Navigator
      screenOptions={{
        headerStyle: { backgroundColor: C.surface },
        headerTintColor: C.text,
        headerTitleStyle: { fontWeight: '600', fontSize: FontSize.lg },
      }}
    >
      <ProfileStack.Screen
        name="ProfileHome"
        component={ProfileScreen}
        options={{ headerShown: false }}
      />
      <ProfileStack.Screen
        name="EditProfile"
        component={EditProfileScreen}
        options={{ title: 'Edit Profile' }}
      />
    </ProfileStack.Navigator>
  );
}

function MealLogNavigator() {
  const C = useTheme();
  return (
    <MealLogStack.Navigator
      screenOptions={{
        headerStyle: { backgroundColor: C.surface },
        headerTintColor: C.text,
        headerTitleStyle: { fontWeight: '600', fontSize: FontSize.lg },
      }}
    >
      <MealLogStack.Screen
        name="MealLogHome"
        component={MealLogScreen}
        options={{ title: 'Meal Log' }}
      />
      <MealLogStack.Screen
        name="AddMeal"
        component={AddMealScreen}
        options={{ title: 'Add Meal' }}
      />
      <MealLogStack.Screen
        name="FoodScan"
        component={FoodScanScreen}
        options={{ title: 'Scan Food', headerShown: false }}
      />
    </MealLogStack.Navigator>
  );
}

function MealPlanNavigator() {
  const C = useTheme();
  return (
    <MealPlanStack.Navigator
      screenOptions={{
        headerStyle: { backgroundColor: C.surface },
        headerTintColor: C.text,
        headerTitleStyle: { fontWeight: '600', fontSize: FontSize.lg },
      }}
    >
      <MealPlanStack.Screen
        name="MealPlanHome"
        component={MealPlanScreen}
        options={{ title: 'Meal Plan' }}
      />
      <MealPlanStack.Screen
        name="CalendarDay"
        component={CalendarDayScreen}
        options={{ title: 'Day View' }}
      />
    </MealPlanStack.Navigator>
  );
}

function RecipesNavigator() {
  const C = useTheme();
  return (
    <RecipesStack.Navigator
      screenOptions={{
        headerStyle: { backgroundColor: C.surface },
        headerTintColor: C.text,
        headerTitleStyle: { fontWeight: '600', fontSize: FontSize.lg },
      }}
    >
      <RecipesStack.Screen
        name="RecipesList"
        component={RecipesScreen}
        options={{ title: 'Recipes' }}
      />
      <RecipesStack.Screen
        name="RecipeDetail"
        component={RecipeDetailScreen}
        options={{ title: 'Recipe Detail' }}
      />
    </RecipesStack.Navigator>
  );
}

export function MainTabNavigator() {
  const C = useTheme();
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        tabBarIcon: ({ focused, color, size }) => {
          let iconName: keyof typeof Ionicons.glyphMap;
          switch (route.name) {
            case 'Dashboard':
              iconName = focused ? 'home' : 'home-outline';
              break;
            case 'MealLog':
              iconName = focused ? 'restaurant' : 'restaurant-outline';
              break;
            case 'MealPlan':
              iconName = focused ? 'calendar' : 'calendar-outline';
              break;
            case 'Grocery':
              iconName = focused ? 'cart' : 'cart-outline';
              break;
            case 'Advice':
              iconName = focused ? 'chatbubble-ellipses' : 'chatbubble-ellipses-outline';
              break;
            case 'Recipes':
              iconName = focused ? 'book' : 'book-outline';
              break;
            case 'Profile':
              iconName = focused ? 'person' : 'person-outline';
              break;
            default:
              iconName = 'help-outline';
          }
          return <Ionicons name={iconName} size={size} color={color} />;
        },
        tabBarLabelStyle: { fontSize: FontSize.xs },
        tabBarActiveTintColor: C.primary,
        tabBarInactiveTintColor: C.textSecondary,
        tabBarStyle: {
          backgroundColor: C.surface,
          borderTopColor: C.divider,
          ...Shadows.sm,
        },
        headerShown: false,
      })}
    >
      <Tab.Screen name="Dashboard" component={DashboardNavigator} options={{ title: 'Dashboard' }} />
      <Tab.Screen name="MealLog" component={MealLogNavigator} options={{ title: 'Meal Log' }} />
      <Tab.Screen name="MealPlan" component={MealPlanNavigator} options={{ title: 'Meal Plan' }} />
      <Tab.Screen name="Grocery" component={GroceryScreen} options={{ title: 'Grocery' }} />
      <Tab.Screen name="Advice" component={AdviceScreen} options={{ title: 'Ask AI' }} />
      <Tab.Screen name="Recipes" component={RecipesNavigator} options={{ title: 'Recipes' }} />
      <Tab.Screen name="Profile" component={ProfileNavigator} options={{ title: 'Profile' }} />
    </Tab.Navigator>
  );
}
