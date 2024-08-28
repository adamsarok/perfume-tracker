import { expect, test, vi } from 'vitest'
import { act, fireEvent, render, screen } from '@testing-library/react'
import PerfumeSelector from '@/components/perfume-selector';
import { Perfume } from '@prisma/client';
import { useRouter } from 'next/navigation';

const mockRefresh = vi.fn()
    vi.mock('next/navigation', () => ({
    useRouter: () => ({
        refresh: mockRefresh
    })
}))
  
  vi.mock('@/db/perfume-repo', () => ({
    getPerfumesForSelector: vi.fn().mockResolvedValue([])
  }))

test('PerfumeSelector renders and handles selection', async () => {
    const mockPerfumes: Perfume[] = [
        {
            id: 1, house: 'Door', perfume: 'Sausage',
            rating: 10,
            notes: 'cool perfume',
            ml: 50,
            imageObjectKey: ''
        },
        {
            id: 2, house: 'Channel', perfume: 'Blue Channel',
            rating: 7,
            notes: 'so blue',
            ml: 100,
            imageObjectKey: ''
        }
    ];
 
    await act(async () => {
        render(<PerfumeSelector perfumes={mockPerfumes} />);
    });
    
    const autocomplete = screen.getByRole('combobox');
    fireEvent.change(autocomplete, { target: { value: 'Sausage' } });
    
    fireEvent.click(autocomplete);
    
    await act(() => {
        const sprayButton = screen.getByText('Spray On');
        fireEvent.click(sprayButton);
    });

    const mockRouter = vi.mocked(useRouter())
    expect(mockRefresh).toHaveBeenCalled()

}); 