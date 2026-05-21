import { api } from './api';
import type { AuthResponse, LoginRequest, RegisterRequest } from '@/types';

export async function register(data: RegisterRequest): Promise<AuthResponse> {
  const response = await api.post<AuthResponse>('/auth/register', data);
  return response.data;
}

export async function login(data: LoginRequest): Promise<AuthResponse> {
  const response = await api.post<AuthResponse>('/auth/login', data);
  return response.data;
}

export async function refreshToken(token: string): Promise<AuthResponse> {
  const response = await api.post<AuthResponse>('/auth/refresh', { token });
  return response.data;
}
