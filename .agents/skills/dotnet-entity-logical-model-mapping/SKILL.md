---
name: "dotnet-entity-logical-model-mapping"
description: "Use one-class-per-file and explicit EF entity to logical model mapping in .NET services"
domain: "dotnet"
confidence: "high"
source: "manual"
---

## Context
Apply this for .NET services using Entity Framework where persistence entities must stay separate from logical/domain models in DDD-style architecture.

## Patterns
1. Keep exactly one class/record/interface per file.
2. Place EF persistence entities in `Data/` and keep them ORM-focused.
3. Place logical/domain records in a separate namespace (for example `History/`).
4. Use explicit mapper interfaces/implementations (`I*ModelMapper`) to convert EF entities into logical/domain records.
5. Keep repositories responsible for querying entities and invoking mapper boundaries before returning data.

## Examples
- `services/chat/Data/TenantConfigurationEntity.cs`
- `services/chat/Data/ConversationThreadEntity.cs`
- `services/chat/Data/ChatMessageEntity.cs`
- `services/chat/History/TenantConfigurationRecord.cs`
- `services/chat/History/ConversationThreadRecord.cs`
- `services/chat/History/ChatMessageRecord.cs`
- `services/chat/History/IChatHistoryModelMapper.cs`
- `services/chat/History/ChatHistoryModelMapper.cs`

## Anti-Patterns
- Multiple classes/records/interfaces in one source file.
- Returning EF entity types directly from application-facing repository contracts.
- Mixing persistence concerns and logical model contracts in the same file.
