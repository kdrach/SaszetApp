import React from 'react';
import { createRoot } from 'react-dom/client';
import { AuthProvider } from './components/AuthProvider';
import App from './App';
import keycloak from './keycloak';
import './index.css';

const rootElement = document.getElementById('root');
if (rootElement) {
  const root = createRoot(rootElement);
  
  root.render(
    <React.StrictMode>
      <AuthProvider>
        <App />
      </AuthProvider>
    </React.StrictMode>
  );
}
