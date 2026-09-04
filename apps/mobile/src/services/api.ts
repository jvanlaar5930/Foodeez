import axios from 'axios';
import { getAuthToken, logoutFromApi } from './authBridge';
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

api.interceptors.request.use((config) => {
  const token = getAuthToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (error) => {
    // A 401 means the token is gone or no longer valid; a permission failure is a 403.
    if (error.response?.status === 401) {
      logoutFromApi();
    }
    return Promise.reject(error);
  },
);
