Product Requirements Document (PRD)

[AI SYSTEM CONTEXT & INSTRUCTIONS]

You are an autonomous coding agent/developer AI. This PRD is your absolute source of truth.

Adhere strictly to the architecture, constraints, and schemas defined below.

Treat keywords MUST, MUST NOT, REQUIRED, SHALL, SHALL NOT, SHOULD, SHOULD NOT, RECOMMENDED, MAY, and OPTIONAL in accordance with RFC 2119.

Do not introduce new technologies or databases outside of the specified stack without explicit user prompt.

Project Name: SaszetApp
Version: 1.3 (AI-Optimized + Text Search + i18n)
Last Updated: July 2026

1. Executive Summary & Vision

The SaszetApp is a mobile-first application (PWA) designed to analyze pet food ingredients via barcode scans, manual text/name search, or label photos.
Core Business Logic: To ensure cost-effectiveness, the system MUST aggressively cache Vision-Language Model (VLM) responses. Direct VLM API calls are a fallback ONLY when the product is absent from the local cache. The application is multi-language, with Polish (PL) as the primary/base language and English (EN) as the secondary language.

2. Architecture & Containerization Stack

The system MUST be deployed using Docker Compose. A strict Separation of Concerns pattern is REQUIRED for databases.

| Container Name | Role | Technology | Description |
| frontend-pwa | Client App | React / Vue (PWA) | Browser-based UI, accesses device camera, calls backend-api. |
| backend-api | Core Server | ASP.NET Core (C#) | RESTful API, handles VLM routing, business logic, DB caching. |
| keycloak | Identity Provider | Keycloak | Handles OAuth2/OIDC, issues JWTs for admin and PWA. |
| keycloak-db | Auth DB | PostgreSQL (Instance A) | MUST be entirely isolated. Used ONLY by Keycloak. |
| app-db | Core DB | PostgreSQL (Instance B) | Used ONLY by backend-api. Stores products, caching, and encrypted VLM API keys. |

3. Functional Requirements

3.1. Frontend Module (PWA)

REQ-FE-1 (Barcode & Search): The PWA MUST use the device camera to read EAN-13 barcodes AND provide a text input field to allow users to search for products by name or brand.

REQ-FE-2 (Fallback Camera): If backend-api returns a 404 for an EAN or text search, the UI MUST prompt the user to capture a high-resolution photo of the ingredients label.

REQ-FE-3 (State Management): The PWA MUST handle loading states gracefully while waiting for VLM inference (which may take 5-15 seconds).

REQ-FE-4 (Internationalization - i18n): The PWA MUST support multiple languages. Polish (PL) is the base/default language. English (EN) MUST be available as a secondary option. The user's language preference MUST be sent in API requests via headers (e.g., Accept-Language).

3.2. Backend Core Flow (ASP.NET Core)

REQ-BE-1 (Cache First): On EAN scan or text search query, backend-api MUST query app-db filtering by both the EAN/Name AND the user's requested language. If a valid PetFoodItem exists for that language, return it immediately (Bypass VLM).

REQ-BE-2 (VLM Router & i18n): If cache misses, backend-api MUST construct a system prompt that explicitly instructs the active Primary LLM Provider to return the analysis in the user's requested language (PL or EN).

REQ-BE-3 (JSON Enforcement): The backend MUST force the VLM to output raw JSON (response_format: { type: "json_object" } where applicable).

REQ-BE-4 (Persistence): Successfully parsed VLM responses MUST be mapped to the database entity, tagged with the response language, and saved to app-db before returning to the frontend.

3.3. Admin Gateway Module

REQ-AD-1 (Auth): Admin endpoints MUST be secured via JWT verification (Keycloak). Requires role claim: admin.

REQ-AD-2 (CRUD Providers): Support configuration for Google Gemini, OpenAI, Anthropic Claude.

REQ-AD-3 (Active State): Exactly ONE provider MUST be marked as IsPrimary = true at any given time.

4. Security & Cryptography (CRITICAL)

SEC-1 (Key Encryption): LLM API Keys entered in the Admin Panel MUST NOT be stored in plain text in app-db.

SEC-2 (AES-256): backend-api MUST use symmetric encryption (AES-256) for API Keys.

SEC-3 (Env Vars): The Master Encryption Key MUST be provided to backend-api strictly via the ENCRYPTION_KEY environment variable.

SEC-4 (Statelessness): backend-api MUST be stateless. No session storage. Auth relies on JWT signature validation against Keycloak's public keys.

5. Data Contracts & Schemas

5.1. VLM Output Contract (TypeScript & C# Representation)

The system expects the VLM to return EXACTLY this JSON structure.

// AI Agent Instruction: Use this interface to create C# DTOs and Frontend Types.
interface VLMResponseContract {
  productName: string; // The identified name of the product from the label
  rating: number; // Integer between 1 and 10
  pros: string[]; // Array of positive dietary traits in the requested language
  cons: string[]; // Array of negative dietary traits in the requested language
  summary: string; // 1-2 sentences summarizing the nutritional value in the requested language
  extractedIngredients: string; // Raw string of ingredients in the requested language
}


5.2. Core Database Entities (Application DB)

Table: LlmProviders

Id (UUID, PK)

ProviderName (String: "Gemini", "OpenAI", "Claude")

ModelName (String: e.g., "gemini-1.5-flash")

EncryptedApiKey (String)

IsPrimary (Boolean)

IsActive (Boolean)

Table: PetFoodItems

Id (UUID, PK)

EanCode (String, Nullable, Indexed)

ProductName (String, Indexed) - Used for text-based searches

Language (String, Indexed) - e.g., "pl" or "en" to support multilingual caching

Rating (Int)

Pros (JSONB / String Array)

Cons (JSONB / String Array)

Summary (Text)

ExtractedIngredients (Text)

CreatedAt (Timestamp)