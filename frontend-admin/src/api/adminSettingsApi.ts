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
