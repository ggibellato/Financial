# Implementation Plan: F02. Category Totals Endpoint (Server-Side Computation)

**Prerequisites:**
- .NET solution builds and existing test suite passes on `main` (F01, F04 already merged)
- No new NuGet/npm packages, environment variables, or configuration files required
- Branch `feat/P19-F02-category-totals-endpoint`, already created from `main`

### Stage 1: Application Layer

**1. Combined Response DTO** - Add `CategoryTotalsAnnualDTO` nesting the existing `CategoryAnnualTotalDTO` list and `IncomeAnnualSummaryDTO`, plus the four new computed fields (`TotalDespesasMonthly`/`TotalDespesasAnnualTotal`, `ResultadoMonthly`/`ResultadoAnnualTotal`), following the sealed/`required`-init style of its sibling DTOs.

**2. Combined Computation Method** - Add `GetCategoryTotalsAnnualForYear` to `IAnnualSummaryService` and implement it in `AnnualSummaryService`, composing the existing `GetCategoryTotalsForYear`/`GetIncomeSummaryForYear` methods unchanged, aggregating Total despesas across all 14 categories per month, and computing Resultado via F01's `AnnualResultCalculator.ComputeResultado(MonthlySeries, MonthlySeries, MonthlySeries)` overload (salary-after-taxes, total despesas, Investimento category series) — the corrected formula with no Dividendo/Juros term.

### Stage 2: Presentation Layer

**3. New Endpoint** - Add a `GetCategoryTotals` action to `AnnualSummaryController` at `[HttpGet("{year:int}/category-totals")]`, thin pass-through to the new service method, alongside (not replacing) the existing `expense-categories`/`income-summary` actions.

### Stage 3: Test Suite

**4. Service Unit Tests** - Add tests to `AnnualSummaryServiceTests.cs` covering: Total despesas equals the per-month sum across all categories; Resultado excludes Dividendo/Juros and includes Investimento; annual totals equal the sum of their monthly series; a zero-data year returns all-zero series; and the nested `categoryTotals`/`incomeSummary` match the existing standalone methods' output exactly.

**5. API Integration Tests** - Add tests to `AnnualSummaryEndpointsTests.cs` covering a `200 OK` response with the combined shape for seeded data and for a zero-data year, and confirm the existing endpoint tests (`expense-categories`, `income-summary`, `investment-diffs`, `historic-summary-averages`) still pass unmodified.

**6. Full Suite Verification** - Run the complete backend test suite (`dotnet test`) and confirm the solution still builds cleanly, verifying this feature introduced no regressions to the unmodified existing endpoints.
