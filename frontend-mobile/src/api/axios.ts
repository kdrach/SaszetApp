import axios from 'axios';
import keycloak from './keycloak';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
});

apiClient.interceptors.request.use(
  (config) => {
    if (keycloak.token) {
      config.headers['Authorization'] = `Bearer ${keycloak.token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

export default apiClient;
