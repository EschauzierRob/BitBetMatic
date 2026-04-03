# Bitvavo v1 Capabilities

Status legend used below:
- **Observed in code**: directly visible in current implementation.
- **Inferred from code paths**: likely behavior, but not fully proven by tests in this repository.
- **Unknown / unverified**: not enough evidence in code/tests.

## Confirmed capabilities (observed in code)

### Public market data
- Fetch current ticker price for a market via `ticker/price?market=...`.
- Fetch candles via `{market}/candles` with `interval`, `limit`, `start`, `end` (milliseconds).
- Convert candle timestamps from Unix milliseconds to UTC `DateTime`.

### Authenticated account and trading calls
- Fetch balances via authenticated `GET balance`.
- Place market buy/sell orders via authenticated `POST order` using:
  - `market`
  - `side` (`buy`/`sell`)
  - `orderType = market`
  - `amountQuote` (formatted string)
- Fetch authenticated trade history via `GET trades/?market=...`.

### Signature/auth mechanics
- Create Bitvavo headers using env vars:
  - `BITVAVO_API_KEY`
  - `BITVAVO_API_SECRET`
  - `API_BASE_URL`
- Signature prehash format in v1: `{timestamp}{method}/v2/{url}{body}`.
- Signature algorithm: HMAC-SHA256 hex lowercase.
- Timestamp is retrieved by calling Bitvavo `GET time` per signed request setup.

### Candle persistence integration
- Candle data is cached in database through `ICandleRepository`.
- Duplicate prevention is done by `(market, date)` checks at repository level.
- DataLoader can fetch missing historical ranges from Bitvavo and persist only missing candles.

## Likely capabilities (inferred from code paths)
- Scheduled and HTTP-triggered functions use `BitvavoApi` through DI/manual instantiation.
- Trading strategy loop can execute live buy/sell when `transact=true`.
- Market metadata retrieval (`GetMarkets`) is intended for market selection, although currently not central in active strategy flow.

## Unclear / partially implemented / legacy-only
- `GetPortfolioData()` in `IApiWrapper` is not implemented in `BitvavoApi`.
- Retry/backoff is not explicitly implemented for Bitvavo HTTP failures.
- Formal rate-limit handling is absent.
- Typed DTO coverage is partial; several endpoints use `dynamic` parsing.
- `GetMarketInfo` exists but is private and unused from outside `BitvavoApi`.
- Some analysis/performance code uses `.Result` on async calls (legacy style), which may hide runtime issues under load.
