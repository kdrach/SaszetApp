import { describe, it, expect, beforeEach } from 'vitest';
import { useAppStore } from './useAppStore';

describe('useAppStore', () => {
  beforeEach(() => {
    useAppStore.getState().clearScans();
  });

  it('should add a scan', () => {
    const scan = { id: '1', query: '123', timestamp: 123, result: {} as any };
    useAppStore.getState().addScan(scan);
    expect(useAppStore.getState().recentScans).toHaveLength(1);
    expect(useAppStore.getState().recentScans[0]).toEqual(scan);
  });

  it('should prevent duplicates by query', () => {
    const scan1 = { id: '1', query: '123', timestamp: 123, result: {} as any };
    const scan2 = { id: '2', query: '123', timestamp: 124, result: {} as any };
    useAppStore.getState().addScan(scan1);
    useAppStore.getState().addScan(scan2);
    expect(useAppStore.getState().recentScans).toHaveLength(1);
    expect(useAppStore.getState().recentScans[0]).toEqual(scan2);
  });

  it('should clear scans', () => {
    const scan = { id: '1', query: '123', timestamp: 123, result: {} as any };
    useAppStore.getState().addScan(scan);
    useAppStore.getState().clearScans();
    expect(useAppStore.getState().recentScans).toHaveLength(0);
  });
});
