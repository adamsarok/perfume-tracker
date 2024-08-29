import { expect, test, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import RecommendationsComponent from './recommendations-component'
import { UserPreferences } from '@/services/recommendation-service';

test('RecommendationsComponent fetches and displays recommendations', async () => {
    const mockUserPreferences: UserPreferences = {
        last3perfumes: {
            perfumes: [{
                perfume: { id: 1, house: 'Door', perfume: 'Sausage', rating: 10, notes: 'cool perfume', ml: 50, imageObjectKey: '' },
                wornTimes: 5,
                lastWorn: new Date(),
                tags: []
            }],
            tags: [{ id: 1, tag: 'Fresh', color: '#00ff00', wornCount: 3 }]
        },
        allTime: {
            perfumes: [],
            tags: []
        }
    };

    render(<RecommendationsComponent userPreferences={mockUserPreferences} />);

    //await waitFor(() => {
        expect(screen.getByText('Door - Sausage')).toBeDefined();
        //expect(screen.getByText('Fresh - 3')).toBeDefined();
    //});
});