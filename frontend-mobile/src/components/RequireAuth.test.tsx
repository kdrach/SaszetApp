import { render, screen } from '@testing-library/react';
import { RequireAuth } from './RequireAuth';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { useAuth } from './AuthProvider';

vi.mock('./AuthProvider', () => ({
  useAuth: vi.fn()
}));

vi.mock('./LoadingOverlay', () => ({
  default: () => <div data-testid="loading-overlay">Loading Overlay</div>
}));

describe('RequireAuth', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading overlay when loading', () => {
    (useAuth as any).mockReturnValue({
      isAuthenticated: false,
      isLoading: true,
      login: vi.fn(),
    });

    render(
      <RequireAuth>
        <div>Protected Content</div>
      </RequireAuth>
    );

    expect(screen.getByTestId('loading-overlay')).toBeInTheDocument();
  });

  it('calls login when not authenticated', () => {
    const loginMock = vi.fn();
    (useAuth as any).mockReturnValue({
      isAuthenticated: false,
      isLoading: false,
      login: loginMock,
    });

    render(
      <RequireAuth>
        <div>Protected Content</div>
      </RequireAuth>
    );

    expect(loginMock).toHaveBeenCalled();
    expect(screen.getByTestId('loading-overlay')).toBeInTheDocument();
  });

  it('renders children when authenticated', () => {
    (useAuth as any).mockReturnValue({
      isAuthenticated: true,
      isLoading: false,
      login: vi.fn(),
    });

    render(
      <RequireAuth>
        <div>Protected Content</div>
      </RequireAuth>
    );

    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });
});
