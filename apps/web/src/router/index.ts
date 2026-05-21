import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const LoginView = () => import('@/views/auth/LoginView.vue');
const RegisterView = () => import('@/views/auth/RegisterView.vue');
const ProfileSetupView = () => import('@/views/auth/ProfileSetupView.vue');
const DashboardView = () => import('@/views/dashboard/DashboardView.vue');
const MealLogView = () => import('@/views/meal-log/MealLogView.vue');
const MealPlanView = () => import('@/views/meal-plan/MealPlanView.vue');
const RecipesView = () => import('@/views/recipes/RecipesView.vue');
const ProfileView = () => import('@/views/profile/ProfileView.vue');

const routes = [
  { path: '/', redirect: '/dashboard' },
  {
    path: '/auth/login',
    component: LoginView,
    meta: { requiresGuest: true },
  },
  {
    path: '/auth/register',
    component: RegisterView,
    meta: { requiresGuest: true },
  },
  {
    path: '/auth/setup',
    component: ProfileSetupView,
  },
  {
    path: '/dashboard',
    component: DashboardView,
    meta: { requiresAuth: true },
  },
  {
    path: '/meal-log',
    component: MealLogView,
    meta: { requiresAuth: true },
  },
  {
    path: '/meal-plan',
    component: MealPlanView,
    meta: { requiresAuth: true },
  },
  {
    path: '/recipes',
    component: RecipesView,
    meta: { requiresAuth: true },
  },
  {
    path: '/profile',
    component: ProfileView,
    meta: { requiresAuth: true },
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/dashboard',
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach(async (to) => {
  const authStore = useAuthStore();

  // If we have a token but no user loaded yet, try to fetch user
  if (authStore.token && !authStore.user) {
    try {
      await authStore.fetchCurrentUser();
    } catch {
      authStore.logout();
    }
  }

  const isAuthenticated = authStore.isAuthenticated;

  if (to.meta.requiresAuth && !isAuthenticated) {
    return { path: '/auth/login', query: { redirect: to.fullPath } };
  }

  if (to.meta.requiresGuest && isAuthenticated) {
    return { path: '/dashboard' };
  }

  return true;
});

export default router;
