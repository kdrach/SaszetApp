import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import BottomTabBar from './BottomTabBar';

describe('BottomTabBar', () => {
  it('renders navigation links on home route', () => {
    const { container } = render(
      <MemoryRouter initialEntries={['/']}>
        <BottomTabBar />
      </MemoryRouter>
    );
    
    const links = container.querySelectorAll('a');
    expect(links).toHaveLength(3);
    expect(links[0].getAttribute('href')).toBe('/');
    expect(links[1].getAttribute('href')).toBe('/scan');
    expect(links[2].getAttribute('href')).toBe('/profile');
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
