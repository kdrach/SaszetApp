# Mobile Frontend AI Context

## Architecture & Navigation
- **`src/components/`**: Reusable UI atoms and molecules (e.g., `BottomTabBar`, `LoadingOverlay`).
- **`src/pages/`**: Full views corresponding to the React Router map (`HomeView`, `ScannerView`, `ResultView`).
- **`src/locales/`**: JSON files for PL and EN strings used by `react-i18next`.
- **`src/api/`**: API wrapper functions for backend communication (`scanApi.ts`).
- **`src/store/`**: Global state management using Zustand (`useAppStore.ts`).

## Core Patterns & Rules
- **Styling**: strictly Tailwind CSS v4.
- **Routing**: `react-router-dom`.
- **Internationalization**: `react-i18next`. All strings MUST be extracted to locale JSON files.
- **State**: `zustand` is the preferred store for global state.
- **Device Features**: `html5-qrcode` is used for camera interactions.

## Recent Feature Context
- Initialized the entire Mobile PWA scaffolding using Vite, React, and Tailwind CSS.
- Implemented Home view with recent scans (persisted via Zustand).
- Implemented Scanner view with full-screen immersive camera UI.
- Implemented Result view displaying VLM output with dynamic gradients.
