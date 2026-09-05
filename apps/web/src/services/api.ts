import axios, { type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';

let authStore: { token: string | null; logout: () => void } | null = null;

/**
 * Lazily inject the auth store to avoid circular dependency issues at module load time.
 */
export function setAuthStore(store: { token: string | null; logout: () => void }): void {
  authStore = store;
}

/**
 * The bearer token for an API call. Exported because streaming endpoints are read with fetch
 * rather than axios, and they need the same token from the same place.
 */
export function getAuthToken(): string | null {
  return authStore?.token ?? localStorage.getItem('foodeez_token');
}

const api: AxiosInstance = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor — attach bearer token
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getAuthToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor — handle 401
//
// A 401 means the token is missing, expired or unreadable, wherever it came from. This used
// to act only on 401s from /auth/ URLs, to avoid logging someone out over a permission
// failure - but a permission failure is a 403. The effect was that an expired token left you
// on a page where every request quietly failed and nothing said why, until you reloaded.
//
// Sign-in and registration are excluded: their 401 is "those credentials are wrong", which
// the form is about to show, and redirecting would throw the message away.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const url: string = error.config?.url ?? '';
    const isSignIn = url.includes('/auth/login') || url.includes('/auth/register');

    if (error.response?.status === 401 && !isSignIn) {
      authStore?.logout();
      window.location.href = '/auth/login';
    }
    return Promise.reject(error);
  },
);

export default api;
