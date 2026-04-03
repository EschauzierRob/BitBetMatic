# Migration Notes for BitBetMatic 2.0 (from v1 Bitvavo integration)

## Principles
- Treat v1 as behavior reference, not architecture template.
- Preserve business intent, redesign integration boundaries.
- Prefer explicit contracts, typed parsing, and deterministic error behavior.

---

## 1) Candle retrieval

### What existed in v1
- Candles fetched from `{market}/candles` with `interval`, `limit`, `start`, `end`.
- Timestamps converted from Unix ms to UTC `DateTime`.
- DataLoader merges database candles with fetched candles and stores only missing entries.

### What is reusable
- General pattern of cache-first historical loading.
- Explicit UTC normalization before persistence.
- De-duplication strategy using market + timestamp identity.

### What must change in 2.0
- Replace interval math helper that assumes 15-minute granularity.
- Define a clear pagination strategy independent of response ordering assumptions.
- Introduce typed response models instead of dynamic row indexing where feasible.
- Add resilient retry/backoff and rate-limit-aware request policy.

---

## 2) Auth/signing

### What existed in v1
- HMAC-SHA256 signature over `{timestamp}{method}/v2/{url}{body}`.
- Timestamp fetched via `GET time`, then reused in signed headers.
- Credentials loaded from environment variables.

### What is reusable
- Core prehash/signature pattern and header names.
- Secret isolation via environment/config injection.

### What must change in 2.0
- Move signing into isolated, testable auth component.
- Add deterministic canonicalization tests for signature payload.
- Avoid hidden network coupling when creating signatures (timestamp strategy should be explicit and controllable).
- Introduce secret-safe diagnostics (never log or serialize secrets).

---

## 3) Order placement

### What existed in v1
- Single order path: market order with `amountQuote`.
- Buy/sell wrappers call shared `PlaceOrder`.
- Success path returns formatted string including `orderId`.

### What is reusable
- Shared internal order execution flow for buy/sell.
- High-level request shape (`market`, `side`, `orderType`, `amountQuote`).

### What must change in 2.0
- Return typed order result object (status, ids, exchange payload metadata).
- Implement instrument-specific precision/step-size handling.
- Add idempotency/client-order-id strategy if supported.
- Add explicit handling for partial fills, rejected orders, and transient failures.

---

## 4) Response parsing

### What existed in v1
- Mixed typed DTOs and `dynamic` parsing.
- Candle rows parsed by numeric index positions.

### What is reusable
- Existing DTO names and rough shape are useful migration hints.

### What must change in 2.0
- Replace dynamic parsing in critical paths with typed contracts and validation.
- Add tolerant parsing with schema drift handling where needed.
- Centralize serialization settings and number/date parsing rules.

---

## 5) Error handling and reliability

### What existed in v1
- Failures generally throw generic `Exception` with response content.
- Minimal warning logs; no structured retry policy.
- Some code paths mix async with `.Result` in legacy utilities.

### What is reusable
- Basic intent to surface upstream response bodies for diagnostics.

### What must change in 2.0
- Introduce typed exception hierarchy (auth, rate-limit, transient, validation, exchange rejection).
- Add retry/backoff/circuit-breaker strategy by endpoint type.
- Use fully async flows end-to-end.
- Add request correlation IDs and structured logging.

---

## 6) Exchange abstraction

### What existed in v1
- `IApiWrapper` combines market data, account data, and trading operations in one interface.
- Some methods are incomplete (`GetPortfolioData`).

### What is reusable
- Single adapter entrypoint concept for strategy engine integration.

### What must change in 2.0
- Split into focused ports/interfaces (market data, account, trading, reference data).
- Version contracts and make capability discovery explicit.
- Ensure implementations can be tested with deterministic mocks and fixture payloads.

---

## 7) Inspiration-only areas (do not copy directly)
- Dynamic JSON access patterns.
- Implicit assumptions about candle order and interval math.
- String-only success returns for order placement.
- Legacy sync-over-async usage patterns.
