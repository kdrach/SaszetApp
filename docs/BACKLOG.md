# SaszetApp - Project Backlog

## Epic 1: CI/CD & Infrastructure

### [Infrastructure] Setup Docker Compose & PostgreSQL Database
**Epic:** Epic 1: CI/CD & Infrastructure
**Objective:** Create the base `docker-compose.yml` file to host the PostgreSQL database instances.
**Blocked By:** None
**Files to Modify/Create:** `docker-compose.yml`
**Technical Instructions (For AI Agent):** Use modern `docker-compose.yml` syntax. Create two strictly separated PostgreSQL databases: `app-db` (for core API) and `keycloak-db` (for auth). Ensure strict separation of concerns.
**Acceptance Criteria (DoD):**
- [ ] `docker-compose.yml` is created with `app-db` and `keycloak-db` services.
- [ ] Databases are completely isolated from each other.

### [Infrastructure] Setup Keycloak Container & Realm Configuration
**Epic:** Epic 1: CI/CD & Infrastructure
**Objective:** Add Keycloak to the docker-compose setup and configure it to initialize a predefined realm on startup.
**Blocked By:** 1
**Files to Modify/Create:** `docker-compose.yml`, `infrastructure/keycloak/realm-export.json`
**Technical Instructions (For AI Agent):** Add the Keycloak image to `docker-compose.yml`. Mount the `realm-export.json` to auto-create the `petfood-realm` and the `admin` role automatically on container startup. Connect Keycloak solely to `keycloak-db`.
**Acceptance Criteria (DoD):**
- [ ] Keycloak service is added to `docker-compose.yml`.
- [ ] `realm-export.json` is created with `petfood-realm` and `admin` role.
- [ ] Keycloak successfully uses `keycloak-db`.

### [CI/CD] GitHub Actions: Unit & Integration Tests Workflow
**Epic:** Epic 1: CI/CD & Infrastructure
**Objective:** Create a GitHub Actions workflow to automatically run backend and frontend tests on every Pull Request.
**Blocked By:** None
**Files to Modify/Create:** `.github/workflows/test.yml`
**Technical Instructions (For AI Agent):** Configure the workflow to trigger on PRs. Include steps to run xUnit/NUnit tests for the .NET backend and Vitest/Cypress tests for the Vite React frontends.
**Acceptance Criteria (DoD):**
- [ ] `test.yml` workflow triggers on PR creation/update.
- [ ] Contains steps for .NET test execution.
- [ ] Contains steps for Vite React test execution.

### [CI/CD] GitHub Actions: AI Code Reviewer Integration
**Epic:** Epic 1: CI/CD & Infrastructure
**Objective:** Implement an automated AI Code Reviewer for Pull Requests to assist with code quality and architectural rules.
**Blocked By:** None
**Files to Modify/Create:** `.github/workflows/ai-pr-reviewer.yml`
**Technical Instructions (For AI Agent):** Utilize a GitHub Marketplace action or custom script to trigger an AI code review on every PR. Ensure it analyzes the diff against our architectural rules from `AGENT_RULES.md`.
**Acceptance Criteria (DoD):**
- [ ] Workflow is created and triggers on PRs.
- [ ] AI review is posted as comments on the Pull Request.

### [CI/CD] GitHub Actions: Docker Compose Build Test
**Epic:** Epic 1: CI/CD & Infrastructure
**Objective:** Verify that the entire Docker Compose environment builds and starts correctly on Pull Requests.
**Blocked By:** 1, 2
**Files to Modify/Create:** `.github/workflows/docker-build-test.yml`
**Technical Instructions (For AI Agent):** Run `docker compose build` and `docker compose up -d` in the CI environment to ensure there are no container startup crashes or missing dependencies.
**Acceptance Criteria (DoD):**
- [ ] Workflow triggers on PRs.
- [ ] Successfully builds and spins up all containers.
- [ ] Validates container health/status before passing.

