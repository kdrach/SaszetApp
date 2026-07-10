import React from 'react';
import { useAuth } from './AuthProvider';

interface ProtectedRouteProps {
  children: React.ReactElement;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const { isAuthenticated, isLoading, keycloak } = useAuth();

  if (isLoading) {
    return <div className="flex items-center justify-center min-h-screen bg-background text-gray-900">Loading authentication...</div>;
  }

  if (!isAuthenticated) {
    return (
      <div className="flex h-screen w-full items-center justify-center bg-gray-100">
        <div className="rounded-lg bg-white p-8 text-center shadow-xl">
          <h1 className="mb-4 text-3xl font-bold text-red-600">Authentication Failed</h1>
          <p className="mb-6 text-gray-600">Unable to authenticate with the server.</p>
          <button 
            onClick={() => window.location.reload()}
            className="rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-opacity-50"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  if (!keycloak.hasRealmRole || !keycloak.hasRealmRole('admin')) {
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
