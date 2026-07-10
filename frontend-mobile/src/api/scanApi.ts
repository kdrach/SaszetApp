import apiClient from './axios';
import { VLMResponseContract } from '../types';

export const fetchAnalysisResult = async (query: string, language: string): Promise<VLMResponseContract> => {
  const response = await apiClient.get<VLMResponseContract>('/Scan/search', {
    params: { query },
    headers: { 'Accept-Language': language }
  });
  return response.data;
};
