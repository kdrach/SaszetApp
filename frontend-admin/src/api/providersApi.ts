import axios from 'axios';
import keycloak from '../keycloak';
import { LlmProvider, CreateProviderDto } from '../types';

const api = axios.create({
  baseURL: `${import.meta.env.VITE_API_URL || 'http://localhost:5000'}/api`,
});

api.interceptors.request.use(async (config) => {
  if (keycloak.token) {
    try {
      await keycloak.updateToken(30);
      config.headers.Authorization = `Bearer ${keycloak.token}`;
    } catch (error) {
      console.error('Failed to refresh token', error);
      keycloak.login();
    }
  }
  return config;
}, (error) => {
  return Promise.reject(error);
});

export const providersApi = {
  getProviders: async () => {
    const response = await api.get<LlmProvider[]>('/AdminProvider');
    return response.data;
  },
  createProvider: async (data: CreateProviderDto) => {
    const response = await api.post<LlmProvider>('/AdminProvider', data);
    return response.data;
  },
  updateProvider: async (id: string, data: CreateProviderDto) => {
    const response = await api.put<LlmProvider>(`/AdminProvider/${id}`, data);
    return response.data;
  },
  setPrimary: async (id: string) => {
    const response = await api.put(`/AdminProvider/${id}/set-primary`);
    return response.data;
  },
  testConnection: async (id: string) => {
    const response = await api.post(`/AdminProvider/${id}/test`);
    return response.data;
  }
};
