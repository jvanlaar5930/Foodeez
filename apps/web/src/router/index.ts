import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const HomeView = () => import('@/views/home/HomeView.vue');
const SearchResultsView = () => import('@/views/search/SearchResultsView.vue');
const LoginView = () => import('@/views/auth/LoginView.vue');
const RegisterView = () => import('@/views/auth/RegisterView.vue');
const ProfileSetupView = () => import('@/views/auth/ProfileSetupView.vue');
const DashboardView = () => import('@/views/dashboard/DashboardView.vue');
const MealLogView = () => import('@/views/meal-log/MealLogView.vue');
const MealPlanView = () => import('@/views/meal-plan/MealPlanView.vue');
const AdviceView = () => import('@/views/advice/AdviceView.vue');
const GroceryListView = () => import('@/views/grocery/GroceryListView.vue');
const RecipesView = () => import('@/views/recipes/RecipesView.vue');
const ReportsView = () => import('@/views/reports/ReportsView.vue');
const ProfileView = () => import('@/views/profile/ProfileView.vue');
const AdminLogsView = () => import('@/views/admin/AdminLogsView.vue');
const AdminUsersView = () => import('@/views/admin/AdminUsersView.vue');
const AdminSettingsView = () => import('@/views/admin/AdminSettingsView.vue');

const routes = [
  // Recipe discovery is public: the landing page and search results never require an account.
  {
    path: '/',
    component: HomeView,
  },
  {
    path: '/search',
    component: SearchResultsView,
  },
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
    path: '/grocery',
    component: GroceryListView,
    meta: { requiresAuth: true },
  },
  {
    path: '/advice',
    component: AdviceView,
    meta: { requiresAuth: true },
  },
  {
    path: '/recipes',
    component: RecipesView,
    meta: { requiresAuth: true },
  },
  {
    path: '/reports',
    component: ReportsView,
    meta: { requiresAuth: true },
  },
  {
    path: '/profile',
    component: ProfileView,
    meta: { requiresAuth: true },
  },
  {
    path: '/admin',
    redirect: '/admin/logs',
    meta: { requiresAuth: true, requiresAdmin: true },
  },
  {
    path: '/admin/logs',
    component: AdminLogsView,
    meta: { requiresAuth: true, requiresAdmin: true },
  },
  {
    path: '/admin/users',
    component: AdminUsersView,
    meta: { requiresAuth: true, requiresAdmin: true },
  },
  {
    path: '/admin/settings',
    component: AdminSettingsView,
    meta: { requiresAuth: true, requiresAdmin: true },
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/',
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior(to, _from, savedPosition) {
    if (savedPosition) return savedPosition;
    if (to.hash) return { el: to.hash, behavior: 'smooth' };
    return { top: 0 };
  },
});

router.beforeEach(async (to) => {
  const authStore = useAuthStore();

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

  if (to.meta.requiresAdmin && !authStore.isAdmin) {
    return { path: '/dashboard' };
  }

  return true;
});

export default router;
