import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { fetchAnalysisResult } from './scanApi';

describe('scanApi', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it('should resolve good response for english', async () => {
    const promise = fetchAnalysisResult('good', 'en');
    vi.advanceTimersByTime(4000);
    const result = await promise;
    expect(result.productName).toBe('GOOD');
    expect(result.rating).toBe(8);
  });

  it('should resolve bad response when query contains bad', async () => {
    const promise = fetchAnalysisResult('bad product', 'en');
    vi.advanceTimersByTime(4000);
    const result = await promise;
    expect(result.rating).toBe(3);
  });

  it('should resolve bad response when query contains złe', async () => {
    const promise = fetchAnalysisResult('złe', 'pl');
    vi.advanceTimersByTime(4000);
    const result = await promise;
    expect(result.rating).toBe(3);
  });
});
