# Implementation Plan: Cryptocurrency Price Fetch Strategy

**Prerequisites:**
- .NET SDK targeting `net10.0` (already installed)
- xUnit + FluentAssertions (already referenced by the affected test projects)
- No new packages; `IRepository` and `IAssetPriceService` are already registered in the same DI container in both the API and WPF composition roots, so no new dependency-injection wiring is required beyond the constructor parameter change

### Stage 1: Contract and Fetch Strategy (Application / Infrastructure)

**1. AssetPriceRequestDTO Extension** - Extend the price-fetch request contract with the asset's classification and owning broker name, both optional so existing callers are unaffected.

**2. GoogleFinance Cryptocurrency URL Support** - Extract the shared fetch-and-parse logic behind the existing stock-style quote lookup into a reusable helper, then add a cryptocurrency-specific entry point that builds the beta quote URL from a ticker and currency and reuses that same helper.

**3. AssetPriceService Class-Based Branching** - Branch the price-fetch flow by the request's asset classification: cryptocurrency requests resolve their broker's currency and call the new cryptocurrency fetch path; every other classification keeps calling the existing exchange-based path unchanged.

**4. Price Fetch Strategy Test Coverage** - Add unit coverage for the new URL-building logic, the classification-based validation branching, and the broker-currency resolution, following the spec's test functions and the project's existing pattern of hand-rolled repository stubs.

### Stage 2: API Contract

**5. AssetPricesController Query Param Extension** - Add optional query parameters for asset classification and broker name to the current-price endpoint, and branch its required-field validation to match the service's rules before forwarding the request.

**6. API Endpoint Test Coverage** - Extend the endpoint's existing stub-based test suite to cover the new query parameters being parsed and forwarded correctly, and the new validation branch for missing broker name on cryptocurrency requests.

### Stage 3: WPF Integration

**7. AssetPriceFetchViewModel Broker Context Threading** - Carry each asset's owning broker name alongside it through the bulk "Current Values" refresh loop so it can be supplied to the price-fetch call together with the asset's classification, which is already available there.

**8. TodayInfoTracker Signature Extension** - Extend the single-asset refresh helper to accept and forward the asset's classification and broker name into the price-fetch request it builds.

**9. AssetDetailsViewModel Wiring** - Pass the asset details page's already-known classification and broker name into the single-asset refresh call, and thread the broker name (without classification, per the spec's documented Portfolio Summary limitation) through the portfolio-row refresh path.
