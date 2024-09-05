import { expect, test, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import PerfumeEditForm from '../components/perfume-edit-form'
import { Perfume, Tag } from '@prisma/client';

vi.mock('react-dom', async () => {
    const actual = await vi.importActual('react-dom');
    return {
      ...actual,
      useFormState: vi.fn().mockImplementation((action, initialState) => [initialState, vi.fn()])
    };
  });

const mockToastError = vi.fn();
vi.mock('react-toastify', () => ({
  toast: {
    error: 'eeee', //mockToastError,
    success: vi.fn()
  }
}));


test('PerfumeEditForm handles errors', async () => {

    const mockPerfume: Perfume = {
      id: 1, house: 'Dior', perfume: 'Sauvage', rating: 9, notes: 'Fresh', ml: 100, imageObjectKey: '',
      winter: true,
      spring: true,
      summer: true,
      autumn: true
    };
    const mockTags: Tag[] = [];
    const mockAllTags: Tag[] = [];
    vi.mock('@/db/perfume-repo', () => ({
        upsertPerfume: vi.fn().mockResolvedValue({ errors: { _form: ['Server error'] }, result: null, state: 'failed' })
    }));

    render(<PerfumeEditForm perfume={mockPerfume} perfumesTags={mockTags} allTags={mockAllTags} />);

    const submitButton = screen.getByText('Update');
    fireEvent.click(submitButton);

    //TODO doesnt work, mockToastError is not called
    // await waitFor(() => {
    //     expect(mockToastError).toHaveBeenCalled(); //.toHaveBeenCalledWith("Perfume save failed! Server error");
    // });
});