---

## Epic 2: Core Backend API

### [Backend] Solution & Project Setup
**Epic:** Epic 2: Core Backend API
**Objective:** Initialize the ASP.NET Core Web API project with the appropriate folder structure.
**Blocked By:** 1
**Files to Modify/Create:** `backend/SaszetApp.Api.sln`, `backend/SaszetApp.Api/SaszetApp.Api.csproj`, `backend/SaszetApp.Api/Program.cs`
**Technical Instructions (For AI Agent):** Use .NET 8.0 or newer. Implement either Minimal APIs in `Program.cs` or a clean Controller-based structure. Set up a clear folder structure (e.g., `/Features`, `/Data`, `/Services`). Configure global exception handling middleware.
**Acceptance Criteria (DoD):**
- [ ] .NET 8 API project is successfully scaffolded.
- [ ] Clean architecture folders are created.
- [ ] Global exception handling is configured.

### [Backend] Create PetFoodItem Entity & EF Core Migration
**Epic:** Epic 2: Core Backend API
**Objective:** Implement the `PetFoodItem` entity and generate the initial EF Core migration for the Application Database.
**Blocked By:** 6
**Files to Modify/Create:** `backend/SaszetApp.Api/Data/AppDbContext.cs`, `backend/SaszetApp.Api/Entities/PetFoodItem.cs`
**Technical Instructions (For AI Agent):** Use EF Core with `Npgsql`. Create `PetFoodItem` with fields: Id (UUID), EanCode (Indexed), ProductName (Indexed), Language (Indexed), Rating (Int), Pros (JSONB), Cons (JSONB), Summary (Text), ExtractedIngredients (Text), CreatedAt (Timestamp).
**Acceptance Criteria (DoD):**
- [ ] `PetFoodItem` entity matches schema.
- [ ] DbContext is configured with Npgsql.
- [ ] Initial EF Core migration is generated.

### [Backend] Create LlmProvider Entity & Encryption Service
**Epic:** Epic 2: Core Backend API
**Objective:** Create the `LlmProvider` entity and implement an AES-256 encryption service for API keys.
**Blocked By:** 7
**Files to Modify/Create:** `backend/SaszetApp.Api/Entities/LlmProvider.cs`, `backend/SaszetApp.Api/Services/EncryptionService.cs`
**Technical Instructions (For AI Agent):** Create `LlmProvider` with fields: Id, ProviderName, ModelName, EncryptedApiKey, IsPrimary, IsActive. Implement AES-256 in `EncryptionService` using standard `System.Security.Cryptography`. The Master Key must be read from the `ENCRYPTION_KEY` environment variable. Never store plain text keys.
**Acceptance Criteria (DoD):**
- [ ] `LlmProvider` entity and migration created.
- [ ] `EncryptionService` correctly encrypts and decrypts strings using AES-256 and env var key.

### [Backend] Keycloak JWT Authentication Setup
**Epic:** Epic 2: Core Backend API
**Objective:** Secure the backend API by validating JWT tokens issued by Keycloak.
**Blocked By:** 6
**Files to Modify/Create:** `backend/SaszetApp.Api/Program.cs`, `backend/SaszetApp.Api/appsettings.json`
**Technical Instructions (For AI Agent):** Integrate `Microsoft.AspNetCore.Authentication.JwtBearer`. The backend must be stateless and rely on JWT signature validation against Keycloak's public keys. Do not use session storage.
**Acceptance Criteria (DoD):**
- [ ] JWT Bearer authentication is configured.
- [ ] Validates against Keycloak realm endpoint.

