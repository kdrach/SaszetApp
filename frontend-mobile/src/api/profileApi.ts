import apiClient from './axios';

export interface Cat {
  id: string;
  name: string;
  breed: string;
  weight: number;
  allergies: string[];
}

export interface CatCreateDto {
  name: string;
  breed: string;
  weight: number;
  allergies: string[];
}

export interface UserProfile {
  id: string;
  remainingScans: number;
  maxScans: number;
  cats: Cat[];
}

export const profileApi = {
  getProfile: async (): Promise<UserProfile> => {
    const response = await apiClient.get<UserProfile>('/Profile');
    return response.data;
  },

  addCat: async (dto: CatCreateDto): Promise<Cat> => {
    const response = await apiClient.post<Cat>('/Profile/cats', dto);
    return response.data;
  },

  deleteCat: async (id: string): Promise<void> => {
    await apiClient.delete(`/Profile/cats/${id}`);
  }
};
