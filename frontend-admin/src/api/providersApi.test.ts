import { describe, it, expect, vi, beforeEach } from 'vitest';
import { api, providersApi } from './providersApi';
import keycloak from '../keycloak';

vi.mock('../keycloak', () => {
  return {
    default: {
      token: undefined,
      updateToken: vi.fn(),
      login: vi.fn()
    }
  };
});

describe('providersApi interceptors', () => {
  let mockAdapter: any;

  beforeEach(() => {
    vi.clearAllMocks();
    
    mockAdapter = vi.fn().mockResolvedValue({
      data: [],
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {}
    });
    
    api.defaults.adapter = mockAdapter;
  });

  it('should not modify config if token is missing', async () => {
    keycloak.token = undefined;
    
    await providersApi.getProviders();
    
    const config = mockAdapter.mock.calls[0][0];
    expect(config.headers.get('Authorization')).toBeFalsy();
    expect(keycloak.updateToken).not.toHaveBeenCalled();
  });

  it('should set Authorization header if token exists and update is successful', async () => {
    keycloak.token = 'test-token';
    vi.mocked(keycloak.updateToken).mockResolvedValueOnce(true);
    
    await providersApi.getProviders();
    
    const config = mockAdapter.mock.calls[0][0];
    expect(keycloak.updateToken).toHaveBeenCalledWith(30);
    expect(config.headers.get('Authorization')).toBe('Bearer test-token');
  });

  it('should call login if updateToken fails', async () => {
    keycloak.token = 'test-token';
    vi.mocked(keycloak.updateToken).mockRejectedValueOnce(new Error('Token refresh failed'));
    
    try {
      await providersApi.getProviders();
    } catch (e) {
      // ignore
    }
    
    expect(keycloak.updateToken).toHaveBeenCalledWith(30);
    expect(keycloak.login).toHaveBeenCalled();
  });
});