### [Backend] Define VLM API Contract & DTOs
**Epic:** Epic 2: Core Backend API
**Objective:** Create C# DTOs that strictly represent the expected JSON output from the Vision-Language Model.
**Blocked By:** 6
**Files to Modify/Create:** `backend/SaszetApp.Api/DTOs/VlmResponseContract.cs`
**Technical Instructions (For AI Agent):** Create a DTO matching the `VLMResponseContract` interface (productName, rating, pros, cons, summary, extractedIngredients). Use `System.Text.Json` attributes if necessary.
**Acceptance Criteria (DoD):**
- [ ] `VlmResponseContract` class matches the required JSON structure.
- [ ] Supports proper serialization/deserialization.

### [Backend] Endpoint: Scan & Search Cache Query
**Epic:** Epic 2: Core Backend API
**Objective:** Implement the cache-first endpoint for querying pet food items by EAN or Name and Language.
**Blocked By:** 7
**Files to Modify/Create:** `backend/SaszetApp.Api/Controllers/ScanController.cs` (or Minimal API in Program.cs)
**Technical Instructions (For AI Agent):** Endpoint must accept an EAN or text search query. It MUST read the `Accept-Language` header (PL or EN) and query `app-db` for a matching `PetFoodItem`. If found, return immediately (cache hit).
**Acceptance Criteria (DoD):**
- [ ] Endpoint accepts search string and reads `Accept-Language` header.
- [ ] Queries `AppDbContext` for EanCode or ProductName matching the language.
- [ ] Returns 200 OK with data if found, 404 if not found.

### [Backend] VLM Routing & Integration Service
**Epic:** Epic 2: Core Backend API
**Objective:** Implement the service to call the active VLM, enforce language/JSON constraints, and save the result.
**Blocked By:** 8, 10
**Files to Modify/Create:** `backend/SaszetApp.Api/Services/VlmService.cs`
**Technical Instructions (For AI Agent):** Service must fetch the provider where `IsPrimary = true`. Decrypt the API key. Construct a system prompt instructing the LLM to reply in the user's requested language and force JSON output (`response_format: { type: "json_object" }`). Map the response to `PetFoodItem`, tag it with the language, and save it to `app-db` before returning.
**Acceptance Criteria (DoD):**
- [ ] Correctly identifies the Primary provider.
- [ ] Prompts LLM for specific language and JSON object response.
- [ ] Saves the successful parsing result to the database as cache.

### [Backend] Endpoint: Admin Provider Configuration
**Epic:** Epic 2: Core Backend API
**Objective:** Create secure endpoints for the Admin Gateway to configure LLM providers.
**Blocked By:** 8, 9
**Files to Modify/Create:** `backend/SaszetApp.Api/Controllers/AdminProviderController.cs`
**Technical Instructions (For AI Agent):** Implement CRUD operations for `LlmProviders`. Ensure endpoints are secured via Keycloak JWT (Require role claim: admin). Ensure only one provider can be `IsPrimary = true`. Includes a "test connection" endpoint.
**Acceptance Criteria (DoD):**
- [ ] Endpoints require JWT and 'admin' role.
- [ ] API keys are passed encrypted to DB.
- [ ] Logic ensures IsPrimary uniqueness.

---

## Epic 3: Admin Gateway (Desktop PWA)

### [Admin Frontend] Initialize Vite React App
**Epic:** Epic 3: Admin Gateway (Desktop PWA)
**Objective:** Scaffold the React application for the Admin Gateway.
**Blocked By:** None
**Files to Modify/Create:** `frontend-admin/package.json`, `frontend-admin/vite.config.ts`
**Technical Instructions (For AI Agent):** Strictly use Vite (`npm create vite@latest -- --template react-ts`). Install and configure Tailwind CSS. Configure `vite-plugin-pwa`.
**Acceptance Criteria (DoD):**
- [ ] Vite + React app created.
- [ ] Tailwind CSS works.
- [ ] PWA plugin configured.

