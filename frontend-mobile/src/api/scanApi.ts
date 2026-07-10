import apiClient from './axios';
import { VLMResponseContract } from '../types';

export const fetchAnalysisResult = async (query: string, language: string, signal?: AbortSignal): Promise<VLMResponseContract> => {
  if (!query || query.trim() === '') {
    throw new Error('Query is required');
  }
  const response = await apiClient.get<VLMResponseContract>('/Scan/search', {
    params: { query },
    headers: { 'Accept-Language': language },
    signal,
    timeout: 60000
  });
  return response.data;
};
