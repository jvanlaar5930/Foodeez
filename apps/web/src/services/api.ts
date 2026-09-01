import axios, { type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';

let authStore: { token: string | null; logout: () => void } | null = null;

/**
 * Lazily inject the auth store to avoid circular dependency issues at module load time.
 */
export function setAuthStore(store: { token: string | null; logout: () => void }): void {
  authStore = store;
}

const api: AxiosInstance = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor — attach bearer token
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = authStore?.token ?? localStorage.getItem('foodeez_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor — handle 401
// Only redirect to login for auth-endpoint failures (token expired/missing),
// not for role-permission failures on other endpoints (that would log out mid-session).
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const url: string = error.config?.url ?? '';
    const isAuthEndpoint = url.includes('/auth/');
    if (error.response?.status === 401 && isAuthEndpoint) {
      authStore?.logout();
      window.location.href = '/auth/login';
    }
    return Promise.reject(error);
  },
);

export default api;
