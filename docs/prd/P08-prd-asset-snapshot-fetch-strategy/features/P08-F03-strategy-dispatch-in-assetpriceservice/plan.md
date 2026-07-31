# Implementation Plan: Strategy Dispatch in AssetPriceService

**Prerequisites:**
- No new tools, libraries, or configuration
- Requires F01 (`IAssetPriceFetcher`, `StandardAssetPriceFetcher`) and F02 (`CryptocurrencyAssetPriceFetcher`) already merged to `main`

### Stage 1: Dispatcher Rewrite

**1. AssetPriceService Constructor and Dispatch Logic** - Change the constructor to depend on the injected collection of fetchers instead of the repository, and rewrite the price-fetch method to select the matching strategy from that collection with an order-based fallback when nothing matches. Reference the spec for the exact selection expression and fallback semantics.

**2. Dead Code Removal** - Delete the now-redundant private/internal methods whose logic was already relocated into the two fetchers in F01 and F02. Reference the spec's Technical Decisions for exactly which methods and why keeping them would duplicate coverage.

### Stage 2: Test Rewrite and Verification

**3. Dispatch-Selection Test Coverage** - Replace the superseded validation tests with new tests proving the dispatcher selects the correct strategy, using lightweight fake fetchers for fast, isolated coverage of the selection and fallback logic. Reference the spec's Testing Strategy for the full list of test functions and which prior tests are removed and why.

**4. Real-Fetcher Reachability Coverage** - Add tests that wire the real Standard and Cryptocurrency fetchers into the dispatcher and confirm each is genuinely reachable through the public `GetCurrentPrice` entry point, satisfying the PRD's cross-feature integration criteria without requiring live network access. Reference the spec for the exact scenarios and expected exceptions.

**5. Regression Check** - Run the full solution test suite, confirming the existing `AssetPriceEndpointsTests` API integration tests and every other previously-passing test remain green, since this feature changes only `AssetPriceService`'s internals behind its unchanged public contract.
