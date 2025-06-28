import { useAuth } from "@/hooks/use-auth";
import { ReactNode } from "react";

interface AuthGuardProps {
  children: ReactNode;
  fallback?: ReactNode;
  requireAuth?: boolean;
}

export function AuthGuard({ 
  children, 
  fallback = <div className="text-center p-4 text-gray-500">Please log in to continue</div>,
  requireAuth = true 
}: AuthGuardProps) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <div className="text-center p-4">Loading...</div>;
  }

  if (requireAuth && !isAuthenticated) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}

// Hook for forms that need to be disabled when not authenticated
export function useFormAuth() {
  const { isAuthenticated, isLoading } = useAuth();
  
  return {
    isDisabled: !isAuthenticated || isLoading,
    isAuthenticated,
    isLoading,
  };
} 