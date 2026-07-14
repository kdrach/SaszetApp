import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import BottomTabBar from './BottomTabBar';

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

  it('renders navigation links and scan buttons on home route', () => {
    const { container } = render(
      <MemoryRouter initialEntries={['/']}>
        <BottomTabBar />
      </MemoryRouter>
    );
    
    const links = container.querySelectorAll('a');
    expect(links).toHaveLength(3); // Home, EAN Scan, Profile
    expect(links[0].getAttribute('href')).toBe('/');
    expect(links[1].getAttribute('href')).toBe('/scan');
    expect(links[2].getAttribute('href')).toBe('/profile');
    
    // There should be a hidden file input
    const fileInput = document.querySelector('input[type="file"]');
    expect(fileInput).toBeInTheDocument();
    
    // There should be a button to trigger the camera (ingredients scan)
    const cameraButton = container.querySelector('button[aria-label="Scan Ingredients"]');
    expect(cameraButton).toBeInTheDocument();
  });

  it('does not render anything on /scan route', () => {
    const { container } = render(
      <MemoryRouter initialEntries={['/scan']}>
        <BottomTabBar />
      </MemoryRouter>
    );
    
    expect(container).toBeEmptyDOMElement();
  });
});
