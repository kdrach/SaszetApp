import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import AdminDashboardView from './AdminDashboardView';
import { providersApi } from '../api/providersApi';

// Mock the API
vi.mock('../api/providersApi', () => ({
  providersApi: {
    getProviders: vi.fn(),
    createProvider: vi.fn(),
    setPrimary: vi.fn(),
    testConnection: vi.fn(),
  },
}));



describe('AdminDashboardView', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('renders loading state initially', () => {
    // Return a promise that doesn't resolve immediately
    (providersApi.getProviders as any).mockImplementation(() => new Promise(() => {}));
    
    render(<AdminDashboardView />);
    
    expect(screen.getByText(/Loading configurations.../i)).toBeInTheDocument();
  });

  it('renders error state if fetching providers fails', async () => {
    const errorMessage = 'Network Error';
    (providersApi.getProviders as any).mockRejectedValue(new Error(errorMessage));
    
    render(<AdminDashboardView />);
    
    await waitFor(() => {
      expect(screen.getByText(errorMessage)).toBeInTheDocument();
    });
  });

  it('renders providers after successful fetch', async () => {
    const mockProviders = [
      { id: '1', providerName: 'OpenAI', apiKey: 'test', isPrimary: true, createdAt: '2023-01-01' },
      { id: '2', providerName: 'Anthropic', apiKey: 'test2', isPrimary: false, createdAt: '2023-01-01' },
    ];
    (providersApi.getProviders as any).mockResolvedValue(mockProviders);
    
    render(<AdminDashboardView />);
    
    await waitFor(() => {
      expect(screen.getByText(/Aktywny Routing LLM/i)).toBeInTheDocument();
      expect(screen.getAllByText('OpenAI').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Anthropic').length).toBeGreaterThan(0);
    });
  });
});
