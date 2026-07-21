export interface Cat {
  id: string;
  name: string;
  breed: string;
  weight: number;
  allergies: string;
}

export interface CatCreateDto {
  name: string;
  breed: string;
  weight: number;
  allergies: string;
}

export interface UserProfile {
  id: string;
  remainingScans: number;
  cats: Cat[];
}

let mockCats: Cat[] = [
  { id: '1', name: 'Puszek', breed: 'Maine Coon', weight: 6.5, allergies: 'Brak' },
  { id: '2', name: 'Mruczek', breed: 'Dachowiec', weight: 4.2, allergies: 'Kurczak' }
];

// Scans are hardcoded in the response to test the logic
// Actually, let's leave it at 1 so the user can see the red bar immediately as requested or I'll just use 1.
// Re-reading: "If remainingScans === 1, the bar must glow/turn RED. Otherwise Emerald."
// I will set it to 1 to demonstrate the glow immediately.

export const profileApi = {
  getProfile: async (): Promise<UserProfile> => {
    return new Promise((resolve) => {
      setTimeout(() => {
        resolve({
          id: 'user123',
          remainingScans: 1, // Set to 1 to show the RED glow state
          cats: [...mockCats]
        });
      }, 800);
    });
  },

  addCat: async (dto: CatCreateDto): Promise<Cat> => {
    return new Promise((resolve) => {
      setTimeout(() => {
        const newCat: Cat = {
          id: Math.random().toString(36).substr(2, 9),
          ...dto
        };
        mockCats.push(newCat);
        resolve(newCat);
      }, 800);
    });
  },

  deleteCat: async (id: string): Promise<void> => {
    return new Promise((resolve) => {
      setTimeout(() => {
        mockCats = mockCats.filter(c => c.id !== id);
        resolve();
      }, 500);
    });
  }
};
