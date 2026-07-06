import Keycloak from 'keycloak-js';

const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL || 'http://localhost:8080',
  realm: 'petfood-realm',
  clientId: 'saszetapp-admin'
});

export default keycloak;
