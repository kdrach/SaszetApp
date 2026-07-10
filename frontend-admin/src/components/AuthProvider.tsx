import React, { createContext, useContext, useEffect, useState, useRef } from 'react';
import keycloak from '../keycloak';
import Keycloak from 'keycloak-js';

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  login: () => void;
  logout: () => void;
  token?: string;
  keycloak: Keycloak;
  initialized: boolean;
}

const AuthContext = createContext<AuthContextType>({
  isAuthenticated: false,
  isLoading: true,
  login: () => {},
  logout: () => {},
  keycloak: keycloak,
  initialized: false
});

export const useAuth = () => useContext(AuthContext);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [initialized, setInitialized] = useState(false);
  const isRun = useRef(false);

  useEffect(() => {
    if (isRun.current) return;
    isRun.current = true;

    let isMounted = true;
    
    keycloak.init({ onLoad: 'login-required', pkceMethod: 'S256' })
      .then((authenticated) => {
        if (isMounted) {
          setIsAuthenticated(authenticated);
          setInitialized(true);
          setIsLoading(false);
        }
      })
      .catch((error) => {
        console.error("Keycloak initialization failed", error);
        if (isMounted) {
          setInitialized(true);
          setIsLoading(false);
        }
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
    <AuthContext.Provider value={{ isAuthenticated, isLoading, login, logout, token: keycloak.token, keycloak, initialized }}>
      {children}
    </AuthContext.Provider>
  );
};
