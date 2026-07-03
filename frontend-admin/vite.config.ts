import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'SaszetApp Admin Gateway',
        short_name: 'Admin',
        theme_color: '#10B981',
        background_color: '#F2F2F7',
        display: 'standalone'
      }
    })
  ],
  server: {
    port: 5173
  }
});
