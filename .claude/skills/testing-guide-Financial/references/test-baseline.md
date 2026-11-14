> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Test Baseline

Snapshot taken 2026-11-14 on `main` at `18089c83`. Counts are `[Fact]` / `[Theory]` /
`[SkippableFact]` attributes (a `[Theory]` counts once, not per row) and `it(` / `test(` calls.
Class counts by `Category` trait come from the `[Trait("Category", …)]` on each class. Compare a
later snapshot against this one to spot drift — but a falling count is not a problem in itself:
deleting a low-value test is an improvement (`../SKILL.md` §2). What matters is a layer losing the
tests that prove its rules, or features shipping without AC-tracing tests.

## .NET

| Project | Tests | Files | Classes by Category |
|---|---|---|---|
| `Financial.Api.Tests` | 696 | 84 | Integration 84, Unit 4 |
| `Financial.Architecture.Tests` | 34 | 15 | Unit 14, Integration 1 |
| `Financial.CashFlow.Domain.Tests` | 670 | 44 | Unit 44 |
| `Financial.CashFlow.Application.Tests` | 905 | 32 | Unit 32 |
| `Financial.CashFlow.Infrastructure.Tests` | 161 | 16 | Integration 10, Unit 6 |
| `Financial.Investment.Domain.Tests` | 620 | 37 | Unit 37 |
| `Financial.Investment.Application.Tests` | 556 | 35 | Unit 35 |
| `Financial.Investment.Infrastructure.Tests` | 240 | 27 | Integration 21, Unit 6 |
| `Financial.Shared.Abstractions.Tests` | 34 | 7 | Unit 7 |
| `Financial.Shared.Infrastructure.Tests` | 78 | 13 | Integration 9, Unit 4 |
| `Financial.Frankfurter.Tests` | 21 | 1 | Unit 1 |
| `Financial.GoogleIntegrations.Tests` | 42 | 7 | Unit 7 |
| `Financial.Observability.Tests` | 18 | 2 | Unit 2 |
| `Financial.WebPageParser.Tests` | 45 | 11 | Unit 7, Live 4 |
| `Financial.CashFlowSpreadsheetImport.Tests` | 230 | 34 | Unit 30, Integration 4 |
| `Financial.InvestmentSpreadsheetImport.Tests` | 48 | 4 | Unit 4 |
| `Financial.InvestmentDataQualityReport.Tests` | 9 | 1 | Unit 1 |
| **Total** | **4,407** | **370** | |

AC-tracing: 356 `[Trait("AC", …)]` attributes; 38 classes under `Tests/Financial.Api.Tests/Acceptance/`.

`Tests/Financial.App.E2ETests` and `Tests/Financial.Presentation.Tests` hold only `bin/`/`obj/`
(leftovers of the removed WPF client) and contain no tests.

## Financial.Web (Vitest)

| Folder | Tests | Files |
|---|---|---|
| `src/components` | 1,391 | 93 |
| `src/hooks` | 928 | 76 |
| `src/pages` | 483 | 25 |
| `src/utils` | 185 | 30 |
| `src/api` | 156 | 4 |
| `src/__tests__`, `src/context`, `src/navigation`, `src/acceptance` | 38 | 6 |
| **Total** | **3,181** | **234** |

AC-tracing: 220 `[AC …]` titles.

## E2E (Playwright)

8 spec files, 10 `@smoke` tests (`references/e2e-environment.md`).

## Coverage (from `coverage-baseline.json`)

| Job | Line | Branch |
|---|---|---|
| `backend` | 96.5% | 92.4% |
| `web` | 95.8% | 90.4% |

## How to refresh

```powershell
Get-ChildItem Tests -Directory | % { $m = Get-ChildItem $_.FullName -Recurse -Filter *.cs | ? FullName -notmatch '\\(obj|bin)\\' | Select-String '^\s*\[(Fact|Theory|SkippableFact)'; "{0}: {1}" -f $_.Name, @($m).Count }
Get-ChildItem Financial.Web/src -Recurse -Include *.test.ts,*.test.tsx | Select-String '^\s*(it|test)(\.each\(.*\))?\(' | Measure-Object
```
