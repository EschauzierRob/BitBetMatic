# Persistence notes (Epic 0002 / Features 1-2)

## Scope
This folder contains persistence concerns for:
- candle storage/retrieval (Feature 1)
- strategy threshold persistence (Feature 2)

No Feature 3 responsibilities should be added here.

## Conventions
- `TradingDbContext` is the single EF Core boundary for app data access.
- Connection string is read from `DB_CONNECTION_STRING` and must be configured in the runtime environment.
- Schema migrations run at host startup through `MigrationHostedService`.
- Repositories should consume `IDbContextFactory<TradingDbContext>` and must not `new` a `TradingDbContext` directly.

## Migration setup
Design-time migration commands rely on `TradingDbContextFactory`, which uses the same `DB_CONNECTION_STRING` setting as runtime.

Example:
```bash
dotnet ef migrations add <Name> --project BitBetMatic/BitBetMaticFunctions.csproj
```

## Deferred by design
- Upsert support at database level (MERGE/SPROC) is intentionally deferred.
- Historical threshold archiving/cleanup policy is intentionally deferred.
