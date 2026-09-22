# Implementation Plan: F03. Batched Frankfurter Historical Fetch

**Prerequisites:**
- .NET 10 SDK, existing `Financial.Shared.Abstractions` and `Integrations/Frankfurter` projects
- No new NuGet packages

### Stage 1: Fetch Contract and Result Type

**1. Batched Fetch Abstraction** - Add the `IUsdRateFetcher` interface and its `UsdRateFetchResult` value type to `Financial.Shared.Abstractions`, giving the future F02 resolution logic a stable, partial-tolerant contract to depend on.

### Stage 2: Batched Frankfurter Implementation

**2. Batched Fetch Method** - Implement `IUsdRateFetcher` on the existing `FrankfurterExchangeRateProvider`, issuing one combined USD→BRL/GBP HTTP call per attempted date, reusing the existing 10-day walk-back window and exception-handling conventions. Leave the existing single-pair `GetHistoricalRateAsync` method untouched.

### Stage 3: Tests

**3. Test Coverage** - Extend the existing Frankfurter provider test file with cases for the batched fetch: full success, partial success per currency, walk-back behavior, exhausted fallback window, and failure/exception handling — while confirming the pre-existing single-pair tests still pass unmodified.
