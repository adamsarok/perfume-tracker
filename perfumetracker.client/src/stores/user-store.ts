import { create } from 'zustand';
import { UserResponse } from '@/services/user-service';

interface UserState {
  user: UserResponse | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  setUser: (user: UserResponse | null) => void;
  setLoading: (loading: boolean) => void;
  clearUser: () => void;
}

export const useUserStore = create<UserState>((set) => ({
  user: null,
  isLoading: true,
  isAuthenticated: false,
  setUser: (user) => set({ 
    user, 
    isAuthenticated: !!user,
    isLoading: false 
  }),
  setLoading: (isLoading) => set({ isLoading }),
  clearUser: () => set({ 
    user: null, 
    isAuthenticated: false, 
    isLoading: false 
  }),
})); 