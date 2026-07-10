import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ResultView from './ResultView';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import * as scanApi from '../api/scanApi';

vi.mock('../api/scanApi', () => ({
  fetchAnalysisResult: vi.fn(),
  uploadImageForAnalysis: vi.fn()
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } })
}));

describe('ResultView', () => {
  it('renders invalid product ID if no ID', async () => {
    render(
      <MemoryRouter initialEntries={['/product']}>
        <Routes>
          <Route path="/product" element={<ResultView />} />
        </Routes>
      </MemoryRouter>
    );
    expect(await screen.findByText('invalid_product_id')).toBeInTheDocument();
  });

  it('renders loading initially and then result', async () => {
    vi.spyOn(scanApi, 'fetchAnalysisResult').mockResolvedValue({
      productName: 'Test Product',
      rating: 8,
      pros: ['pro1'],
      cons: ['con1'],
      summary: 'summary',
      extractedIngredients: 'ingredients'
    });

    render(
      <MemoryRouter initialEntries={['/product/123']}>
        <Routes>
          <Route path="/product/:id" element={<ResultView />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Test Product')).toBeInTheDocument();
    });
  });

  it('renders error popup when fetch fails', async () => {
    vi.spyOn(scanApi, 'fetchAnalysisResult').mockRejectedValue(new Error('Network error'));

    render(
      <MemoryRouter initialEntries={['/product/123']}>
        <Routes>
          <Route path="/product/:id" element={<ResultView />} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByText('scan_error')).toBeInTheDocument();
  });

  it('renders no_pet_food_found error when 422 with NO_PET_FOOD_FOUND errorCode is returned', async () => {
    vi.spyOn(scanApi, 'uploadImageForAnalysis').mockRejectedValue({
      response: {
        status: 422,
        data: { errorCode: 'NO_PET_FOOD_FOUND' }
      }
    });

    render(
      <MemoryRouter initialEntries={[{ pathname: '/product/photo', state: { imageBlob: new Blob(), scanMode: 'Ingredients' } }]}>
        <Routes>
          <Route path="/product/:id?" element={<ResultView />} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByText('no_pet_food_found')).toBeInTheDocument();
  });
});
