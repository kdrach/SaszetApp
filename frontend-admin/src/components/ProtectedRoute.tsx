import React from 'react';
import { useAuth } from './AuthProvider';

interface ProtectedRouteProps {
  children: React.ReactElement;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const { isAuthenticated, keycloak } = useAuth();

  if (!isAuthenticated) {
    return null;
  }

  if (keycloak.hasRealmRole && !keycloak.hasRealmRole('admin')) {
    return (
      <div className="flex h-screen w-full items-center justify-center bg-gray-100">
        <div className="rounded-lg bg-white p-8 text-center shadow-xl">
          <h1 className="mb-4 text-3xl font-bold text-red-600">Access Denied</h1>
          <p className="text-gray-600">You must be an administrator to access this page.</p>
        </div>
      </div>
    );
  }

  return children;
};

export default ProtectedRoute;
