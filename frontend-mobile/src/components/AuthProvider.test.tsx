import { render, screen, waitFor } from '@testing-library/react';
import { AuthProvider, useAuth } from './AuthProvider';
import keycloak from '../api/keycloak';
import { describe, it, expect, vi, beforeEach } from 'vitest';

vi.mock('../api/keycloak', () => {
  return {
    default: {
      init: vi.fn(),
      login: vi.fn(),
      logout: vi.fn(),
      updateToken: vi.fn(),
      token: 'fake-token'
    }
  };
});

const TestComponent = () => {
  const { isAuthenticated, isLoading, login, logout, token } = useAuth();
  if (isLoading) return <div>Loading...</div>;
  return (
    <div>
      <div data-testid="auth-status">{isAuthenticated ? 'Authenticated' : 'Not Authenticated'}</div>
      <div data-testid="token">{token}</div>
      <button onClick={login}>Login</button>
      <button onClick={logout}>Logout</button>
    </div>
  );
};

describe('AuthProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state initially and then authenticates', async () => {
    (keycloak.init as any).mockResolvedValueOnce(true);

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    expect(screen.getByText('Loading...')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByTestId('auth-status')).toHaveTextContent('Authenticated');
    });

    expect(keycloak.init).toHaveBeenCalledWith({ onLoad: 'check-sso', checkLoginIframe: false });
    expect(screen.getByTestId('token')).toHaveTextContent('fake-token');
  });

  it('handles initialization failure gracefully', async () => {
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    (keycloak.init as any).mockRejectedValueOnce(new Error('Failed init'));

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(screen.getByTestId('auth-status')).toHaveTextContent('Not Authenticated');
    });

    consoleErrorSpy.mockRestore();
  });
});
