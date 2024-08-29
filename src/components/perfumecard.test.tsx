import { expect, test } from 'vitest'
import PerfumeCard from '../components/perfumecard'
import { WornWithPerfume } from '@/db/perfume-worn-repo';
import { act, render, screen  } from '@testing-library/react'

test('PerfumeCard snapshot', () => {
    
    const mockWorn: WornWithPerfume =  {
        id: 1,
        perfumeId: 1,
        wornOn: new Date('2024-03-15'),
        perfume: {
            id: 1, house: 'Door', perfume: 'Sausage', rating: 9, notes: 'Fresh', ml: 100,
            imageObjectKey: ''
        },
        tags: [{ id: 1, tag: 'Fresh', color: '#00ff00' }]
    };

    //const { asFragment } = render(<PerfumeCard worn={mockWorn} />);
    //expect(asFragment()).toMatchSnapshot();   // Snapshot test, if I ever need one?
    //await act(async () => {
    render(<PerfumeCard worn={mockWorn} />);
    //});
    expect(screen.getByText('Door - Sausage')).toBeDefined();
    expect(screen.getByText('SA')).toBeDefined();

    //expect(screen.getByRole('button', { name: /Spray On/i })).toBeDefined();
    //expect(screen.getByTestId('perfume-selector')).toBeDefined();
    //TODO: button etc?
});