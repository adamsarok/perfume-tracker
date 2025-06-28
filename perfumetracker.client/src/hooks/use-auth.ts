import { showError } from '@/services/toasty-service';
import { useUserStore } from '@/stores/user-store';

export const useAuth = () => {
  const { user, isAuthenticated, isLoading } = useUserStore();

  const guardAction = () => {
    if (isLoading) {
      showError('User is loading.');
      return true;
    }
    if (user?.roles?.includes("Demo")) { 
      showError('Demo mode: changes are not saved.');
      return true;
    }
    return false;
  }

  return {
    user,
    isAuthenticated,
    isLoading,
    isDemo: user?.roles?.includes("Demo") ?? false,
    guardAction,
  };
}; 