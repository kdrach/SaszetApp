import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App.tsx';
import './index.css';
import './i18n';

import keycloak from './api/keycloak';

keycloak.init({ onLoad: 'login-required', checkLoginIframe: false }).then((authenticated) => {
  if (authenticated) {
    ReactDOM.createRoot(document.getElementById('root')!).render(
      <React.StrictMode>
        <App />
      </React.StrictMode>,
    );
  } else {
    window.location.reload();
  }
}).catch((error) => {
  console.error("Keycloak initialization failed", error);
});
