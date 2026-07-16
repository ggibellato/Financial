# Implementation Plan: Historic Broker Breakdown Charts Service

**Prerequisites:**
- F05 (Scoped Navigation & Summary API) merged — provides the `scope` query parameter, `InvestmentScope` enum, and scope-aware `IRepository`/`SummaryController` this feature builds on
- No new tools, libraries, or environment variables required

### Stage 1: Shared Breakdown Building Blocks

**1. Breakdown Builder** - Extract the current breakdown service's portfolio/asset filtering, alphabetical sorting, and DTO-assembly logic into a shared, scope-agnostic builder that accepts an invested-amount selector per asset, so both scopes reuse identical construction logic instead of duplicating it.

**2. Scoped Service Contracts** - Define two narrow contracts, one for the Active-scope breakdown and one for the Historic-scope breakdown, each exposing a single broker-name lookup method with scope implicit in which contract is called, per the spec's Component Overview.

### Stage 2: Scope-Specific Breakdown Services

**3. Active Breakdown Service** - Rename the existing breakdown service to make explicit that it serves the Active scope, preserving its current net-invested sizing behavior unchanged, and have it build its result through the shared builder from Stage 1.

**4. Historic Breakdown Service** - Introduce a new service serving the Historic scope, sizing each asset by gross total bought rather than net invested, built through the same shared builder.

**5. Breakdown Facade** - Update the existing public breakdown service so it acts as a facade routing a request to the Active or Historic implementation based on the scope value it already receives, keeping `SummaryController`'s existing call site unchanged.

**6. Dependency Injection** - Register the two scope-specific services and the facade in the Application layer's service collection, following the existing singleton registration pattern used by every other Application service.

### Stage 3: Test Coverage

**7. Shared Builder Tests** - Cover the extracted builder's filtering, sorting, and selector-driven amount assignment independently of any scope.

**8. Active Service Tests** - Adapt the existing breakdown service test suite to the renamed service and its simplified method signature, retaining coverage of net-invested sizing and broker-lookup guard behavior.

**9. Historic Service Tests** - Add coverage proving the Historic breakdown sizes by gross total bought, including a fully-closed position case where net-invested would misleadingly collapse toward zero.

**10. Facade Dispatch Tests** - Verify the facade routes each scope value, including the default, to the correct underlying scoped service.

**11. Integration Test Updates** - Extend the existing historic-scope breakdown endpoint test to assert on the actual sized values, add a regression test proving Active-scope endpoint behavior is unchanged, and add a test proving a historic portfolio with no qualifying assets is excluded.

### Stage 4: Documentation Alignment

**12. PRD Capability Wording** - Update the F07 capability description in the historic-investments PRD to name the Active/Historic breakdown services and the shared builder introduced by this feature, replacing the now-superseded reference to a single, undifferentiated breakdown service.
