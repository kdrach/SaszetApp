import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import ProviderCard from './ProviderCard';

describe('ProviderCard', () => {
  it('renders provider name and tests connection correctly', async () => {
    const onSave = vi.fn();
    const onTest = vi.fn().mockResolvedValue(true);
    const onSetPrimary = vi.fn();

    const existingData = {
      id: '123',
      providerName: 'OpenAI',
      modelName: 'gpt-4o',
      isPrimary: false,
      isActive: true
    };

    render(
      <ProviderCard 
        providerName="OpenAI" 
        existingData={existingData}
        isGlobalPrimary="456"
        onSave={onSave}
        onTest={onTest}
        onSetPrimary={onSetPrimary}
      />
    );

    expect(screen.getByText('OpenAI')).toBeInTheDocument();
    expect(screen.getByDisplayValue('gpt-4o')).toBeInTheDocument();

    const testBtn = screen.getByText('Testuj');
    fireEvent.click(testBtn);

    expect(onTest).toHaveBeenCalledWith('123');
  });
});
