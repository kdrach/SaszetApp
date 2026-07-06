import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter, useNavigate } from 'react-router-dom';
import HomeView from './HomeView';
import { I18nextProvider } from 'react-i18next';
import i18n from '../i18n';
import { useAppStore } from '../store/useAppStore';

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: vi.fn(),
  };
});

vi.mock('../store/useAppStore', () => ({
  useAppStore: vi.fn(),
}));

describe('HomeView', () => {
  const mockNavigate = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useNavigate).mockReturnValue(mockNavigate);
    vi.mocked(useAppStore).mockImplementation((selector: any) => 
      selector({ recentScans: [] })
    );
  });

  it('renders the header and search input', () => {
    render(
      <I18nextProvider i18n={i18n}>
        <MemoryRouter>
          <HomeView />
        </MemoryRouter>
      </I18nextProvider>
    );
    
    expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument();
    expect(screen.getByRole('textbox')).toBeInTheDocument();
  });

  it('navigates when search form is submitted with a query', () => {
    const { container } = render(
      <I18nextProvider i18n={i18n}>
        <MemoryRouter>
          <HomeView />
        </MemoryRouter>
      </I18nextProvider>
    );
    
    const input = screen.getByRole('textbox');
    fireEvent.change(input, { target: { value: 'chicken' } });
    
    const form = container.querySelector('form');
    expect(form).not.toBeNull();
    fireEvent.submit(form!);
    
    expect(mockNavigate).toHaveBeenCalledWith('/product/chicken');
  });

  it('renders recent scans if present in the store', () => {
    vi.mocked(useAppStore).mockImplementation((selector: any) => 
      selector({ 
        recentScans: [
          { 
            id: '1', 
            query: 'beef', 
            timestamp: Date.now(), 
            result: { productName: 'Premium Beef', rating: 9 } 
          }
        ] 
      })
    );
    
    render(
      <I18nextProvider i18n={i18n}>
        <MemoryRouter>
          <HomeView />
        </MemoryRouter>
      </I18nextProvider>
    );

    expect(screen.getByText('Premium Beef')).toBeInTheDocument();
    expect(screen.getByText('9/10')).toBeInTheDocument();
  });
});
