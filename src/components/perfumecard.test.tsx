import { expect, test } from 'vitest'
import { render } from '@testing-library/react'
import PerfumeCard from '../components/perfumecard'
import { WornWithPerfume } from '@/db/perfume-worn-repo';

test('PerfumeCard snapshot', () => {
    // Snapshot test, if I ever need one?
    // const mockWorn: WornWithPerfume =  {
    //     id: 1,
    //     perfumeId: 1,
    //     wornOn: new Date('2024-03-15'),
    //     perfume: {
    //         id: 1, house: 'Dior', perfume: 'Sauvage', rating: 9, notes: 'Fresh', ml: 100,
    //         imageObjectKey: ''
    //     },
    //     tags: [{ id: 1, tag: 'Fresh', color: '#00ff00' }]
    // };

    // const { asFragment } = render(<PerfumeCard worn={mockWorn} />);
    // expect(asFragment()).toMatchSnapshot();
});