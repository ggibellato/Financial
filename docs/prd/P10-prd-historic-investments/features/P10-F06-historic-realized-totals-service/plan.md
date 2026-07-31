# Implementation Plan: Historic Realized Totals Service

**Prerequisites:**
- F05 (Scoped Navigation & Summary API) merged — provides the `scope` query parameter, `InvestmentScope` enum, and scope-aware `IRepository`/`SummaryController` this feature builds on
- F07 (Historic Broker Breakdown Charts Service) merged — establishes the Active/Historic facade pattern this feature mirrors
- No new tools, libraries, or environment variables required

### Stage 1: Shared Building Blocks

**1. Portfolio Asset Summary Builder** - Extract the current service's per-asset computation (totals, cash flows, credits analysis) and portfolio-level aggregation (weight and credit-percentage calculation against a portfolio-wide basis) into a shared, scope-agnostic builder that accepts a weight-basis selector and a realized-gain-loss selector per asset, so both scopes reuse identical computation logic instead of duplicating it. Add the new realized-gain-loss field to the shared DTO the builder assembles.

**2. Scoped Service Contracts** - Define two narrow contracts, one for the Active-scope summary and one for the Historic-scope summary, each exposing a single broker/portfolio-name lookup method with scope implicit in which contract is called, per the spec's Component Overview.

### Stage 2: Scope-Specific Summary Services

**3. Active Summary Service** - Rename the existing summary service to make explicit that it serves the Active scope, preserving its current net-invested weighting behavior unchanged and always returning a null realized-gain-loss, built through the shared builder from Stage 1.

**4. Historic Summary Service** - Introduce a new service serving the Historic scope, weighting portfolio share and credit percentages by gross total bought rather than net invested, and computing the realized-gain-loss figure, built through the same shared builder.

**5. Summary Facade** - Update the existing public summary service so it acts as a facade routing a request to the Active or Historic implementation based on the scope value it already receives, keeping `SummaryController`'s existing call site unchanged.

**6. Dependency Injection** - Register the two scope-specific services and the facade in the Application layer's service collection, following the existing singleton registration pattern used elsewhere in this project.

### Stage 3: Frontend Contract Parity

**7. TypeScript Type Update** - Add the new realized-gain-loss field to the corresponding TypeScript API type, keeping the frontend contract in sync with the backend DTO without introducing any UI changes.

### Stage 4: Test Coverage

**8. Shared Builder Tests** - Cover the extracted builder's weight/percentage calculation against a caller-supplied basis selector and its application of the realized-gain-loss selector, independently of any scope.

**9. Active Service Tests** - Adapt the existing summary service test suite to the renamed service and its simplified method signature, retaining coverage of net-invested weighting and confirming realized-gain-loss stays null.

**10. Historic Service Tests** - Add coverage proving the Historic summary weights by gross total bought and computes realized gain/loss correctly, including the credit-percentage fields no longer collapsing toward a nonsensical value for a closed position.

**11. Facade Dispatch Tests** - Verify the facade routes each scope value, including the default, to the correct underlying scoped service.

**12. Integration Test Updates** - Extend the existing historic-scope summary endpoint test to assert on realized-gain-loss and portfolio-weight values summing to 100%, and add a regression test proving Active-scope endpoint behavior is unchanged.
