UI/UX Specifications Document

[AI SYSTEM CONTEXT & INSTRUCTIONS]
You are an autonomous frontend developer AI. This document is your absolute source of truth for all UI/UX decisions, component structures, and styling.

Framework Agnostic but Style Specific: Implement these layouts using standard modern web frameworks (React, Vue) with Tailwind CSS for styling.

Design Paradigm: STRICTLY Mobile-First for the End-User App (PWA). Desktop-First for the Admin Gateway (also an installable PWA).

Internationalization (i18n): All UI strings MUST be extracted into language files (e.g., using react-i18next). Default language is Polish (PL), fallback/secondary is English (EN).

Accessibility: Ensure all touch targets are at least 44x44px. Use semantic HTML. Ensure high contrast for text.

Project Name: SaszetApp
Document: UI/UX Specifications & User Flows
Version: 1.3
Last Updated: July 2026

1. Design System & Theme

To maintain a clean, trustworthy, and modern aesthetic (Native iOS/Android feel).

Primary Color: #10B981 (Emerald Green) - Conveys health, nature, and positive ratings.

Secondary Color: #3B82F6 (Blue) - Used for search or alternative actions.

Warning/Negative Color: #EF4444 (Red) - Used for low ratings or bad ingredients.

Background Colors: #F2F2F7 (iOS System Gray 6) for app background, #FFFFFF for cards/lists.

Typography: Modern Sans-Serif (Inter or system-default Apple/Roboto).

Border Radius: rounded-xl or rounded-2xl (12px - 16px) for cards.

UI Elements: Use native-looking grouped lists (like iOS Settings), large titles, and transparent glassmorphism for bottom navigation.

2. End-User PWA (Mobile-First Interface)

The End-User application consists of three primary screens navigated via a Bottom Tab Bar.

2.1. Home & Search Screen (/)

Purpose: The entry point. Allows users to quickly search by text.
Layout Elements:

Large Header: App title "SaszetApp" and a Segmented Control for Language Switcher (PL/EN toggle) in the top right.

Hero Section: Large friendly title (e.g., "Odkrywaj / Discover") adapting dynamically to scrolling (Native Large Title behavior).

Search Bar: A prominent text input field with a search icon. Localized Placeholder (e.g., "Wpisz nazwę karmy..." / "Enter pet food name...").

Grouped List (Recent Scans): A native-style grouped list (ul > li with separators) showing recently scanned products from local storage.

Bottom Navigation Bar: Fixed at the bottom with glassmorphism blur. Contains icons for Home (Left), a Barcode Scan button (Center-Left), an Ingredients Photo Scan button (Center-Right), and Profile/Settings (Right). The Ingredients Scan button triggers a native OS camera launch (`<input type="file" capture="environment">`).

2.2. Barcode Scanner Screen (/scan)

Purpose: Camera viewport for capturing EAN codes.
Layout Elements:

Immersive View: 100% full-screen camera view (black background). Bottom navigation is hidden.

Top Bar: Back button (left), Flashlight toggle (right), floating over the camera view.

Overlay: A clear "cutout" box guiding the user where to place the barcode/label.

Instruction Text (Localized): Floating text badge.

State 1 (EAN): "Umieść kod w ramce" / "Place barcode inside the frame"

2.3. Analysis Results Screen (/product/{id})

Purpose: Displaying the VLM output or cached database result.
Layout Elements:

Header: Back button, Share button. Native styling, sticky top.

Product Title: Large heading showing the productName.

Rating Card (Hero):

A prominent badge showing the rating (e.g., "8/10") centered.

Color gradient based on score (1-4: Red gradient, 5-7: Yellow gradient, 8-10: Emerald gradient).

Text block showing the AI-generated summary.

Pros & Cons Section (Grouped Lists):

Pros: A native-style list with green indicators.

Cons: A native-style list with red indicators.

Raw Ingredients Accordion: A collapsible section showing extractedIngredients.

2.4. Loading State (Global)

Animation: When waiting for the VLM response.

Text (Localized): Rotating playful tips (e.g., "Czytam etykietę...", "Konsultacja z AI weterynarzem...", "Szukam mięsa...").

3. Admin Gateway (Desktop-First PWA)

The Admin Gateway is a dedicated, separate desktop-first Progressive Web App (PWA) for configuring the global LLM routing. It is NOT part of the end-user PWA.

3.1. Admin Dashboard (/)

Purpose: Overview and global configuration of AI providers affecting all end-users.
Layout Elements:

Sidebar: Fixed left sidebar containing App Title (Admin), User Info, and Logout button (Keycloak integration).

Top Bar: Page Title ("Konfiguracja LLM") and connection status.

Global Route Selector: A dedicated prominent card to select the "Primary Provider" (the default route for all mobile app traffic).

3.2. Provider Cards (Grid Layout)

A CSS Grid displaying cards for each supported provider (Google Gemini, OpenAI, Anthropic Claude).
Card Elements (Per Provider):

Header: Provider Logo/Name and an Active/Inactive Native Toggle Switch.

Model Selection: Dropdown to select the specific VLM model.

API Key Input: Password-type input field for the API Key. Masked by default.

Action Footer:

"Testuj" (Ping API) - Left side.

"Zapisz" (Save Configuration) - Right side.

Status Badge: Visual indicator of whether the key is configured and tested.

4. Technical Implementation Directives (For AI)

To ensure smooth generation and preview rendering, adhere to these structural constraints:

4.1. Routing Map

Implement client-side routers for both applications.
End-User App:

/ -> HomeView

/scan -> ScannerView

/product/:id -> ResultView

Admin App:

/ -> AdminDashboardView (Must be wrapped in a Protected Route check for Keycloak auth).

4.2. State Management

i18n: Use an i18n context/provider to manage the current language (PL/EN) globally in the End-User App.

Global State: Use a lightweight state manager (e.g., Context API or Zustand) to store JWT Tokens and Recent scanned EAN codes.

4.3. Folder Architecture

Separate UI concerns strictly for BOTH apps:

src/components/ (Reusable atoms/molecules: Buttons, Cards, Icons)

src/pages/ (Full views corresponding to the Routing Map)

src/locales/ (JSON files for PL and EN strings)

src/api/ (API wrapper functions for backend communication)

4.4. Mocking Strategy (Crucial)

API Mocks: Before writing integration logic with the C# backend, create mock services (e.g., returning a hardcoded VLMResponseContract object). This allows immediate visual verification of all states in the AI's internal browser preview.