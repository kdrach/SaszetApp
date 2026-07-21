import { describe, it, expect, vi, beforeEach } from 'vitest';
import { profileApi } from './profileApi';
import apiClient from './axios';

vi.mock('./axios', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn()
  }
}));

describe('profileApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getProfile calls GET /Profile', async () => {
    const mockProfile = { id: '1', remainingScans: 5, cats: [] };
    (apiClient.get as any).mockResolvedValue({ data: mockProfile });

    const result = await profileApi.getProfile();
    expect(apiClient.get).toHaveBeenCalledWith('/Profile');
    expect(result).toEqual(mockProfile);
  });

  it('addCat calls POST /Profile/cats', async () => {
    const mockCat = { id: 'c1', name: 'Kitty', breed: 'Mixed', weight: 4, allergies: '' };
    (apiClient.post as any).mockResolvedValue({ data: mockCat });

    const dto = { name: 'Kitty', breed: 'Mixed', weight: 4, allergies: '' };
    const result = await profileApi.addCat(dto);
    expect(apiClient.post).toHaveBeenCalledWith('/Profile/cats', dto);
    expect(result).toEqual(mockCat);
  });

  it('deleteCat calls DELETE /Profile/cats/:id', async () => {
    (apiClient.delete as any).mockResolvedValue({});

    await profileApi.deleteCat('c1');
    expect(apiClient.delete).toHaveBeenCalledWith('/Profile/cats/c1');
  });
});
