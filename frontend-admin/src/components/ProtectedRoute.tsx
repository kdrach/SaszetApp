import React from 'react';

import { useKeycloak } from '@react-keycloak/web';

interface ProtectedRouteProps {
  children: React.ReactElement;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const { keycloak } = useKeycloak();

  React.useEffect(() => {
    if (!keycloak.authenticated) {
      keycloak.login();
    }
  }, [keycloak]);

  if (!keycloak.authenticated) {
    return null;
  }

  return children;
};

export default ProtectedRoute;
