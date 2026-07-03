---
name: "ef-migration-domain-mapping"
description: "Generate EF Core migrations from persistence entities and map them to logical/domain models"
domain: "persistence"
confidence: "high"
source: "manual"
---

## Context
Use this when a service needs relational persistence in EF Core while keeping DDD boundaries between persistence entities and logical/domain models.

## Patterns
1. Define persistence entities in `Data/` (one class per file) and keep them ORM-focused.
2. Configure mapping in `DbContext` (`OnModelCreating`) with explicit table names, keys, indexes, and FK constraints.
3. Keep logical/domain models separate (`History/Models.cs` or equivalent).
4. Add a mapper (`I*ModelMapper` + implementation) that converts persistence entities to logical/domain models.
5. Repositories query EF entities, then map via mapper before returning logical models.
6. Store schema evolution in EF migrations (`Data/Migrations/*`) and run `Database.MigrateAsync()` on startup (skip in testing env).

## Examples
- `services/chat/Data/ChatMessageEntity.cs` — persistence entity model
- `services/chat/Data/ChatDbContext.cs` — EF mapping configuration
- `services/chat/History/IChatHistoryModelMapper.cs` and `ChatHistoryModelMapper.cs` — entity → logical model mapping
- `services/chat/History/EfChatHistoryRepository.cs` — repository using entities + mapper
- `services/chat/Data/Migrations/20260526134200_InitialChatSchema.cs` — schema migration

## Anti-Patterns
- Returning EF entities directly from application/domain-facing repository contracts.
- Mixing SQL bootstrap scripts with EF migrations for the same schema lifecycle.
- Building API response models directly in SQL/ORM query expressions when a mapper boundary is required.
