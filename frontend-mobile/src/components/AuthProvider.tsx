import React, { createContext, useContext, useEffect, useState } from 'react';
import keycloak from '../api/keycloak';

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  login: () => void;
  logout: () => void;
  token?: string;
}

const AuthContext = createContext<AuthContextType>({
  isAuthenticated: false,
  isLoading: true,
  login: () => {},
  logout: () => {},
});

export const useAuth = () => useContext(AuthContext);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;
    
    keycloak.init({ onLoad: 'check-sso', checkLoginIframe: false })
      .then((authenticated) => {
        if (isMounted) {
          setIsAuthenticated(authenticated);
          setIsLoading(false);
        }
      })
      .catch((error) => {
        console.error("Keycloak initialization failed", error);
        if (isMounted) setIsLoading(false);
      });

    keycloak.onTokenExpired = () => {
      keycloak.updateToken(30).catch(() => {
        console.error('Failed to refresh token');
        keycloak.logout();
      });
    };

    return () => {
      isMounted = false;
    };
  }, []);

  const login = () => keycloak.login();
  const logout = () => keycloak.logout();

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading, login, logout, token: keycloak.token }}>
      {children}
    </AuthContext.Provider>
  );
};
