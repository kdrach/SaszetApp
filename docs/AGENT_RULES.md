AI Agent Technical Directives & Constraints

[CONTEXT FOR AI AGENT]
You are generating code for the "SaszetApp" project. Always refer to docs/PRD.md for business logic and docs/UI_SPEC.md for UI/UX.
This document defines your technical boundaries, libraries, and coding standards. DO NOT deviate from these unless explicitly instructed by the user.

1. Tech Stack Requirements

1.1 Backend (C# / .NET)

Framework: .NET 10.0 (or newer) using ASP.NET Core Web API.

Architecture: Use Minimal APIs (Program.cs) for routing, or a clean Controller-based approach. Prefer a Vertical Slice Architecture or clean folder structure (e.g., /Features, /Data, /Services).

ORM: Entity Framework Core (EF Core) with Npgsql.EntityFrameworkCore.PostgreSQL.

Auth: Microsoft.AspNetCore.Authentication.JwtBearer configured to validate tokens against the Keycloak instance.

JSON: Use System.Text.Json.

1.2 Frontend PWA (React)

Scaffolding: STRICTLY use Vite (npm create vite@latest -- --template react). DO NOT use Create React App (CRA).

Styling: Tailwind CSS (v3 or v4).

Routing: react-router-dom (latest version).

State & Data Fetching: zustand for global state (if needed), axios or native fetch for API calls.

PWA Plugin: Use vite-plugin-pwa to automatically generate the manifest.json and service workers.

Scanner Library: Prefer @zxing/browser or html5-qrcode for robust barcode scanning.

i18n: Use react-i18next for translations.

1.3 Infrastructure (Docker)

Compose: Use modern docker-compose.yml syntax.

Keycloak Bootstrap: When generating the Keycloak container, provide instructions or a script to mount a realm-export.json file so the petfood-realm and admin roles are auto-created on startup.

2. Coding Standards for AI

Self-Contained Components: When writing React components, keep them modular. Extract icons to a separate import, use descriptive class names.

Environment Variables: Never hardcode URLs, API keys, or database credentials. Always use process.env (Node) or IConfiguration (C#) and map them to .env files.

C# Encryption: When implementing the LLM Provider configuration, provide a concrete implementation of AES-256 encryption/decryption for the EncryptedApiKey column using standard System.Security.Cryptography.

Error Handling: Implement global exception handling in the C# API (Middleware) and Error Boundaries in React.

No Placeholders: Write complete, functional code. Avoid using // ... rest of the code unless making a very specific, small diff.

3. Step-by-Step Generation Workflow (Instruction for AI)

When the user asks you to start building the app, execute tasks in this EXACT order to avoid breaking dependencies:

Step 1: Generate the docker-compose.yml and a dummy realm-export.json for Keycloak.

Step 2: Generate the ASP.NET Core solution, EF Core DbContext, and Entities.

Step 3: Generate the API Endpoints (Minimal APIs or Controllers).

Step 4: Generate the Vite + React frontend for the Admin Gateway (Desktop PWA).

Step 5: Generate the Vite + React frontend for the End-User App (Mobile PWA).