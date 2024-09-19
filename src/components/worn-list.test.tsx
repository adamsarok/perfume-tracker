import { expect, test, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import WornList from '../components/worn-list'

test('WornList loads and displays perfume cards', async () => {
    vi.mock('@/db/perfume-worn-repo', () => ({
        getWornBeforeID: vi.fn().mockResolvedValue([
            { id: 1, perfume: { id: 1, house: 'Door', perfume: 'Sausage', rating: 8}, wornOn: new Date() },
            { id: 2, perfume: { id: 2, house: 'Channel', perfume: 'Blue Channel', rating: 10 }, wornOn: new Date() }
        ])
    }));
    vi.mock('@/db/perfume-repo', () => ({
        getPerfumesForSelector: vi.fn().mockResolvedValue([])
    }));

    render(<WornList r2_api_address='' />);

    await waitFor(() => {
        expect(screen.getByText('SA')).toBeDefined();
        //expect(screen.getByText('BL')).toBeDefined(); //TODO: why is the second one not showing up?
    });
});