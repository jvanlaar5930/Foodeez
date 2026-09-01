import axios from 'axios';
import { API_URL } from '@/constants/api';

// Surface the resolved host in the Metro console - the usual cause of a failing dev login
// is the device not being able to reach this address.
if (__DEV__) console.log(`[api] base URL: ${API_URL}`);

export const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

// We import the store lazily to avoid circular dependency issues
api.interceptors.request.use((config) => {
  // Lazy import to avoid circular dependency
  const { useAuthStore } = require('@/store/authStore');
  const token = useAuthStore.getState().token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error.response?.status === 401) {
      const { useAuthStore } = require('@/store/authStore');
      useAuthStore.getState().logout();
    }
    return Promise.reject(error);
  },
);
