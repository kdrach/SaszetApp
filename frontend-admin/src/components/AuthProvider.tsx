import React, { createContext, useContext, useEffect, useState } from 'react';
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

let didInit = false;

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [initialized, setInitialized] = useState(false);
  const [token, setToken] = useState<string | undefined>(keycloak.token);

  useEffect(() => {
    if (didInit) return;
    didInit = true;

    keycloak.init({ onLoad: 'login-required', pkceMethod: 'S256' })
      .then((authenticated) => {
        setIsAuthenticated(authenticated);
        setInitialized(true);
        setIsLoading(false);
        setToken(keycloak.token);
      })
      .catch((error) => {
        console.error("Keycloak initialization failed", error);
        setInitialized(true);
        setIsLoading(false);
      });

    keycloak.onTokenExpired = () => {
      keycloak.updateToken(30).catch(() => {
        console.error('Failed to refresh token');
        keycloak.logout();
      });
    };

    keycloak.onAuthRefreshSuccess = () => setToken(keycloak.token);
    keycloak.onAuthSuccess = () => setToken(keycloak.token);
  }, []);

  const login = () => keycloak.login();
  const logout = () => keycloak.logout();

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading, login, logout, token, keycloak, initialized }}>
      {children}
    </AuthContext.Provider>
  );
};
