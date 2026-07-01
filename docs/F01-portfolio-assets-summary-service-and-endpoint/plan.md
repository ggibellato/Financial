# Implementation Plan: F01 — Portfolio Assets Summary Service and Endpoint

**Prerequisites:**
- No new packages or tools required
- All existing dependencies (`FluentAssertions`, `xUnit`, `WebApplicationFactory`) already present in the test projects
- `NavigationMapper.CalculateTotals` and `NavigationMapper.OrderByNameWithEncerradasLast` are accessible from within `Financial.Application` (same assembly, `internal` visibility)

---

### Phase 1: Application Layer

**1. PortfolioAssetSummaryItemDTO** — Create the response DTO in `Financial.Application/DTOs/`. Follow the `AggregatedSummaryDTO` sealed-class-with-init pattern. See spec Section 4 for all property names and types.

**2. IPortfolioAssetSummaryQueryService Interface** — Create the service interface in `Financial.Application/Interfaces/` declaring the single `GetPortfolioAssetsSummary` method. See spec Section 4 for the method signature.

**3. PortfolioAssetSummaryQueryService Implementation** — Create the service in `Financial.Application/Services/`. Guard null/whitespace inputs; retrieve assets from the repository; compute all per-asset values including `TotalInvested`, `FirstInvestmentDate`, and `PortfolioWeight`; sort; return. See spec Sections 4 and 5 for the full computation rules.

**4. DI Registration** — Add the singleton registration for `IPortfolioAssetSummaryQueryService` to `ApplicationServiceCollectionExtensions.AddFinancialApplication()`. See spec Section 4.

---

### Phase 2: API Layer

**5. SummaryController Update** — Inject `IPortfolioAssetSummaryQueryService` as a second constructor parameter into `SummaryController` and add the new `GetPortfolioAssetsSummary` action on route `portfolio/{brokerName}/{portfolioName}/assets`. Validate both path parameters and return HTTP 400 on whitespace; otherwise return HTTP 200 with the service result. See spec Sections 4 and 5 for the route, attributes, and error response.

---

### Phase 3: Tests

**6. PortfolioAssetSummaryQueryServiceTests** — Create the unit test class in `Tests/Financial.Application.Tests/Services/` using the inner `StubRepository` pattern from `SummaryQueryServiceTests`. Cover all computation cases, sort order, null/whitespace inputs, and edge cases (no assets, all-zero TotalInvested, no Buy transactions). See spec Section 7 for the full test function list.

**7. SummaryEndpointsTests Additions** — Add the two integration tests to the existing `SummaryEndpointsTests.cs` using the same `ApiTestFactory` pattern: one verifying HTTP 200 with items against the real test data file, one verifying HTTP 400 for a whitespace broker name. See spec Section 7.
