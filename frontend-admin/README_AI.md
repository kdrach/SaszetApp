# Admin Frontend (PWA)

## Architecture & Navigation
- This is the desktop-first Progressive Web Application for the Admin Gateway.
- Uses **React, Vite, Tailwind v4, and React Router**.
- **`src/layouts/`**: `AdminLayout.tsx` contains the desktop sidebar structure.
- **`src/pages/`**: Views like `AdminDashboardView.tsx`.
- **`src/components/`**: Reusable parts like `ProviderCard.tsx`.
- **`src/api/`**: Interacts with the backend, appending Keycloak JWT tokens via Axios interceptor.

## Core Patterns & Rules
- **Testing**: Vitest + React Testing Library are used. Tests should be named `*.test.tsx`.
- **Auth**: Fully secured by Keycloak via `@react-keycloak/web`. Token checks must wrap protected views.
- **State**: Currently relies on React's local state, as the scope is limited to config management.
- Always use the predefined Tailwind CSS variables located in `index.css`.

## Recent Feature Context
- Initialized app with LLM Provider configuration dashboard (Epic 3). Provides ability to test connections and swap primary LLM routes.
