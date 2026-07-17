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
  language: string = 'pl',
  signal?: AbortSignal
): Promise<VLMResponseContract> => {
  const formData = new FormData();
  formData.append('image', imageBlob, 'capture.jpg');
  
  const response = await apiClient.post<VLMResponseContract>('/Scan/analyze-image', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
      'Accept-Language': language
    },
    signal,
    timeout: 120000
  });
  
  return response.data;
};

export const compareProducts = async (
  images: Blob[],
  language: string = 'pl',
  signal?: AbortSignal
): Promise<VLMResponseContract[]> => {
  const formData = new FormData();
  images.forEach((image, index) => {
    formData.append('images', image, `capture_${index}.jpg`);
  });
  
  const response = await apiClient.post<VLMResponseContract[]>('/Scan/compare', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
      'Accept-Language': language
    },
    signal,
    timeout: 300000 // 5 minutes timeout for 5 images comparison
  });
  
  return response.data;
};
