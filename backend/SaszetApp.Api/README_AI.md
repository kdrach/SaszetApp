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
