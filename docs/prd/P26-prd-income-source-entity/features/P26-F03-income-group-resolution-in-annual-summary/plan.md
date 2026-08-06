# Implementation Plan: Income Group Resolution in Annual Summary

**Prerequisites:**
- F01 merged (`AnnualSummaryService` group-lookup implementation, `ICashFlowRepository.GetIncomeSources()`)
- No new NuGet packages required

### Stage 1: Characterization Tests

**1. Unresolved Source Coverage** - Add test coverage proving an income whose source name matches no seeded `IncomeSource` record is treated as `NonReportable` (excluded from Salary and DividendoJuros totals) without raising an error, in both the Income Summary table computation and the Historical Averages computation. No production code changes — the behavior under test already shipped as part of F01.
