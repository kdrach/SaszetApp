import axios from 'axios';
import keycloak from './keycloak';

const baseURL = import.meta.env.VITE_API_URL 
  ? `${import.meta.env.VITE_API_URL}/api`
  : '/api';

const apiClient = axios.create({
  baseURL,
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
