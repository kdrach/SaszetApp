import { describe, it, expect, vi } from 'vitest';
import apiClient from './axios';
import keycloak from './keycloak';

vi.mock('./keycloak', () => ({
  default: {
    token: undefined
  }
}));

describe('Axios Interceptor', () => {
  it('adds Authorization header if keycloak.token exists', async () => {
    keycloak.token = 'mock-token';
    const config = { headers: {} as any } as any;
    
    // get the full interceptor configuration
    const reqInterceptor = (apiClient.interceptors.request as any).handlers[0].fulfilled;
    const result = await reqInterceptor(config);
    
    expect(result.headers['Authorization']).toBe('Bearer mock-token');
  });

  it('does not add Authorization header if keycloak.token is missing', async () => {
    keycloak.token = undefined;
    const config = { headers: {} as any } as any;
    
    const reqInterceptor = (apiClient.interceptors.request as any).handlers[0].fulfilled;
    const result = await reqInterceptor(config);
    
    expect(result.headers['Authorization']).toBeUndefined();
  });
});
