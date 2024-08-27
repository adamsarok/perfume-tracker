import { expect, test, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import Home from './page'

test('renders home page links', async () => {
    render(await Home());
    expect(screen.getByText('New Perfume')).toBeDefined();

    expect(screen.getByRole('button', { name: /Spray On/i })).toBeDefined();

    expect(screen.getByTestId('perfume-selector')).toBeDefined();

    //TODO complete
});

  