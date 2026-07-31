# Implementation Plan: F03. Investment Annual Result Endpoint (Server-Side Computation)

**Prerequisites:**
- .NET solution builds and existing test suite passes on `main` (F01, F02, F04 already merged)
- No new NuGet/npm packages, environment variables, or configuration files required
- Branch `feat/P19-F03-investment-annual-result-endpoint`, already created from `main`

### Stage 1: Application Layer

**1. Combined Response DTO** - Add `InvestmentAnnualResultDTO`, reusing the existing `InvestmentAccountAnnualDiffDTO`/`NetPositionAnnualDiffDTO` nested types verbatim under the new top-level wrapper name.

**2. Extract Shared Investment Series Computation** - Pull the account-resolution and per-account/net-position `MonthlySeries`/diff-sequence logic out of `GetInvestmentDiffsForYear` into a private helper, with `GetInvestmentDiffsForYear` itself unchanged in behavior and output.

**3. New Combined Computation Method** - Add `GetInvestmentAnnualResultForYear` to `IAnnualSummaryService` and `AnnualSummaryService`, consuming the shared helper and computing `AverageMonthResult`/`SumOfMonthResults` via F01's `MonthlySeries.Average`/`.Sum()` at full (unrounded) precision, so the new endpoint's values stay byte-identical to the existing `investment-diffs` output.

### Stage 2: Presentation Layer

**4. New Endpoint** - Add a `GetInvestmentAnnualResult` action to `AnnualSummaryController` at `[HttpGet("{year:int}/investment-annual-result")]`, thin pass-through, alongside (not replacing) the existing `investment-diffs` action.

### Stage 3: Test Suite

**5. Service Unit Tests** - Add tests to `AnnualSummaryServiceTests.cs` confirming the new method's `Accounts`/`NetPosition` are byte-identical to `GetInvestmentDiffsForYear`'s output for the same data (including the unrounded `AverageMonthResult`), and that a year with no accounts/snapshots returns an empty accounts array and all-zero net position.

**6. API Integration Tests** - Add tests to `AnnualSummaryEndpointsTests.cs` covering a `200 OK` response for seeded snapshot data and for no data, and confirm the existing `investment-diffs` test still passes unmodified.

**7. Full Suite Verification** - Run the complete backend test suite (`dotnet test`) and confirm the solution still builds cleanly, verifying the extracted helper introduced no regressions to `GetInvestmentDiffsForYear`'s existing behavior.