### [Admin Frontend] Configure Routing & Keycloak Auth
**Epic:** Epic 3: Admin Gateway (Desktop PWA)
**Objective:** Set up client-side routing and Keycloak protected routes for the admin interface.
**Blocked By:** 14
**Files to Modify/Create:** `frontend-admin/src/App.tsx`, `frontend-admin/src/components/ProtectedRoute.tsx`
**Technical Instructions (For AI Agent):** Use `react-router-dom`. Create a `ProtectedRoute` component that verifies Keycloak authentication before rendering the `/` route (`AdminDashboardView`).
**Acceptance Criteria (DoD):**
- [ ] Routing is functional.
- [ ] Access to `/` requires Keycloak login.

### [Admin Frontend] Admin Dashboard Layout
**Epic:** Epic 3: Admin Gateway (Desktop PWA)
**Objective:** Build the desktop-first main layout for the Admin UI.
**Blocked By:** 14
**Files to Modify/Create:** `frontend-admin/src/layouts/AdminLayout.tsx`
**Technical Instructions (For AI Agent):** Desktop-first design. Create a fixed left sidebar with App Title and Logout button. Create a top bar displaying the page title and status. Use Tailwind classes.
**Acceptance Criteria (DoD):**
- [ ] Layout matches Desktop-first specs.
- [ ] Sidebar and top bar are correctly positioned.

### [Admin Frontend] Provider Card Component
**Epic:** Epic 3: Admin Gateway (Desktop PWA)
**Objective:** Create the reusable UI card for configuring an individual LLM Provider.
**Blocked By:** 16
**Files to Modify/Create:** `frontend-admin/src/components/ProviderCard.tsx`
**Technical Instructions (For AI Agent):** Build a CSS Grid card. Header: Provider Logo/Name, Active Toggle. Body: Model selection dropdown, masked API key input. Footer: Test button, Save button. Status badge.
**Acceptance Criteria (DoD):**
- [ ] Component visually matches specifications.
- [ ] Inputs are correctly controlled.

### [Admin Frontend] API Integration: Providers
**Epic:** Epic 3: Admin Gateway (Desktop PWA)
**Objective:** Connect the Admin UI to the backend to fetch, save, and test LLM configurations.
**Blocked By:** 13, 17
**Files to Modify/Create:** `frontend-admin/src/api/providersApi.ts`, `frontend-admin/src/pages/AdminDashboardView.tsx`
**Technical Instructions (For AI Agent):** Use `axios` or `fetch`. Create API mock first for visual verification. Then integrate with the backend, passing the Keycloak JWT token in the Authorization header. Handle global route selector (setting IsPrimary).
**Acceptance Criteria (DoD):**
- [ ] API calls function correctly with JWT.
- [ ] UI state updates upon saving/testing.

---

## Epic 4: End-User App (Mobile PWA)

### [Mobile Frontend] Initialize Vite React App & PWA
**Epic:** Epic 4: End-User App (Mobile PWA)
**Objective:** Scaffold the React mobile application.
**Blocked By:** None
**Files to Modify/Create:** `frontend-mobile/package.json`, `frontend-mobile/vite.config.ts`
**Technical Instructions (For AI Agent):** Strictly use Vite (`npm create vite@latest -- --template react-ts`). Install Tailwind CSS. Add `vite-plugin-pwa`. Implement Mobile-First design paradigm. Use color palette: `#10B981`, `#3B82F6`, `#EF4444`.
**Acceptance Criteria (DoD):**
- [ ] Vite + React app created for mobile.
- [ ] Tailwind configured with brand colors.

### [Mobile Frontend] Setup i18n & Global State
**Epic:** Epic 4: End-User App (Mobile PWA)
**Objective:** Implement internationalization (PL/EN) and a global state store.
**Blocked By:** 19
**Files to Modify/Create:** `frontend-mobile/src/i18n.ts`, `frontend-mobile/src/store/useAppStore.ts`
**Technical Instructions (For AI Agent):** Extract all UI strings to language files using `react-i18next`. Default language is PL. Use `zustand` to store user's recent scanned EAN codes.
**Acceptance Criteria (DoD):**
- [ ] Language can be toggled between PL and EN.
- [ ] Zustand store is initialized for recent scans.

