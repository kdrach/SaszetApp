import { describe, it, expect, vi, beforeEach } from 'vitest';
import { profileApi } from './profileApi';
import apiClient from './axios';

vi.mock('./axios', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
    put: vi.fn()
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
    const mockCat = { id: 'c1', name: 'Kitty', breed: 'Mixed', weight: 4, allergies: [] };

    vi.mocked(apiClient.post).mockResolvedValueOnce({ data: mockCat });
    const dto = { name: 'Kitty', breed: 'Mixed', weight: 4, allergies: [] };
    const result = await profileApi.addCat(dto);
    expect(apiClient.post).toHaveBeenCalledWith('/Profile/cats', dto);
    expect(result).toEqual(mockCat);
  });

  it('deleteCat calls DELETE /Profile/cats/:id', async () => {
    (apiClient.delete as any).mockResolvedValue({});

    await profileApi.deleteCat('c1');
    expect(apiClient.delete).toHaveBeenCalledWith('/Profile/cats/c1');
  });

  it('updateCat calls PUT /Profile/cats/:id', async () => {
    const mockCat = { id: 'c1', name: 'Kitty 2', breed: 'Mixed', weight: 5, allergies: ['fish'] };
    vi.mocked(apiClient.put).mockResolvedValueOnce({ data: mockCat });
    
    const dto = { name: 'Kitty 2', breed: 'Mixed', weight: 5, allergies: ['fish'] };
    const result = await profileApi.updateCat('c1', dto);
    
    expect(apiClient.put).toHaveBeenCalledWith('/Profile/cats/c1', dto);
    expect(result).toEqual(mockCat);
  });
});
