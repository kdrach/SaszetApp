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

export let didInit = false;
export const __resetAuthInitForTests = () => { didInit = false; };

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [token, setToken] = useState<string | undefined>(keycloak.token);

  useEffect(() => {
    if (didInit) return;
    didInit = true;

    keycloak.init({ onLoad: 'login-required', checkLoginIframe: false })
      .then((authenticated) => {
        setIsAuthenticated(authenticated);
        setIsLoading(false);
        setToken(keycloak.token);
      })
      .catch((error) => {
        console.error("Keycloak initialization failed", error);
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
    <AuthContext.Provider value={{ isAuthenticated, isLoading, login, logout, token }}>
      {children}
    </AuthContext.Provider>
  );
};
