import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { ScannedItem } from '../types';

interface AppState {
  recentScans: ScannedItem[];
  addScan: (scan: ScannedItem) => void;
  clearScans: () => void;
}

export const useAppStore = create<AppState>()(
  persist(
    (set) => ({
      recentScans: [],
      addScan: (scan) =>
        set((state) => {
          // Prevent duplicates by query
          const filtered = state.recentScans.filter(s => s.query !== scan.query);
          return { recentScans: [scan, ...filtered].slice(0, 50) };
        }),
      clearScans: () => set({ recentScans: [] }),
    }),
    {
      name: 'saszetapp-storage',
    }
  )
);
