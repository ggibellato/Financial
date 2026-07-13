# Implementation Plan: BR Bond Price Fetcher

**Prerequisites:**
- No new tools or libraries — uses the existing xUnit + FluentAssertions (.NET) and Vitest + React Testing Library (TypeScript) stacks

### Stage 1: Application and Infrastructure

**1. AssetPriceRequestDTO Name Field** - Add the new optional field that carries a bond's title through the price-request pipeline, leaving every other field untouched.

**2. BondAssetPriceFetcher** - Add the new fetch strategy for Bond-class assets, validating its input and delegating to the Status Invest finance service from the previous feature. Reference the spec for the exact validation and delegation behavior.

**3. Standard Fetcher Exclusion** - Update the default fetcher so it no longer claims Bond-class assets, since a dedicated fetcher now exists for them.

**4. Dependency Injection Registration** - Register the new fetcher in the Infrastructure composition root alongside the existing two.

### Stage 2: Infrastructure Test Coverage

**5. BondAssetPriceFetcher Tests** - Add coverage for its asset-class matching and validation behavior, following the existing sibling fetchers' test structure.

**6. Standard Fetcher Test Update** - Flip the existing test that documented Bond dispatching to the default fetcher, since that guarantee no longer holds.

### Stage 3: API Endpoint Wiring

**7. AssetPricesController Update** - Add the new query parameter and adjust validation so Bond requests are recognized by their bond name instead of an exchange, mirroring the existing Cryptocurrency-specific validation branch.

**8. API Endpoint Test Coverage** - Add test cases covering a Bond request with a name (success) and without one (rejected), following the existing endpoint test file's structure.

### Stage 4: WPF Wiring

**9. AssetPriceFetchViewModel Update** - Pass the asset's name through when building each price request during a bulk refresh.

**10. TodayInfoTracker Update** - Add the new parameter this tracker needs to build a Bond-aware request, and adjust its validation so Bond assets are recognized by name instead of exchange.

**11. AssetDetailsViewModel Call Site Update** - Pass the already-available asset name through to the tracker at its single call site.

**12. WPF Test Coverage** - Add a test case to the existing bulk-refresh ViewModel test file covering a Bond asset, and add a new test file for the tracker's validation and request-building behavior, since it currently has none.

### Stage 5: Web Frontend Wiring

**13. API Client and Hook Update** - Add the new optional parameter to the API client method and thread it through the summary hook's two price-fetch call sites, adjusting each site's guard so a Bond asset with a name (but no exchange) still triggers a fetch.

**14. Web Test Coverage** - Add test cases to the existing API client and hook test files covering the new parameter and the Bond-without-exchange scenario.

### Stage 6: Regression Check

**15. Full Suite Verification** - Run every existing test across all affected projects (.NET and TypeScript) to confirm no existing behavior changed, since every modified file preserves its non-Bond behavior unchanged.
