import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import ProfileView from './ProfileView';
import { profileApi } from '../api/profileApi';
import { I18nextProvider } from 'react-i18next';
import i18n from '../i18n';

vi.mock('react-i18next', async () => {
  const actual = await vi.importActual('react-i18next');
  return {
    ...actual as any,
    useTranslation: () => ({
      t: (key: string) => key,
    }),
  };
});

vi.mock('../api/profileApi', () => ({
  profileApi: {
    getProfile: vi.fn(),
    addCat: vi.fn(),
    deleteCat: vi.fn()
  }
}));

describe('ProfileView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockProfile = {
    id: 'user-1',
    remainingScans: 3,
    cats: [
      { id: 'cat-1', name: 'Mruczek', breed: 'Dachowiec', weight: 4.5, allergies: '' }
    ]
  };

  it('renders loading state initially', () => {
    (profileApi.getProfile as any).mockImplementation(() => new Promise(() => {})); // pending promise
    render(
      <I18nextProvider i18n={i18n}>
        <ProfileView />
      </I18nextProvider>
    );
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders profile data and cats successfully', async () => {
    (profileApi.getProfile as any).mockResolvedValue(mockProfile);
    render(
      <I18nextProvider i18n={i18n}>
        <ProfileView />
      </I18nextProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Mruczek')).toBeInTheDocument();
      expect(screen.getByText('3')).toBeInTheDocument();
    });
  });

  it('renders error state when fetchProfile fails', async () => {
    (profileApi.getProfile as any).mockRejectedValue(new Error('Network error'));
    render(
      <I18nextProvider i18n={i18n}>
        <ProfileView />
      </I18nextProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Network error')).toBeInTheDocument();
    });
  });

  it('adds a cat successfully', async () => {
    (profileApi.getProfile as any).mockResolvedValue({ ...mockProfile, cats: [] });
    const newCat = { id: 'cat-2', name: 'Filemon', breed: 'Pers', weight: 3.2, allergies: 'Chicken' };
    (profileApi.addCat as any).mockResolvedValue(newCat);

    render(
      <I18nextProvider i18n={i18n}>
        <ProfileView />
      </I18nextProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('addCat')).toBeInTheDocument(); // button to open modal
    });

    // open modal
    fireEvent.click(screen.getByText('addCat'));
    
    // Fill form (we use input names/placeholders or text, but name/breed are standard)
    // Wait for modal to render inputs
    await waitFor(() => {
      expect(screen.getAllByText('name')[0]).toBeInTheDocument();
    });
    
    const inputs = screen.getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: 'Filemon' } }); // name
    fireEvent.change(inputs[1], { target: { value: 'Pers' } }); // breed
    fireEvent.change(screen.getByRole('spinbutton'), { target: { value: '3.2' } }); // weight
    fireEvent.change(inputs[2], { target: { value: 'Chicken' } }); // allergies

    // submit form
    const saveButtons = screen.getAllByText('save');
    fireEvent.click(saveButtons[saveButtons.length - 1]);

    await waitFor(() => {
      expect(profileApi.addCat).toHaveBeenCalledWith({
        name: 'Filemon',
        breed: 'Pers',
        weight: 3.2,
        allergies: 'Chicken'
      });
      expect(screen.getByText('Filemon')).toBeInTheDocument();
    });
  });

  it('handles error when adding a cat', async () => {
    (profileApi.getProfile as any).mockResolvedValue({ ...mockProfile, cats: [] });
    (profileApi.addCat as any).mockRejectedValue(new Error('Failed to add cat'));

    render(
      <I18nextProvider i18n={i18n}>
        <ProfileView />
      </I18nextProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('addCat')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('addCat'));
    
    const inputs = screen.getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: 'Filemon' } });
    fireEvent.change(inputs[1], { target: { value: 'Pers' } });
    fireEvent.change(screen.getByRole('spinbutton'), { target: { value: '3.2' } });

    const saveButtons = screen.getAllByText('save');
    fireEvent.click(saveButtons[saveButtons.length - 1]);

    await waitFor(() => {
      expect(screen.getByText('Failed to add cat')).toBeInTheDocument();
    });
  });

  it('deletes a cat successfully', async () => {
    (profileApi.getProfile as any).mockResolvedValue(mockProfile);
    (profileApi.deleteCat as any).mockResolvedValue(undefined);

    render(
      <I18nextProvider i18n={i18n}>
        <ProfileView />
      </I18nextProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Mruczek')).toBeInTheDocument();
    });

    // click delete button
    const trashButton = screen.getByTestId('delete-cat-button');
    fireEvent.click(trashButton);

    await waitFor(() => {
      expect(profileApi.deleteCat).toHaveBeenCalledWith('cat-1');
      expect(screen.queryByText('Mruczek')).not.toBeInTheDocument();
    });
  });
  
  it('handles error when deleting a cat', async () => {
    (profileApi.getProfile as any).mockResolvedValue(mockProfile);
    (profileApi.deleteCat as any).mockRejectedValue(new Error('Delete error'));

    render(
      <I18nextProvider i18n={i18n}>
        <ProfileView />
      </I18nextProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Mruczek')).toBeInTheDocument();
    });

    const trashButton = screen.getByTestId('delete-cat-button');
    fireEvent.click(trashButton);

    await waitFor(() => {
      expect(screen.getByText('Delete error')).toBeInTheDocument();
    });
  });
});
