# Bitvavo Legacy Reference (BitBetMatic v1)

## Purpose
This folder captures the current Bitvavo integration behavior in BitBetMatic v1.

It exists to support migration planning and implementation design for BitBetMatic 2.0.

## Scope
This package is **reference-only**:
- It describes what v1 does today.
- It extracts compact snippets from the legacy code.
- It highlights known gaps, risks, and architectural issues.

It does **not** introduce new runtime code.

## How to use this in BitBetMatic 2.0
1. Start with `capabilities.md` to see what appears proven vs uncertain.
2. Use `api-surface.md` to understand operations, payload shapes, and auth behavior.
3. Use `migration-notes.md` to decide what to keep as concepts and what to redesign.
4. Read `risks-and-gotchas.md` before implementing production flows.
5. Consult `snippets/` only as quick reference patterns.

## Important warning
Do **not** copy v1 code blindly into 2.0.

Several parts are tightly coupled, use dynamic JSON parsing, mix sync/async patterns, and rely on implicit assumptions (timestamp source, response ordering, numeric precision, and order-size behavior). These should be redesigned for a robust 2.0 exchange layer.

## Primary source files reviewed
- `BitBetMatic/API/BitvavoApi.cs`
- `BitBetMatic/API/IApiWrapper.cs`
- `BitBetMatic/BitBetMaticProcessor.cs`
- `BitBetMatic/Backtesting/Dataloader.cs`
- `BitBetMatic/Repositories/CandleRepository.cs`
- `BitBetMatic/Models/Balance.cs`
- `BitBetMatic/Models/MarketData.cs`
- `BitBetMatic/Backtesting/BitvavoPerformanceFunction.cs`
- `BitBetMatic/Program.cs`
