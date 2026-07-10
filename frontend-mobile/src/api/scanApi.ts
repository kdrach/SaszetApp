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

export const uploadImageForAnalysis = async (
  imageBlob: Blob, 
  mode: 'Ingredients' | 'General', 
  language: string, 
  signal?: AbortSignal
): Promise<VLMResponseContract> => {
  const formData = new FormData();
  formData.append('image', imageBlob, 'photo.jpg'); 
  formData.append('mode', mode);

  const response = await apiClient.post<VLMResponseContract>('/Scan/analyze-image', formData, {
    headers: { 
      'Accept-Language': language,
      'Content-Type': 'multipart/form-data'
    },
    signal,
    timeout: 120000
  });
  
  return response.data;
};
