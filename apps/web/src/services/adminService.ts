import api from './api';

export interface AdminUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isAdmin: boolean;
  isActive: boolean;
  profileCompleted: boolean;
  createdAt: string;
}

export interface AppLogEntry {
  id: number;
  level: string;
  message: string;
  exception?: string;
  source?: string;
  timestamp: string;
}

export interface AppLogPage {
  items: AppLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AppSettingItem {
  key: string;
  value: string;
  category: string;
  description?: string;
  isSecret: boolean;
  updatedAt: string;
}

export const adminService = {
  async getLogs(page = 1, pageSize = 50, level?: string, search?: string): Promise<AppLogPage> {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (level) params.append('level', level);
    if (search) params.append('search', search);
    const response = await api.get<AppLogPage>(`/admin/logs?${params}`);
    return response.data;
  },

  async getUsers(): Promise<AdminUser[]> {
    const response = await api.get<AdminUser[]>('/admin/users');
    return response.data;
  },

  async toggleAdmin(userId: string): Promise<{ isAdmin: boolean }> {
    const response = await api.put<{ isAdmin: boolean }>(`/admin/users/${userId}/toggle-admin`);
    return response.data;
  },

  async toggleActive(userId: string): Promise<{ isActive: boolean }> {
    const response = await api.put<{ isActive: boolean }>(`/admin/users/${userId}/toggle-active`);
    return response.data;
  },

  async getSettings(): Promise<AppSettingItem[]> {
    const response = await api.get<AppSettingItem[]>('/admin/settings');
    return response.data;
  },

  async updateSettings(settings: { key: string; value: string }[]): Promise<void> {
    await api.post('/admin/settings', { settings });
  },
};
