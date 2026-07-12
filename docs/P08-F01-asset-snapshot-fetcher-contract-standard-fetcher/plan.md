# Implementation Plan: Asset Snapshot Fetcher Contract & Standard Fetcher

**Prerequisites:**
- No new tools, libraries, or configuration — uses the existing xUnit + FluentAssertions test stack and the existing `GoogleFinance` integration as-is

### Stage 1: Fetcher Contract and Implementation

**1. IAssetSnapshotFetcher Interface** - Define the new strategy contract in the Application layer, with members for checking whether a fetcher applies to a given asset class and for producing a price snapshot from a request. Reference the spec for the exact member signatures and location.

**2. StandardAssetSnapshotFetcher Implementation** - Extract today's non-cryptocurrency price-fetch logic into a new Infrastructure-layer class implementing the interface, preserving the existing exchange validation and its exact error message, and delegating to the existing Google Finance integration unmodified. Reference the spec for the exact `Supports` semantics and file location.

### Stage 2: Wiring and Verification

**3. Dependency Injection Registration** - Register the new fetcher in the Infrastructure composition root as an implementation of the strategy interface, resolvable as part of a collection, without touching the existing `AssetPriceService` registration. Reference the spec's Component Overview for the exact registration point.

**4. Unit Test Coverage** - Add a new test file covering the fetcher's asset-class matching behavior and its exchange validation, following the existing sibling test file's structure and assertion style. Reference the spec's Testing Strategy for the full list of test functions and what stays intentionally untested.

**5. Regression Check** - Confirm `AssetPriceService`, its existing branching logic, and its existing test file remain completely untouched and still pass, since this feature only adds new, not-yet-wired types. Run the full test suite to verify no existing behavior changed.
