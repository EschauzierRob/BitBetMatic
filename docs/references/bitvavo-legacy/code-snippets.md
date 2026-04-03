# Code Snippets Guide (Legacy Bitvavo)

This file explains what each extracted snippet demonstrates and how to use it safely for BitBetMatic 2.0 planning.

## Snippet index

- `snippets/auth-signing-example.cs`
  - Demonstrates v1 signing prehash + HMAC flow and header composition.
  - Source: `BitBetMatic/API/BitvavoApi.cs`.

- `snippets/candle-fetch-example.cs`
  - Demonstrates v1 candle request parameters, response row mapping, UTC conversion, and paging behavior.
  - Source: `BitBetMatic/API/BitvavoApi.cs`.

- `snippets/order-placement-example.cs`
  - Demonstrates v1 market order body creation and response parsing (`orderId`).
  - Source: `BitBetMatic/API/BitvavoApi.cs`.

## How to interpret these snippets

- They are **historical implementation references**, not drop-in code.
- They intentionally preserve legacy choices that may need redesign.
- They are compact to make key ideas easy to inspect quickly.

## What these snippets are useful for

- Reconstructing v1 request/response expectations.
- Understanding existing naming and field conventions.
- Preserving migration context for AI agents and humans.

## What not to copy directly

- Dynamic JSON parsing (`dynamic`) for production-critical flows.
- Generic `Exception` throwing without typed error contracts.
- Fixed precision assumptions for `amountQuote`.
- Interval calculations that assume 15-minute candles for all intervals.
- Any coupling that causes repeated `GET time` calls per signed request.
