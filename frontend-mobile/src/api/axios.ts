import axios from 'axios';
import keycloak from './keycloak';
import { useLogStore } from '../store/useLogStore';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
});

apiClient.interceptors.request.use(
  (config) => {
    if (keycloak.token) {
      config.headers['Authorization'] = `Bearer ${keycloak.token}`;
    }
    const logId = Date.now().toString() + Math.random().toString(36).substring(7);
    (config as any).logId = logId;
    (config as any).startTime = Date.now();
    useLogStore.getState().addLog({
      id: logId,
      timestamp: Date.now(),
      method: (config.method || 'GET').toUpperCase(),
      url: config.url || '',
      requestData: config.data,
    });
    return config;
  },
  (error) => Promise.reject(error)
);

apiClient.interceptors.response.use(
  (response) => {
    const logId = (response.config as any).logId;
    const startTime = (response.config as any).startTime;
    if (logId) {
      useLogStore.getState().updateLog(logId, {
        status: response.status,
        responseData: response.data,
        duration: Date.now() - (startTime || Date.now()),
      });
    }
    return response;
  },
  (error) => {
    const logId = error.config ? (error.config as any).logId : null;
    const startTime = error.config ? (error.config as any).startTime : Date.now();
    if (logId) {
      useLogStore.getState().updateLog(logId, {
        status: error.response?.status,
        responseData: error.response?.data,
        error: error.message,
        duration: Date.now() - startTime,
      });
    }
    return Promise.reject(error);
  }
);

export default apiClient;
