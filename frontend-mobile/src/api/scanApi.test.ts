import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fetchAnalysisResult } from './scanApi';
import apiClient from './axios';

vi.mock('./axios', () => {
  return {
    default: {
      get: vi.fn(),
    },
  };
});

describe('scanApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should call apiClient with correct parameters', async () => {
    const mockData = { productName: 'REAL PRODUCT', rating: 10 };
    (apiClient.get as any).mockResolvedValue({ data: mockData });

    const result = await fetchAnalysisResult('real', 'en');
    
    expect(apiClient.get).toHaveBeenCalledWith('/Scan/search', {
      params: { query: 'real' },
      headers: { 'Accept-Language': 'en' }
    });
    expect(result).toEqual(mockData);
  });
});
