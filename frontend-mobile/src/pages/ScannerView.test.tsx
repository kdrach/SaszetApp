import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ScannerView from './ScannerView';
import { BrowserRouter } from 'react-router-dom';

vi.mock('html5-qrcode', () => {
  return {
    Html5Qrcode: class {
      static getCameras = vi.fn().mockResolvedValue([{ id: 'cam1', label: 'Camera 1' }]);
      start = vi.fn().mockResolvedValue(undefined);
      stop = vi.fn().mockResolvedValue(undefined);
      isScanning = true;
    }
  };
});

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key })
}));

describe('ScannerView', () => {
  it('renders correctly', async () => {
    render(<BrowserRouter><ScannerView /></BrowserRouter>);
    expect(await screen.findByText('modeEan')).toBeInTheDocument();
  });
  
  it('changes mode to photo and shows capture buttons and file input', async () => {
    render(<BrowserRouter><ScannerView /></BrowserRouter>);
    const photoModeBtn = await screen.findByText('modePhoto');
    fireEvent.click(photoModeBtn);
    expect(await screen.findByText('takePhoto')).toBeInTheDocument();
    expect(await screen.findByText('scanIngredients')).toBeInTheDocument();
    expect(await screen.findByText('scanFrontPackaging')).toBeInTheDocument();
    expect(document.querySelector('input[type="file"]')).toBeInTheDocument();
  });
});
