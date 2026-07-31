# Implementation Plan: Cryptocurrency Asset Price Fetcher

**Prerequisites:**
- No new tools, libraries, or configuration — uses the existing xUnit + FluentAssertions test stack and the existing `GoogleFinance` integration as-is
- Requires F01 (`IAssetPriceFetcher`, `StandardAssetPriceFetcher`) already merged to `main`

### Stage 1: Fetcher Implementation

**1. CryptocurrencyAssetPriceFetcher Implementation** - Create the new Infrastructure-layer class implementing the strategy interface, relocating today's cryptocurrency-specific validation, broker-currency resolution, and Google Finance beta-quote delegation from `AssetPriceService` unchanged. Reference the spec for the exact `Supports` semantics, error messages, and the `ResolveBrokerCurrency` seam's shape.

### Stage 2: Wiring and Verification

**2. Dependency Injection Registration** - Register the new fetcher in the Infrastructure composition root alongside the existing `StandardAssetPriceFetcher` registration, without touching `AssetPriceService`'s own registration. Reference the spec's Component Overview for the exact registration point.

**3. Unit Test Coverage** - Add a new test file covering the fetcher's asset-class matching, broker-name validation, and currency-resolution branching, following the sibling `StandardAssetPriceFetcherTests` file's structure and assertion style. Reference the spec's Testing Strategy for the full list of test functions and what stays intentionally untested.

**4. Regression Check** - Confirm `AssetPriceService`, its existing branching logic (including its own copy of `GetCryptocurrencySnapshot`/`ResolveBrokerCurrency`), and its existing test file remain completely untouched and still pass, since this feature only adds a new, not-yet-wired type. Run the full test suite to verify no existing behavior changed.
