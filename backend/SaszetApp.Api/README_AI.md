# SaszetApp.Api Backend Service

## Architecture & Navigation
This is a standard .NET 10.0 ASP.NET Core Web API project using Controllers and EF Core. 
- **Controllers/**: API Endpoints (e.g., `ScanController` for mobile app caching/VLM routing, `AdminProviderController` for backend LLM configuration).
- **Data/**: EF Core persistence layer. Contains the `AppDbContext` and entity classes (e.g. `PetFoodItemEntity`, `LlmProviderEntity`).
- **Data/Migrations/**: EF Core migrations.
- **Models/**: Logical/Domain models mapped from Entities.
- **DTOs/**: Data Transfer Objects strictly defining JSON contracts, like `VlmResponseContract`.
- **Services/**: Business logic. Includes `EncryptionService` (for AES-256 API key encryption) and `VlmService` (for integrating with LLM providers).
- **Services/Mappers/**: Mappers to translate between EF Entities and Domain Models.

## Core Patterns & Rules
- **Domain Mapping Rule**: As per the `ef-migration-domain-mapping` and `dotnet-entity-logical-model-mapping` skills, persistence entities must stay in `Data/` and be suffixed with `Entity`. Logical models go in `Models/`. Always use explicit mappers (`Services/Mappers/`) to convert between them before returning data from the database.
- **Stateless Auth**: The application uses stateless JWT bearer authentication validated against Keycloak.
- **Stateless VLM Calls**: The VLM routers do not cache state in memory. Results are persistently cached as `PetFoodItemEntity` in PostgreSQL.

## Recent Feature Context
- **Epic 2 Completion**: 
  - EF Core migrations generated for `PetFoodItems` and `LlmProviders`.
  - Implemented Domain-Entity mapping separation.
  - Added `ScanController` with localized caching logic based on `Accept-Language` headers and VLM fallback.
  - Implemented `VlmService` with real HTTP clients for OpenAI, Anthropic, and Gemini.
  - Added `AdminProviderController` secured by the `admin` role claim to manage encrypted LLM API keys via `EncryptionService`.
- **Photo Mode (Epic #87)**:
  - Added `ScanMode` enum to differentiate between Ingredients (OCR focus) and General package (Product recognition focus).
  - Added `[HttpPost("analyze-image")]` endpoint in `ScanController` to process direct photo uploads as base64 memory streams without persistent storage.
  - Adapted `IVlmService` and `VlmService` to construct multimodal payloads using `inlineData` / `image_url` depending on the LLM provider (OpenAI, Anthropic, Gemini).
- **Compare Multiple Pet Foods (Epic #122)**:
  - Added `[HttpPost("compare")]` in `ScanController` to handle comparing up to 5 images simultaneously.
  - Extended `IVlmService` with `AnalyzeMultipleImagesAsync` allowing multi-image prompt injection and returning `MultiVlmResponseContract`.
- **Bug Fixes (Provider Updates & Testing)**:
  - Added missing `[HttpPut("{id}")]` endpoint in `AdminProviderController` to allow updating existing providers via panel.
  - Updated `TestConnection` logic to explicitly query specific models (e.g., `/models/{ModelName}`) ensuring that entering a random model name results in a connection test failure.
  - Fixed a Postgres unique constraint violation when switching `IsPrimary` LLM providers by splitting `SaveChangesAsync` calls.

  - Fixed Gemini API URL generation bug by stripping 'models/' prefix appropriately for both Text and Image payloads.
- **Security Hotfix (Token Burn)**:
  - Ensured `NO_PET_FOOD_FOUND` errors intentionally skip the `RefundUsageAsync` process. This burns the scan quota for invalid/malicious image uploads, preventing wallet exhaustion.
- **User Profile Epic (Schema Update)**:
  - Added `UserEntity` and `CatEntity` to persist user profiles and registered cats.
  - Implemented `UserProfileMapper` (TDD) to map to `User` and `Cat` domain models.
  - Created EF Core migration `AddUsersAndCats`.
- **User Profile Epic (API Endpoints)**:
  - Added `ProfileController` with endpoints to GET profile (with cats and `RemainingScans`), POST a new cat, and DELETE a cat.
  - Extended `IScanQuotaService` with `GetRemainingScansAsync` to calculate quota without burning it.

- **Scan Quota Fix**:
  - Updated `IScanQuotaService` to return tuple `(Remaining, Limit)` instead of just `Remaining` to support dynamic user limits on frontend.
  - Added `MaxScans` property to `User` domain model and updated `UserProfileMapper` (TDD).
