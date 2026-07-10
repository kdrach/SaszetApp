import { create } from 'zustand';

export interface LogEntry {
  id: string;
  timestamp: number;
  method: string;
  url: string;
  status?: number;
  requestData?: any;
  responseData?: any;
  duration?: number;
  error?: string;
}

interface LogState {
  logs: LogEntry[];
  addLog: (log: LogEntry) => void;
  updateLog: (id: string, log: Partial<LogEntry>) => void;
  clearLogs: () => void;
}

export const useLogStore = create<LogState>((set) => ({
  logs: [],
  addLog: (log) => set((state) => ({ logs: [log, ...state.logs].slice(0, 100) })),
  updateLog: (id, updated) => set((state) => ({
    logs: state.logs.map((l) => (l.id === id ? { ...l, ...updated } : l))
  })),
  clearLogs: () => set({ logs: [] }),
}));
