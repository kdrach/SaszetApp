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

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export const getAllUsersLimits = async (token: string, page: number = 1, pageSize: number = 50): Promise<PagedResult<UserLimitDto>> => {
  const response = await axios.get(`${API_BASE_URL}/AdminSettings/users?page=${page}&pageSize=${pageSize}`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  return response.data;
};
