import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface GlobalSettings {
  globalScanLimit: number;
  scanLimitRollingDays: number;
}

export interface UserLimit {
  userId: string;
  maxScans: number;
}

export const getGlobalSettings = async (token: string): Promise<GlobalSettings> => {
  const response = await axios.get(`${API_BASE_URL}/AdminSettings/global`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  return response.data;
};

export const updateGlobalSettings = async (token: string, settings: GlobalSettings): Promise<GlobalSettings> => {
  const response = await axios.put(`${API_BASE_URL}/AdminSettings/global`, settings, {
    headers: { Authorization: `Bearer ${token}` }
  });
  return response.data;
};

export const getUserLimit = async (token: string, userId: string): Promise<UserLimit> => {
  const response = await axios.get(`${API_BASE_URL}/AdminSettings/users/${userId}`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  return response.data;
};

export const updateUserLimit = async (token: string, userId: string, maxScans: number): Promise<UserLimit> => {
  const response = await axios.put(`${API_BASE_URL}/AdminSettings/users/${userId}`, { userId, maxScans }, {
    headers: { Authorization: `Bearer ${token}` }
  });
  return response.data;
};

export interface UserLimitDto {
  userId: string;
  usage: number;
  maxScans: number;
  lastReset: string;
}

export const getAllUsersLimits = async (token: string): Promise<UserLimitDto[]> => {
  // Mock Data for UX validation before backend implementation
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve([
        { userId: 'user_123_abc', usage: 3, maxScans: 5, lastReset: new Date().toISOString() },
        { userId: 'user_456_def', usage: 5, maxScans: 5, lastReset: new Date().toISOString() },
        { userId: 'user_789_ghi', usage: 1, maxScans: 10, lastReset: new Date().toISOString() },
        { userId: 'user_999_xyz', usage: 12, maxScans: 15, lastReset: new Date().toISOString() },
        { userId: 'user_000_foo', usage: 0, maxScans: 5, lastReset: new Date().toISOString() },
      ]);
    }, 500);
  });
};

