# Implementation Plan: Finance Service Common Interface

**Prerequisites:**
- No new tools, libraries, or configuration — uses the existing xUnit + FluentAssertions test stack and the existing `GoogleFinance` integration as-is

### Stage 1: Finance Service Contract and Google Finance Adapter

**1. IFinanceService Interface and Request Type** - Define the new strategy contract and its request shape in the Infrastructure layer, so any current or future website-based price source shares one signature. Reference the spec for the exact member and property list.

**2. GoogleFinanceService Implementation** - Wrap today's static Google Finance scraper behind the new contract, routing to the existing stock-quote or cryptocurrency-quote path based on which identifying field the request carries, with no change to the underlying scraping logic. Reference the spec for the exact routing rule and its validation behavior.

### Stage 2: Relocate the Fetcher Strategy Interface

**3. Move IAssetPriceFetcher to Infrastructure** - Relocate the existing asset-class fetch-strategy interface out of the Application layer into the same Infrastructure location as the new contract, updating its namespace only. Reference the spec's Technical Decisions for why this interface belongs alongside the new one.

### Stage 3: Wire Existing Fetchers to the New Contract

**4. Update StandardAssetPriceFetcher** - Change the default fetch strategy to depend on the new finance-service contract instead of calling the static Google Finance class directly, preserving its existing validation and error message unchanged. Reference the spec for the exact request construction.

**5. Update CryptocurrencyAssetPriceFetcher** - Change the cryptocurrency fetch strategy to depend on the new finance-service contract alongside its existing repository dependency, preserving its existing validation, broker-currency resolution, and error messages unchanged. Reference the spec for the exact request construction.

**6. Dependency Injection Registration** - Register the new Google Finance service implementation in the Infrastructure composition root, and update the moved interface's registration references to its new location. Reference the spec's Component Overview for the exact registration point.

### Stage 4: Test Coverage and Regression Check

**7. New GoogleFinanceService Test Coverage** - Add a new test file covering the finance service's request-validation branch. Reference the spec's Testing Strategy for the exact test function and what stays intentionally untested.

**8. Update Existing Fetcher and Dispatcher Tests** - Update the constructor calls in the existing fetcher test files and the dispatcher's test file to supply the new dependency, following the existing sibling test files' nested fake/stub pattern, without changing any existing assertion. Reference the spec's Testing Strategy for the affected test files.

**9. Regression Check** - Confirm `AssetPriceService`'s dispatch logic, `AssetPriceRequestDTO`, and every Presentation call site remain completely unchanged, since this feature only relocates and rewires internal Infrastructure dependencies. Run the full test suite to verify no existing behavior changed.
