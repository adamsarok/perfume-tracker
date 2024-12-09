import { test, vi } from 'vitest'


const mockRefresh = vi.fn()
    vi.mock('next/navigation', () => ({
    useRouter: () => ({
        refresh: mockRefresh
    })
}))
  

test('PerfumeSelector renders and handles selection', async () => {
    //TODO: fix this test
    // const mockPerfumes: PerfumeWithWornStatsDTO[] = [
    //     {
    //         perfume: 1, house: 'Door', perfumeName: 'Sausage',
    //         rating: 10,
    //         notes: 'cool perfume',
    //         ml: 50,
    //         imageObjectKey: '',
    //         winter: false,
    //         spring: false,
    //         summer: false,
    //         autumn: false
    //     },
    //     {
    //         id: 2, house: 'Channel', perfumeName: 'Blue Channel',
    //         rating: 7,
    //         notes: 'so blue',
    //         ml: 100,
    //         imageObjectKey: '',
    //         winter: false,
    //         spring: false,
    //         summer: false,
    //         autumn: false
    //     }
    // ];
 
    // await act(async () => {
    //     render(<PerfumeSelector perfumes={mockPerfumes} />);
    // });
    
    // const autocomplete = screen.getByRole('combobox');
    // fireEvent.change(autocomplete, { target: { value: 'Sausage' } });
    
    // fireEvent.click(autocomplete);
    
    // await act(() => {
    //     const sprayButton = screen.getByText('Spray On');
    //     fireEvent.click(sprayButton);
    // });

    // vi.mocked(useRouter())
    // expect(mockRefresh).toHaveBeenCalled()

}); 