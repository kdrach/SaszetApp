import React from 'react';
import { createRoot } from 'react-dom/client';
import { ReactKeycloakProvider } from '@react-keycloak/web';
import App from './App';
import keycloak from './keycloak';
import './index.css';

const rootElement = document.getElementById('root');
if (rootElement) {
  const root = createRoot(rootElement);
  
  root.render(
    <React.StrictMode>
      <ReactKeycloakProvider 
        authClient={keycloak}
        initOptions={{ onLoad: 'login-required', pkceMethod: 'S256' }}
        LoadingComponent={<div className="flex items-center justify-center min-h-screen bg-background">Loading authentication...</div>}
      >
        <App />
      </ReactKeycloakProvider>
    </React.StrictMode>
  );
}
