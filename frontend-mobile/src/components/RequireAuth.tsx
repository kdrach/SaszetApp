import React, { useEffect } from 'react';
import { useAuth } from './AuthProvider';
import LoadingOverlay from './LoadingOverlay';

export const RequireAuth: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading, login } = useAuth();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      login();
    }
  }, [isLoading, isAuthenticated, login]);

  if (isLoading || !isAuthenticated) {
    return <LoadingOverlay />;
  }

  return <>{children}</>;
};
