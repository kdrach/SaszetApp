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

  it('should call apiClient with correct parameters, including signal and timeout', async () => {
    const mockData = { productName: 'REAL PRODUCT', rating: 10 };
    (apiClient.get as any).mockResolvedValue({ data: mockData });
    const controller = new AbortController();

    const result = await fetchAnalysisResult('real', 'en', controller.signal);
    
    expect(apiClient.get).toHaveBeenCalledWith('/Scan/search', {
      params: { query: 'real' },
      headers: { 'Accept-Language': 'en' },
      signal: controller.signal,
      timeout: 60000
    });
    expect(result).toEqual(mockData);
  });

  it('should throw an error if query is empty or whitespace', async () => {
    await expect(fetchAnalysisResult('', 'en')).rejects.toThrow('Query is required');
    await expect(fetchAnalysisResult('   ', 'en')).rejects.toThrow('Query is required');
    expect(apiClient.get).not.toHaveBeenCalled();
  });
});
