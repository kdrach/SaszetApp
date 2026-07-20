import { render, fireEvent, waitFor, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import BottomTabBar from './BottomTabBar';
import { compressImage } from '../utils/imageUtils';

const mockedNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockedNavigate,
  };
});

vi.mock('../utils/imageUtils', () => ({
  compressImage: vi.fn().mockResolvedValue(new Blob(['dummy content'], { type: 'image/jpeg' }))
}));

describe('BottomTabBar', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders navigation links and main FAB on home route', () => {
    const { container } = render(
      <MemoryRouter initialEntries={['/']}>
        <BottomTabBar />
      </MemoryRouter>
    );
    
    const links = container.querySelectorAll('a');
    // Home, History, Compare, Profile
    expect(links).toHaveLength(4); 
    expect(links[0].getAttribute('href')).toBe('/');
    expect(links[1].getAttribute('href')).toBe('/history');
    expect(links[2].getAttribute('href')).toBe('/compare');
    expect(links[3].getAttribute('href')).toBe('/profile');
    
    // There should be a hidden file input
    const fileInput = document.querySelector('input[type="file"]');
    expect(fileInput).toBeInTheDocument();
    
    // There should be a main FAB to toggle scan menu
    const fabButton = screen.getByLabelText('Toggle Scan Menu');
    expect(fabButton).toBeInTheDocument();
    
    // The menu should not be visible initially
    expect(screen.queryByLabelText('Scan Barcode')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Take Photo')).not.toBeInTheDocument();
  });

  it('toggles scan menu when FAB is clicked', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <BottomTabBar />
      </MemoryRouter>
    );
    
    const fabButton = screen.getByLabelText('Toggle Scan Menu');
    fireEvent.click(fabButton);
    
    // Menu items should appear
    expect(screen.getByLabelText('Scan Barcode')).toBeInTheDocument();
    expect(screen.getByLabelText('Take Photo')).toBeInTheDocument();
    expect(screen.getByLabelText('Scan Multiple')).toBeInTheDocument();
    
    // Clicking again should close it
    fireEvent.click(fabButton);
    expect(screen.queryByLabelText('Scan Barcode')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Take Photo')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Scan Multiple')).not.toBeInTheDocument();
  });

  it('does not render anything on /scan route', () => {
    const { container } = render(
      <MemoryRouter initialEntries={['/scan']}>
        <BottomTabBar />
      </MemoryRouter>
    );
    
    expect(container).toBeEmptyDOMElement();
  });

  it('triggers file input click when Take Photo is clicked', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <BottomTabBar />
      </MemoryRouter>
    );
    
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    const clickSpy = vi.spyOn(fileInput, 'click');
    
    // Open menu
    fireEvent.click(screen.getByLabelText('Toggle Scan Menu'));
    
    // Click Take Photo
    const photoButton = screen.getByLabelText('Take Photo');
    fireEvent.click(photoButton);
    
    expect(clickSpy).toHaveBeenCalled();
  });

  it('compresses image and navigates to photo view on file change', async () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <BottomTabBar />
      </MemoryRouter>
    );
    
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    const testFile = new File(['dummy'], 'test.png', { type: 'image/png' });
    
    fireEvent.change(fileInput, { target: { files: [testFile] } });
    
    expect(fileInput.value).toBe('');
    
    await waitFor(() => {
      expect(compressImage).toHaveBeenCalledWith(testFile);
      expect(mockedNavigate).toHaveBeenCalledWith('/product/photo', {
        state: { 
          imageBlob: expect.any(Blob)
        }
      });
    });
  });
});
