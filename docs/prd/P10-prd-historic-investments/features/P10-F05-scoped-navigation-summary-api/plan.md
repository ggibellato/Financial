# Implementation Plan: F05. Scoped Navigation & Summary API

**Prerequisites:**
- No new tools, libraries, or environment variables required
- Builds on `IRepository`'s existing `InvestmentScope`-parameterized methods (F02) and `GoogleGenerator`'s Active/Historic routing (F04); consumes `PositionType` as already computed by F01

### Stage 1: Scope Parsing and Application-Layer Threading

**1. Investment Scope Parser** - Add a new validation helper that parses a raw scope string into `InvestmentScope`, following the same generic-parser pattern already used for transaction and credit types, including a convenience method that resolves straight to a default when parsing fails.

**2. Navigation Service and Mapper Scope Threading** - Extend `INavigationService`/`NavigationService` and `NavigationMapper`'s internal mapping chain to accept and forward an investment scope, overriding position type to the closed/flat value whenever the scope is Historic.

**3. Summary and Breakdown Services Scope Threading** - Extend `ISummaryService`/`SummaryService`, `IPortfolioAssetSummaryService`/`PortfolioAssetSummaryService`, and `IBrokerBreakdownService`/`BrokerBreakdownService` to accept and forward an investment scope to their repository calls, removing the now-redundant closed-portfolio/inactive-asset filters from the two services that still have them.

### Stage 2: API Surface

**4. Controller Query Parameter Wiring** - Add a scope query parameter to `NavigationController`'s tree/brokers endpoints, `AssetsController`'s asset-detail endpoint, and all four `SummaryController` endpoints, parsing it through the new helper and defaulting to the active scope when absent or unrecognized.

### Stage 3: Test Coverage

**5. Application-Layer Test Updates** - Update the existing navigation/summary/breakdown unit test suites to remove or rewrite assertions tied to the deleted filters, and add coverage proving the scope value reaches the repository and correctly overrides position type for historic assets; add unit tests for the new scope parser.

**6. API-Level Test Fixture and Coverage** - Extend both `data.test.json` copies with a historic broker/portfolio/asset entry, then extend the navigation, summary, and asset-detail integration test suites to cover scoped requests (active, historic, and omitted) end-to-end through the real HTTP pipeline.

### Stage 4: Presentation-Layer Compatibility

**7. WPF Test Stub Signature Updates** - Update the navigation, summary, portfolio-asset-summary, and broker-breakdown stub implementations used by the existing WPF view-model tests so they compile against the extended service interfaces, without changing any production WPF call site.
