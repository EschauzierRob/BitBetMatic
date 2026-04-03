# Bitvavo v1 API Surface (Implementation-Oriented)

This document describes the exchange operations that the v1 code appears to call.

## 1) Base clients and routing assumptions

- Main REST client uses `API_BASE_URL` env var (`RestClient(Environment.GetEnvironmentVariable("API_BASE_URL"))`).
- Most exchange calls are relative paths against that base URL.
- One call (`GetMarkets`) uses a hardcoded secondary host: `https://edge.bitvavo.com` with path `exchange/proxy/v3/markets/data`.

## 2) Authentication and signing

### Header set in signed requests
For signed endpoints, v1 sets:
- `Bitvavo-Access-Key`
- `Bitvavo-Access-Signature`
- `Bitvavo-Access-Timestamp`
- `Bitvavo-Access-Window: 60000`
- `Content-Type: application/json`

### Timestamp behavior
- Timestamp is fetched from server `GET time` and used as string value.
- Signing and timestamp header are coupled through this retrieved timestamp.

### Signature construction
- Prehash format: `timestamp + method + "/v2/" + url + body`
- HMAC-SHA256 with `BITVAVO_API_SECRET`
- Output as lowercase hex.

## 3) Operations in `IApiWrapper` / `BitvavoApi`

### 3.1 `GetPrice(market)`
- Endpoint pattern: `GET ticker/price?market={market}`
- Parsing: `dynamic`, expects `price` field.
- Returns `decimal`.

### 3.2 `GetCandleData(market, interval, limit, start, end)`
- Endpoint pattern: `GET {market}/candles`
- Query params:
  - `interval` (e.g. `15m`, `1h`)
  - `limit`
  - `start` (Unix ms)
  - `end` (Unix ms)
- Response parsing expects array rows with indices:
  - `[0]` timestamp ms
  - `[1]` open
  - `[2]` high
  - `[3]` low
  - `[4]` close
  - `[5]` volume
- Maps to `FlaggedQuote` (`Quote` extension + `Market`, `TradeAction`, `Id`).

### 3.3 `GetBalances()`
- Endpoint pattern: signed `GET balance`
- Parsing target: `List<Balance>` where model uses lower-case property names (`symbol`, `available`, `inOrder`).

### 3.4 `Buy(market, amount)` / `Sell(market, amount)`
- Implemented via private `PlaceOrder`.
- Endpoint pattern: signed `POST order`
- Request JSON body shape:
  - `market`
  - `side`
  - `orderType: "market"`
  - `amountQuote` (formatted as string, usually 2 decimals)
- Success parsing: `dynamic`, reads `orderId`.

### 3.5 `GetTradeData(market)`
- Endpoint pattern: signed `GET trades/?market={market}`
- Parsing target: `List<TradeData>` with fields like `Id`, `OrderId`, `Timestamp`, `Amount`, `Price`, etc.
- Utility conversion: `TimestampAsDateTime` from Unix ms.

### 3.6 `GetMarkets()`
- Endpoint pattern: `GET https://edge.bitvavo.com/exchange/proxy/v3/markets/data?miniChart=false`
- Parsing target: `List<MarketData>` with nested `Base` and `Quote`.

### 3.7 `GetPortfolioData()`
- Not implemented (`NotImplementedException`).

## 4) Key data models and DTO shape notes

- `Balance`: `symbol` (string), `available` (decimal), `inOrder` (string).
- `TradeData`: mostly typed scalar fields; includes computed UTC date conversion.
- `MarketData`: mixed numeric types (`double`, `int`, `object` for `HighlightedAt`).
- `FlaggedQuote`: inherits indicator-library `Quote` and adds app-specific metadata.

## 5) Behavioral assumptions encoded in v1

- Candle granularity helper `Get15MinuteIntervals` always divides by 15 minutes, regardless of requested interval string.
- Candle fetching logic paginates by moving `end` backward to earliest fetched candle minus 1 second.
- Order amount precision is mostly fixed to 2 decimals unless amount is whole number.
- Error handling relies on thrown generic `Exception` with response body text.

## 6) Security note

The implementation expects API key/secret values via environment variables. Sensitive values are not copied into this reference package.
