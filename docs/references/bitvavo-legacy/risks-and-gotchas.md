# Risks and Gotchas (Legacy Bitvavo v1)

This list separates what is directly observed from what is inferred risk.

## Observed in code

1. **Interval calculation hardcoded to 15-minute buckets**
   - `Get15MinuteIntervals` is used to cap fetch `limit` regardless of requested interval string.
   - Risk: incorrect limits for `1h`, `5m`, etc.

2. **Candle paging relies on response ordering assumption**
   - Loop updates `end = newQuotes.Min(q => q.Date).AddSeconds(-1)`.
   - Risk: if API ordering changes, paging may skip or overlap unexpectedly.

3. **Dynamic JSON parsing in critical paths**
   - Price, candles, and order response fields are read via `dynamic`.
   - Risk: runtime failures on shape/type drift without compile-time protection.

4. **Generic exceptions without typed categories**
   - Most failures throw `Exception` with raw response text.
   - Risk: caller cannot reliably branch on failure class.

5. **Order amount formatting is simplistic**
   - `amountQuote` uses 2 decimal places unless whole number.
   - Risk: market-specific precision/min step rules may not be respected.

6. **Additional host dependency for market metadata**
   - `GetMarkets` uses `https://edge.bitvavo.com` directly.
   - Risk: behavior differs from `API_BASE_URL`-based routing and signing model.

7. **Partially implemented API abstraction**
   - `GetPortfolioData` throws `NotImplementedException`.
   - Risk: interface suggests capability that runtime cannot provide.

## Likely operational risks (inferred)

1. **Rate-limit sensitivity**
   - No explicit throttle/retry/backoff in BitvavoApi.
   - Repeated timestamp/time calls for signed requests may increase request count.

2. **Clock/timestamp coupling fragility**
   - Signing depends on obtaining remote time first.
   - If `time` endpoint is delayed/failing, authenticated calls may fail indirectly.

3. **Nullability and parse robustness**
   - Limited defensive checks for missing fields in dynamic payloads.
   - Could surface as runtime binder/serialization errors.

4. **Sync-over-async legacy usage in ancillary flows**
   - Some `.Result` usage appears in performance/backtesting code.
   - Potential deadlock/latency patterns in certain hosts.

## Unknown / unverified areas

- Exact production rate-limit behavior under current Bitvavo limits is not validated here.
- Formal exchange error-code mapping is not implemented or documented in v1.
- It is unclear whether all markets used by strategies have uniform precision and minimum order constraints.
- No direct proof in this repo that all authenticated endpoints were exercised in production recently.

## Sensitive material handling note
Credentials are consumed from environment variables in code. No secrets are copied into this reference package.
