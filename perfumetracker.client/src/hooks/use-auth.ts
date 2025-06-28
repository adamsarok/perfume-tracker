import { useUserStore } from '@/stores/user-store';

export const useAuth = () => {
  const { user, isAuthenticated, isLoading } = useUserStore();

  return {
    user,
    isAuthenticated,
    isLoading,
    hasRole: (role: string) => user?.roles?.includes(role) ?? false,
    hasAnyRole: (roles: string[]) => user?.roles?.some(role => roles.includes(role)) ?? false,
  };
}; 