### [Mobile Frontend] Core Navigation & Layout
**Epic:** Epic 4: End-User App (Mobile PWA)
**Objective:** Implement the bottom tab bar and mobile layout structure.
**Blocked By:** 19
**Files to Modify/Create:** `frontend-mobile/src/App.tsx`, `frontend-mobile/src/components/BottomTabBar.tsx`
**Technical Instructions (For AI Agent):** Implement `react-router-dom` with routes: `/`, `/scan`, `/product/:id`. Build a fixed BottomTabBar with transparent glassmorphism blur. Icons: Home (left), elevated Scan (center), Profile (right).
**Acceptance Criteria (DoD):**
- [ ] Bottom navigation is visible and fixed.
- [ ] Routing transitions work correctly.

### [Mobile Frontend] Home & Search View
**Epic:** Epic 4: End-User App (Mobile PWA)
**Objective:** Build the entry screen with text search and recent scans list.
**Blocked By:** 20, 21
**Files to Modify/Create:** `frontend-mobile/src/pages/HomeView.tsx`, `frontend-mobile/src/components/SearchBar.tsx`
**Technical Instructions (For AI Agent):** Top right: Segmented Control for PL/EN switch. Large dynamic title ("Odkrywaj"). Prominent Search Bar with localized placeholder. Native-style grouped list for Recent Scans reading from Zustand store.
**Acceptance Criteria (DoD):**
- [ ] Language toggle updates text.
- [ ] Search Bar is functional.
- [ ] Recent scans display correctly.

### [Mobile Frontend] Scanner View (Camera)
**Epic:** Epic 4: End-User App (Mobile PWA)
**Objective:** Implement the camera view for capturing barcodes and labels.
**Blocked By:** 21
**Files to Modify/Create:** `frontend-mobile/src/pages/ScannerView.tsx`
**Technical Instructions (For AI Agent):** Use `@zxing/browser` or `html5-qrcode`. Immersive 100% full-screen black background (hide bottom nav). Floating top bar. Clear cutout overlay box. Bottom sheet to toggle between EAN and Photo modes. Floating instruction text.
**Acceptance Criteria (DoD):**
- [ ] Camera opens and requests permissions.
- [ ] UI elements float above camera feed.
- [ ] Toggles successfully between EAN/Photo mode.

### [Mobile Frontend] Analysis Results View
**Epic:** Epic 4: End-User App (Mobile PWA)
**Objective:** Build the detailed results screen showing VLM output.
**Blocked By:** 21
**Files to Modify/Create:** `frontend-mobile/src/pages/ResultView.tsx`
**Technical Instructions (For AI Agent):** Display product title. Rating Card with dynamic gradient depending on score (1-4 red, 5-7 yellow, 8-10 emerald). Native-style grouped lists for Pros (green) and Cons (red). Raw Ingredients accordion.
**Acceptance Criteria (DoD):**
- [ ] Rating color updates dynamically.
- [ ] Pros/Cons lists render correctly.

### [Mobile Frontend] API Integration & Global Loading State
**Epic:** Epic 4: End-User App (Mobile PWA)
**Objective:** Connect mobile UI to backend and handle the 5-15s loading delay.
**Blocked By:** 11, 24
**Files to Modify/Create:** `frontend-mobile/src/api/scanApi.ts`, `frontend-mobile/src/components/LoadingOverlay.tsx`
**Technical Instructions (For AI Agent):** Send `Accept-Language` header in requests. Create a global loading overlay with playful rotating text (e.g., "Czytam etykietę...", "Szukam mięsa...") to manage user expectations during VLM inference. Create API mocks first for frontend testing.
**Acceptance Criteria (DoD):**
- [ ] Correct language header is sent to API.
- [ ] Loading overlay shows rotating text during delay.
- [ ] Seamless transition to ResultView upon success.
