# Test Coverage and Test Quality Audit: Financial

- **Repository:** `E:\dev\projetos\financial` at `main` @ `244539d3`
- **Audit date:** 2026-10-04
- **Mode:** static audit plus existing CI artifacts. No build, test or coverage run was performed locally.
- **Coverage data used:**
  - Downloaded from the latest `main` run of `Financial build`: run `36844680000`, 2026-10-01, head SHA `244539d3`. This is the same commit as the audited tree.
  - Artifacts: `coverage-report-backend`, `coverage-report-wpf`, `coverage-report-web`.
- **Branch protection:** read live with `gh api repos/ggibellato/financial/branches/main/protection`.
- **Counts:** all test counts are grep estimates of `[Fact]`/`[Theory]` methods or `it(`/`test(` blocks, unless stated otherwise.

---

## 1. Executive Summary

### Overall assessment

**Test health: good, but noisy.** There are about 8,460 automated tests (est.) across 18 real test projects/suites. The core domain is tested by its behaviour, especially Investment and CashFlow domain rules, persistence atomicity, data-version migrations and API endpoints. Examples:

- XIRR edge cases: `Tests/Financial.Investment.Domain.Tests/Domain/XirrCalculatorTests.cs`
- UK tax-year boundary: `TaxYearCalculatorTests.cs`
- Atomic JSON writes: `Tests/Financial.Shared.Infrastructure.Tests/Persistence/LocalJsonStorageTests.cs`
- Write-gate concurrency: `InvestmentJsonRepositoryTests.cs`
- Rollback on a failed save: `ReserveServiceTests.cs:PostWithdrawalAsync_BankPathSaveFails_RollsBackMovementAndBothExpenses`

Around this core sits a large layer of low-signal tests:

- 170 `Constructor_WithNull*` guard tests
- 84 `ControllerGuardClauseTests`, 34 of which test paths the file itself calls unreachable
- 35 "rethrows" tests that still pass if the `catch` block is deleted
- 29 "two ids differ" tests
- About 90–110 copy-pasted React dialog/tab tests

There is also a systemic determinism problem. 17 production call sites read `DateTime.Now/Today/UtcNow` instead of the injected `TimeProvider`, so hundreds of tests depend on the wall clock. Api.Tests also reaches the live Frankfurter API by default.

**CI test-gate health: real and enforced, but measures only execution.**

- `ci-status` is a verified required status check (strict). It fails when any test job fails, and test failures fail their jobs.
- Each of the three coverage gates is a **line-only, job-wide 90% floor** with no branch, diff, ratchet or mutation signal.
- Today the three jobs sit at 95.5%, 94.6% and 94.6%. A PR can add large amounts of untested branching logic and still pass.
- `enforce_admins` is `false`, so an admin can bypass the protection.

### Technologies and tooling found

| Area | Stack |
|---|---|
| Backend | .NET 10, ASP.NET Core API (serves the SPA), JSON-document persistence (LocalJson / Google Drive), CLI tools |
| Desktop | WPF desktop client (`Financial.App`, assembly `Financial.Presentation.App`) |
| Web | React 19 + TypeScript + Vite SPA |
| Deployment | Docker single image |
| .NET tests | xUnit 2.9.3, FluentAssertions 6.12.0, hand-written fakes and stubs (no Moq/NSubstitute; grep has 0 hits), `Microsoft.AspNetCore.Mvc.Testing`, coverlet XPlat, reportgenerator |
| Web tests | Vitest 4 + jsdom, Testing Library (`react`, `user-event`, `jest-dom`), `vi.mock`, `@vitest/coverage-v8` |
| E2E | Playwright library used from a single script (`Financial.Web/scripts/smoke-test.mjs`) |
| Not found | No mutation testing tool; no database or container tooling (none needed: JSON files) |

### Coverage at each level

- **Unit:** extensive at every level.
- **Integration:** substantial. Covers the API host via `WebApplicationFactory`, real-filesystem storage and serializers, and the WPF real-DI composition.
- **E2E:** one CI browser smoke test covering two read-only paths. There is no automated WPF E2E.

### Coverage data and trustworthiness

Current CI artifacts exist for the audited commit:

| Job | Line | Branch | Method |
|---|---:|---:|---:|
| backend | 95.5% | 87.8% | 97.3% |
| wpf | 94.6% | 80.7% | 90.6% |
| web | 94.6% | 87.5% | 85.7% |

These figures are trustworthy as *execution* measures. The exclusions are documented in `coverlet.runsettings` and the reports are assembly-filtered per job.

They are less trustworthy as *quality* measures, for four reasons:

1. Several hundred tests are execution-only (null-guards, rethrows, getters).
2. Web coverage counts test helpers (`src/test/**` and `src/test-utils/**` are not excluded).
3. The local `Financial.Web/CoverageReport/Summary.json` is stale (2026-09-06, 90.0%) and should not be read as current.
4. `Financial.Integrations.GoogleDrive`, the **production storage provider**, sits inside the backend figure at 21.1% line / 10.7% branch.

### CI quality gate

A gate exists. It measures line execution only and is verifiably merge-blocking through `ci-status`. It fails closed when `Summary.json` is missing.

### Three most important test risks or gaps

1. **Data-integrity defects not caught by any test.**
   - CashFlow reads hand out live collections while writes mutate them (`CashFlowJsonRepository.cs:26`), so a concurrent GET can throw "Collection was modified".
   - Reserve-split movements are rounded per bucket with no residual (`ReserveService.cs:345-349`), so a split can be off by ±£0.01.
   - `CashFlowSpreadsheetImport` defaults its output to the live data file and does not carry over Transfers, BalanceAdjustments or Tithe carry-forwards.
2. **Front-end correctness bugs that the tests themselves pin in place.**
   - React and WPF format the round-up suggestion differently: `"0.60"` vs `"0.6"` (`useExpenseForm.ts:33` vs `ExpenseWorkflowViewModel.cs:490`). Each suite asserts its own version.
   - `todayIsoDate()` returns the UTC date (`formatters.ts:76-78`), which is wrong between 00:00 and 01:00 BST. The tests compute the expected value the same way, so they cannot catch it.
   - Neither front end has an unsaved-changes guard, although `docs/ui` requires one.
3. **Non-determinism.**
   - Api.Tests calls the live Frankfurter API unless an FX override is passed (`ApiTestFactory.cs:55-62`).
   - 17 production sites bypass `TimeProvider`.
   - Locale-dependent money parsing and formatting is never tested under a non-`en` culture: `Intl.NumberFormat(undefined)`, `decimal.Parse` with the current culture.

### Three most important CI gate risks or gaps

1. **The coverage gate is line-only and job-wide.** It has no branch, diff or ratchet signal, so new decision-heavy code can merge untested as long as the job stays ≥ 90%.
2. **No test-strength (mutation) gate and no test-hygiene gate.** Nothing catches wall-clock use, `Task.Delay` synchronisation, live network calls, or `.only` and skipped tests.
3. **Bypass paths.**
   - `enforce_admins: false` (admins can merge red).
   - `detect-changes.sh` classes any `*.md` and `.claude/*` path as docs-only.
   - `OpenApiContractTests` passes unconditionally when `UPDATE_OPENAPI_SNAPSHOT` is set (`OpenApiContractTests.cs:41-46`).
   - There is no architecture gate for CashFlow ↔ Investment isolation (CLAUDE.md invariant 3).

### Behaviour-focused or coverage-number-focused?

**Mostly behaviour-focused at the core, with a visible coverage-number layer around it.** `ControllerGuardClauseTests.cs` openly tests "unreachable via real HTTP" paths, and the constructor and rethrow tests exist mainly to execute lines under a 90% line gate.

### Recommended actions (estimates)

| Action | Tests |
|---|---|
| Remove | ~120–130 |
| Merge or parameterize | ~560–600 tests, collapsing to ~110 |
| Rewrite | ~125–170 |
| Move or convert to integration (incl. re-trait) | ~195–205 |
| Delete (ghost directories) | 7 untracked `Tests/*` folders containing only bin/obj |

**Estimated simplification opportunity:** about 590 fewer test methods (~7% of the suite). The noise drops more than that figure suggests, because the removed tests are the ones that obscure real signal.

### Highest-priority actions

1. **P1:** Make `ApiTestFactory` register a deterministic FX stub by default.
2. **P1:** Add tests, and fixes, for the CashFlow read/write race, split residual, round-up parity, `todayIsoDate`, and the SPA fallback returning 200 HTML for unknown `/api/*` routes.
3. **P1:** Guard `UPDATE_OPENAPI_SNAPSHOT` against `CI=true`, and add a CashFlow ↔ Investment architecture rule.
4. **P2:** Add a diff branch-coverage gate and a per-job ratchet next to the 90% line floor, then remove or collapse the ~600 low-value tests.
5. **P2:** Inject `TimeProvider` in the 17 production sites and migrate the wall-clock tests.
6. **P3:** Run mutation testing on a schedule for `Financial.*.Domain`, report-only first.

---

## 2. Technology and Test Tooling

### Application stack

| Component | Technology | Evidence |
|---|---|---|
| Runtime | .NET 10 | `.github/workflows/build.yml` `setup-dotnet` `dotnet-version: 10.0.x` |
| Solution | `Financial.slnx` | repo root |
| API + SPA host | ASP.NET Core | `Financial.Api/Program.cs:142` `app.MapFallbackToFile("index.html")` |
| SPA | React 19.2, TS ~6.0, Vite 8, Fluent UI v9, react-router 7, recharts 3 | `Financial.Web/package.json` |
| Desktop | WPF, net10.0-windows | `Financial.App/Financial.App.csproj`, `Tests/Financial.Presentation.Tests` (`UseWPF`) |
| Persistence | One JSON document per context, LocalJson or GoogleDrive | `Financial.Shared.Infrastructure`, `Integrations/GoogleDrive`, CLAUDE.md |
| External APIs | Frankfurter FX, Google Drive/Sheets/Calendar, scraped price sites | `Integrations/*` |
| CLI tools | `Tools/CashFlowSpreadsheetImport`, `Tools/InvestmentSpreadsheetImport`, `Tools/ImportGoogleSpreadSheets`, DataQualityReport | `Tools/` |
| Deploy | Single Docker image on port 8080 | `Dockerfile`, `docker-compose.yml` |
| CI | GitHub Actions | `.github/workflows/build.yml`, `semantic-pr.yml`, `.github/actions/coverage-gate/action.yml`, `.github/scripts/detect-changes.sh` |

### Architecture and boundary map

| Boundary | Production projects | Main test projects |
|---|---|---|
| Domain | `Financial.CashFlow.Domain`, `Financial.Investment.Domain` | `*.Domain.Tests` (pure unit) |
| Application | `Financial.*.Application` | `*.Application.Tests` (sociable unit with `TestUtilities` stubs); also `Investment.Infrastructure.Tests/Services/*` (misplaced, see §4) |
| Infrastructure / persistence | `Financial.*.Infrastructure`, `Financial.Shared.Infrastructure` | `*.Infrastructure.Tests`, `Shared.Infrastructure.Tests` (real temp files) |
| Shared kernel | `Financial.Shared.Abstractions` | **No dedicated test project.** It is tested from `Shared.Infrastructure.Tests` and indirectly. |
| API / controllers / middleware | `Financial.Api` | `Financial.Api.Tests` (`WebApplicationFactory`) |
| External adapters | `Integrations/*` | `Frankfurter.Tests`, `GoogleIntegrations.Tests`, `WebPageParser.Tests`, `Observability.Tests` |
| Frontend components / hooks / state | `Financial.Web/src` | 165 `*.test.ts(x)` files |
| Desktop view models | `Financial.App` | `Financial.Presentation.Tests` |
| Architecture rules | n/a | `Financial.Architecture.Tests` (9 files, assembly-reference rules) |
| Tools | `Tools/*` | `CashFlowSpreadsheetImport.Tests`, `InvestmentSpreadsheetImport.Tests`, `InvestmentDataQualityReport.Tests` |

### Test tooling by level

| Test level | Framework / runner | Assertions | Mocking / fakes | Runtime dependencies | Coverage tool | Local command | CI command / job | Evidence |
|---|---|---|---|---|---|---|---|---|
| Unit (.NET) | xUnit 2.9.3 | FluentAssertions 6.12.0 | Hand-written stubs and fakes in `Tests/Financial.TestUtilities` (21 files: `StubCashFlowRepository`, `StubInvestmentRepository`, `FakeTimeProvider`, `RecordingTelemetryTracer`, …) | In-memory | coverlet XPlat via `coverlet.runsettings`; reportgenerator | `dotnet test` | `backend`: `dotnet test --configuration Release --no-build --filter "FullyQualifiedName!~Financial.Presentation.Tests" --settings coverlet.runsettings` | `Tests/Financial.Architecture.Tests/Financial.Architecture.Tests.csproj:12-16`; `build.yml` backend "Run tests" |
| Unit (WPF VM) | xUnit | FluentAssertions | Hand-written `Stub*`/`Spy`/`Fake*` (e.g. `ViewModels/Admin/TestStubs.cs`, 958 lines) | In-process WPF assembly | coverlet | `dotnet test Tests/Financial.Presentation.Tests` | `wpf` job | `build.yml` wpf "Run tests" |
| Unit (Web) | Vitest 4 (`vitest run`) | Vitest `expect` + `@testing-library/jest-dom` | `vi.mock`/`vi.fn` (`financialApiClient` mocked in 72 files) | jsdom 29 | `@vitest/coverage-v8` (`all: true`, `lcov` + `json-summary`) → reportgenerator | `npm test` / `npm run test:coverage` | `web`: `npm run test:coverage` | `Financial.Web/vite.config.ts`, `package.json` |
| Integration (.NET) | xUnit + `Microsoft.AspNetCore.Mvc.Testing` | FluentAssertions | `ApiTestFactory` with optional FX/time overrides | In-process HTTP host, temp JSON files, **live Frankfurter by default** | coverlet | `dotnet test Tests/Financial.Api.Tests` | `backend` | `Tests/Financial.Api.Tests/ApiTestFactory.cs:55-62` |
| Integration (storage) | xUnit | FluentAssertions | `ControllableJsonStorage`, `ObservableFakeClock` | Real filesystem (Guid temp files) | coverlet | `dotnet test Tests/Financial.Shared.Infrastructure.Tests` | `backend` | `LocalJsonStorageTests.cs` |
| Integration (Web, in-process) | Vitest + RTL | jest-dom | API client mocked | jsdom; page + hooks + real children | v8 | `npm test` | `web` | `src/pages/__tests__/*` (28 files) |
| Integration (WPF composition) | xUnit | FluentAssertions | Stubbed FX only | Real DI over a temp copy of TestData | coverlet | `dotnet test Tests/Financial.Presentation.Tests` | `wpf` | `Tests/Financial.Presentation.Tests/Integration/DashboardCompositionTests.cs` |
| Architecture | xUnit | FluentAssertions | none | Assembly metadata | not collected in `wpf`; collected in `backend` | `dotnet test Tests/Financial.Architecture.Tests` | `backend` **and** `wpf` (runs twice) | `build.yml` wpf "Run tests" line 2 |
| Contract | xUnit (`OpenApiContractTests`); Vitest (`openapiFreshness.test.ts`) | FluentAssertions / `expect` | none | API host; `openapi-typescript` CLI | yes | as above | `backend`, `web` | `Tests/Financial.Api.Tests/OpenApiContractTests.cs`; `Financial.Web/src/api/generated/__tests__/openapiFreshness.test.ts` |
| E2E | Playwright 1.61 library (`chromium.launch`), single script | Custom checks in the script | Seeds via HTTP POST | Published API + built SPA on :8080, seeded test JSON | none | `npm run smoke-test` | `smoke` job | `Financial.Web/scripts/smoke-test.mjs`; `build.yml` smoke |

### Commands

- **Test (backend):** `dotnet test` (CLAUDE.md). The CI form is above.
- **Test (WPF):** `dotnet test Tests/Financial.Presentation.Tests`
- **Test (web):** `cd Financial.Web && npm test`
- **Coverage (.NET):** `dotnet test --settings coverlet.runsettings --results-directory TestResults` (CLAUDE.md, `build.yml`)
- **Coverage (web):** `npm run test:coverage`
- **Mutation testing:** none discovered. Grep for `stryker|mutation` in json/yml/csproj/props: 0 hits.

### CI execution summary

| Step | What happens |
|---|---|
| Classify | `changes` runs `detect-changes.sh` to decide which jobs a diff needs. |
| `backend` (Windows) | Build → all non-WPF tests with coverage → reportgenerator (`-assemblyfilters:+*;-Financial.Presentation.App`) → coverage gate |
| `wpf` (Windows) | Presentation.Tests with coverage + Architecture.Tests → reportgenerator (`+Financial.Presentation.App`) → coverage gate |
| `web` (Ubuntu) | lint → `test:coverage` → reportgenerator → coverage gate → `npm run build` (`tsc -b`) |
| `smoke` | Publish API + SPA, seed test JSON, run Playwright |
| `coverage-comment` | Sticky PR comment |
| `ci-status` | Aggregates all jobs |
| Every push to `main` | Runs everything (`FULL_RUN`) |

### Potential but unverified tooling

- `Microsoft.Extensions.TimeProvider.Testing` is referenced in `Shared.Infrastructure.Tests`, alongside the hand-written `TestUtilities/FakeTimeProvider.cs`.
- `FakeTimeProvider` exists in `TestUtilities` but is **unused in `Financial.Presentation.Tests`**.
- No Testcontainers, emulator, msw, Stryker or `@playwright/test` config was found.

---

## 3. CI Test Gate Assessment

| Gate / check | CI workflow / job | Trigger | Metric or rule | Scope | Threshold / baseline | Can fail CI? | Merge-blocking? | Bypass risks | Effectiveness | Recommendation | Evidence |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Backend test execution | build.yml / `backend` | PR to main (if affected), push main | All non-WPF tests pass | Repo minus Presentation.Tests | Any failure | Yes | Yes, via `ci-status` | Path classification skip; admin bypass | Effective merge-blocking gate | Retain | `build.yml` backend "Run tests"; branch protection `contexts: [semantic-pr, ci-status]`, `strict: true` |
| WPF test execution | `wpf` | Same | Presentation + Architecture tests pass | WPF VM tests | Any failure | Yes | Yes (`ci-status`) | Same | Effective merge-blocking gate | Retain; drop the duplicate Architecture run | `build.yml` wpf "Run tests" |
| Web lint + test + typecheck build | `web` | Same | eslint, vitest, `tsc -b && vite build` | `Financial.Web` | Any failure | Yes | Yes (`ci-status`) | eslint has no vitest/testing-library rules (focused tests not caught) | Effective merge-blocking gate | Retain; add lint rules | `build.yml` web; `eslint.config.js` |
| Backend coverage gate | `backend` / `coverage-gate` | Same | **Line** coverage | Whole job, assembly-filtered | Discovered existing threshold: **90%** (fail < 90; warn < 100); current 95.5% | Yes (`exit 1`) | Yes (`ci-status`) | Line-only; job-wide (new code can hide in 5.5 pts of headroom); exclusions edited in the same PR | Partially effective gate | Strengthen (branch + diff + ratchet) | `.github/actions/coverage-gate/action.yml`; run 36844680000 |
| WPF coverage gate | `wpf` / `coverage-gate` | Same | Line | `Financial.Presentation.App` | 90%; current 94.6% (branch 80.7%) | Yes | Yes | Same; plus `DecimalInputBehavior` (money input) is excluded | Partially effective gate | Strengthen | as above; `coverlet.runsettings` |
| Web coverage gate | `web` / `coverage-gate` | Same | Line (reportgenerator from lcov) | `Financial.Web/src` minus generated/main/setup | 90%; current 94.6% (branch 87.5%) | Yes | Yes | Line-only; test helpers counted as product code; no vitest `coverage.thresholds` | Partially effective gate | Strengthen; exclude `src/test*/**` | `vite.config.ts`; `action.yml` |
| Gate artifact integrity | `coverage-gate` | Same | `summary.linecoverage` must be readable | n/a | `$null` → `exit 1` | Yes | Yes | None found: CI starts from a fresh checkout and `CoverageReport` is untracked | Effective merge-blocking gate | Retain | `action.yml` lines with `if ($null -eq $lineCoverage)` |
| OpenAPI contract snapshot | `backend` (`OpenApiContractTests`) | Same | Canonical OpenAPI must equal the committed snapshot | Public API | Exact match | Yes | Yes | **Fails open** if `UPDATE_OPENAPI_SNAPSHOT` is set (not set in CI today); missing snapshot → fails | Partially effective gate | Strengthen: fail when `CI=true` and the flag is set | `OpenApiContractTests.cs:41-46` |
| Generated-client freshness | `web` (`openapiFreshness.test.ts`) | Same | Generated TS must match the snapshot | `src/api/generated` | Exact match | Yes | Yes | Only runs when the `web` job runs. `Tests/Financial.Api.Tests/*` → web=true ✓ | Effective merge-blocking gate | Retain | `detect-changes.sh` "contract" rule |
| Architecture / layering | `backend` + `wpf` | Same | Assembly-reference rules | Domain→App→Infra, Shared, Integrations isolation | Any violation | Yes | Yes | **No CashFlow ↔ Investment isolation rule** (invariant 3); no "Domain has no framework refs" rule | Partially effective gate | Add rules | `Tests/Financial.Architecture.Tests/*DependencyRuleTests.cs` (grep shows no cross-context `NotContain`) |
| Browser smoke | `smoke` | PR if `smoke`=true, push main | 2 read paths + no console errors | Full app | Any failure | Yes | Yes (`ci-status`) | Skipped when backend/web failed (still caught by `ci-status`); substring match `includes('25.00')` | Partially effective gate | Strengthen with one write flow | `build.yml` smoke; `smoke-test.mjs` |
| Change classification | `changes` | All | Path → job map | All files | Fail-safe on unknown path / missing base | Yes (`ci-status` requires `changes` success) | Yes | `*.md` anywhere and `.claude/*` count as docs; a rule mistake silently skips jobs | Effective merge-blocking gate (with bypass risk) | Retain; add a self-test | `.github/scripts/detect-changes.sh` |
| Aggregate | `ci-status` | Always | Any need `failure`/`cancelled` → fail; skipped = pass | All jobs | n/a | Yes | **Yes: required check, strict** | `enforce_admins: false` | Effective merge-blocking gate | Retain; consider `enforce_admins` | branch-protection API |
| PR title | semantic-pr.yml | PR | Conventional Commits | Title | n/a | Yes | Yes (required) | n/a | Effective merge-blocking gate (not a test gate) | Retain | `semantic-pr.yml` |
| Coverage PR comment | `coverage-comment` | PR | Displays gate result | n/a | n/a | No (not in `ci-status` needs) | No | n/a | Effective non-blocking observability check | Retain | `build.yml` coverage-comment |
| Branch coverage | — | — | — | — | — | — | — | — | **Missing gate** (reported, not enforced) | Add | reportgenerator `Summary.json` has `branchcoverage` |
| Diff / patch coverage | — | — | — | — | — | — | — | — | **Missing gate** | Add | none found |
| Coverage ratchet | — | — | — | — | — | — | — | — | **Missing gate** | Add | none found |
| Mutation testing | — | — | — | — | — | — | — | — | **Missing gate** | Add (scheduled, report-only first) | grep: 0 hits |
| Test hygiene (clock / sleep / network / only / skip) | — | — | — | — | — | — | — | — | **Missing gate** | Add | eslint config; no analyzers found |
| AC traceability | — | — | — | — | — | — | — | — | **Missing gate**: 111 `[Trait("AC", …)]` across 23 files, never validated | Add (advisory) | grep `Trait("AC"` |

### Current gate verdict

- **Is there a test-quality gate?** Yes: three line-coverage floors at 90%, plus test-execution, contract and architecture tests.
- **Is it real and enforced?** Yes. The gates `exit 1`, the jobs feed `ci-status`, and `ci-status` is a required, strict status check on `main`. This was verified via the GitHub API, which is outside the repo files.
- **What does it measure?** Mainly **line execution**. The contract snapshot and assembly-reference architecture rules add behavioural signal. Nothing measures branches, assertion strength or determinism.
- **Is it scoped to changed code or high-risk modules?** No. It is job-wide, with path-based job selection only.
- **Does it block merges?** Yes, through `ci-status`. Admins can bypass (`enforce_admins: false`).
- **Does it fail closed on missing artifacts?** Yes for `Summary.json`. The OpenAPI snapshot fails open under its update flag.

### Gate weaknesses

1. **Line-only gate.** Branch coverage is lower in every job: backend 87.8, WPF 80.7, web 87.5. Weakest WPF classes by branch: `ReportingCurrencyViewModel` 35.7%, `ReservaViewModel` 50% (24/48), `IncomeSplitViewModel` 54.1%.
2. **No diff coverage.** A PR can add an untested class and still pass, because there is 4.6–5.5 points of headroom. The gate cannot say which changed lines are uncovered.
3. **No ratchet.** Coverage can drift from 95.5% to 90.0% silently.
4. **It rewards execution-only tests.** The 34 "unreachable" null-body tests in `ControllerGuardClauseTests.cs` exist to execute guard lines.
5. **Exclusions are uncontrolled.** `coverlet.runsettings` `<Exclude>` can be extended in the same PR that adds untested code. It currently excludes `DecimalInputBehavior`, the money-input behaviour.
6. **Inconsistent vendor-wrapper policy.** `GoogleSheets` is excluded, but `GoogleDrive` (21.1% line) and `GoogleCalendar` (33.7%) are not. They are counted inside the backend figure and dilute it.
7. **No determinism or hygiene gate.** Nothing catches `DateTime.Now` in services that inject `TimeProvider`, `Task.Delay` synchronisation (WPF has 10 sites), live HTTP in tests, or committed `.only`.
8. **The snapshot test fails open** under `UPDATE_OPENAPI_SNAPSHOT`.
9. **No architecture rule for bounded-context isolation.** There are no cross references today (csproj grep), but nothing would block a new one.
10. **Admin bypass:** `enforce_admins: false`.
11. **Architecture tests run twice** (`backend` and `wpf`). Minor cost.

### Recommended gate design

| Priority | Proposed gate | Risk addressed | Scope | Stage | Blocking or advisory | Initial threshold / baseline | Tooling | Runtime impact | Rollout |
|---|---|---|---|---|---|---|---|---|---|
| P1 | Keep test execution required; turn on `enforce_admins` (optional for a single-user repo) | Red merges | All | PR + main | Blocking | n/a | Branch protection | None | Immediate |
| P1 | Snapshot update guard: fail when `CI` is set and `UPDATE_OPENAPI_SNAPSHOT` is non-empty | Silent contract drift | `OpenApiContractTests` | PR | Blocking | n/a | Code change in the test | None | Immediate |
| P1 | Architecture: CashFlow ↔ Investment isolation; Domain has no framework refs | Context coupling | Assemblies | PR | Blocking | Zero violations (current state) | Existing `ProjectAssembly` helper | < 1 s | Immediate |
| P2 | Diff **branch** coverage on changed lines | Untested new decision logic | Changed files in `*.Domain`, `*.Application`, `Financial.Api`, `Financial.App/ViewModels`, `Financial.Web/src/{hooks,utils}` | PR | Advisory → blocking | Baseline to measure before enforcing. Suggested starting threshold: 80% diff-line / 70% diff-branch, chosen to sit below today's 87–88% backend/web branch averages so legitimate PRs pass. | Candidate: `diff-cover` over Cobertura/lcov (not in repo) | Seconds | 2–4 weeks observe-only, then block |
| P2 | Per-job ratchet on line **and** branch | Silent regression | Each job | PR | Blocking | Baseline from the latest `main` artifact: backend 95.5/87.8, WPF 94.6/80.7, web 94.6/87.5; tolerance suggested starting point 0.5 pt | Committed `coverage-baseline.json` + extension of `coverage-gate` | Seconds | Baseline updated only by a reviewed PR |
| P2 | Exclusion-change guard: a PR touching `coverlet.runsettings` `<Exclude>` or `vite.config.ts` `coverage.exclude` must carry a label | Hiding untested code | Config files | PR | Advisory | n/a | `detect-changes.sh`-style script | None | Advisory |
| P2 | Test-hygiene script: new `DateTime.Now/Today/UtcNow` in `Tests/**` or in services with a `TimeProvider` ctor parameter; `Task.Delay`/`Thread.Sleep` in tests; `.only(`; new `Skip =` | Flakiness | Diff only | PR | Advisory → blocking on new violations | Zero new violations | grep script; `eslint-plugin-vitest` `no-focused-tests` | Seconds | Diff-only to avoid legacy noise |
| P3 | Mutation testing (Stryker.NET) on `Financial.*.Domain` + selected Application services | Weak assertions | High-risk modules | Nightly / weekly | Report-only, then break threshold | Baseline to measure before enforcing | Candidate: Stryker.NET (not in repo) | Minutes to hours; keep off the PR path | Report → baseline → `--break-at` (suggested starting point: baseline − 5) |
| P3 | AC-traceability report | Unverified PRD criteria | `Trait("AC", …)` vs `docs/prd/**` Section 9 | main / nightly | Advisory | n/a | Script | Seconds | Advisory only; tags alone are not proof |
| P3 | Lower the 90% line floor to a tripwire *after* the diff and ratchet gates are proven | Coverage gaming | Jobs | PR | Blocking | Keep 90 (discovered) until the replacement gates hold | existing | None | Last |

---

## 4. Test Suite Classification

### Counts (estimates)

| Category | Suites / projects | Approx. tests |
|---|---|---|
| Unit | Domain ×2, Application ×2, Frankfurter, GoogleIntegrations, Observability, WebPageParser, InvestmentDataQualityReport, InvestmentSpreadsheetImport, Presentation.Tests (VMs, converters), Web utils/hooks/components | ~5,900 |
| Integration | Api.Tests (minus ~94 unit), *.Infrastructure.Tests, Shared.Infrastructure.Tests, CashFlowSpreadsheetImport.Tests (filesystem migrators), Web pages/acceptance, WPF `DashboardCompositionTests` | ~2,450 |
| E2E | `smoke-test.mjs` (1 script); 9 skipped live-verification tests | 1 (+9 skipped) |
| Unclear / mixed | Investment.Infrastructure.Tests (`Services/*` are Application integration); Api.Tests (unit + integration with no trait); Web hook-mocking component tests | (see table) |
| Architecture | Architecture.Tests | 17 methods |

### Suites

| Suite | Files | Tests (est.) | Classification | Runtime dependencies | Name vs scope | Evidence |
|---|---:|---:|---|---|---|---|
| `Financial.CashFlow.Domain.Tests` | 24 | 281 | Unit | none | ✓ | 0 IO/time hits |
| `Financial.CashFlow.Application.Tests` | 28 | 657 | Unit (sociable) | `StubCashFlowRepository`, `FakeTimeProvider`; 16 wall-clock uses | ✓ | |
| `Financial.CashFlow.Infrastructure.Tests` | 14 | 105 | Integration | Temp files, polling | ✓ | |
| `Financial.Investment.Domain.Tests` | 37 | 603 | Unit | 18 wall-clock uses | ✓ | |
| `Financial.Investment.Application.Tests` | 35 | 611 | Unit (sociable) | Stubs; **134 wall-clock uses** | ✓ | `PortfolioAssetSummaryServiceTests` 46 |
| `Financial.Investment.Infrastructure.Tests` | 27 | 242 | **Mixed** | Filesystem, `FakeHttpMessageHandler` | ✗ `Services/*` (~79 tests) are Application integration | `AssetPriceLookupServiceTests.cs` (32) |
| `Financial.Shared.Infrastructure.Tests` | 17 | 94 | Unit + integration | Filesystem; fakes | ✗ partly: tests `Shared.Abstractions` types | `UsdBasedExchangeRateProviderTests`, `SyncStatusTests` |
| `Financial.Architecture.Tests` | 9 | 17 | Architecture (static) | Assembly metadata | ✓ | |
| `Financial.Api.Tests` | 64 | 648 | Integration (~94 unit inside) | `WebApplicationFactory`, temp JSON, **live Frankfurter** | ✗ partly: `ControllerGuardClauseTests` (84), `DomainExceptionLoggingTests` (8) are unit | `ApiTestFactory.cs:55-62` |
| `Financial.CashFlowSpreadsheetImport.Tests` | 28 | 185 | Unit + filesystem integration | ClosedXML in memory; temp `.bak` | ✓ | |
| `Financial.Frankfurter.Tests` | 1 | 18 | Unit | Fake handler | ✓ | |
| `Financial.GoogleIntegrations.Tests` | 6 | 29 | Unit | Fake handler; **real 2 s backoff** | ✓ | `RetryPolicy.cs:5,9,38` |
| `Financial.InvestmentDataQualityReport.Tests` | 1 | 9 | Unit | none | ✓ | |
| `Financial.InvestmentSpreadsheetImport.Tests` | 4 | 48 | Unit | `StubDataSource` | ✓ | |
| `Financial.Observability.Tests` | 4 | 23 | Unit / DI wiring | localhost endpoint with no listener | ✓ | |
| `Financial.WebPageParser.Tests` | 10 | 37 (9 skipped) | Unit + skipped live E2E | Strings; live sites (skipped) | ✓ | `*VerificationTests.cs` `Skip=` |
| `Financial.Presentation.Tests` | 155 | ~1,731 | Unit (VM) + 1 integration + 6 source-text guards | Stubs; `Task.Delay` (10 sites) | ✓ | `ThreadPoolWarmup.cs` |
| `Financial.Web` vitest | 165 | ~2,124 | Unit (utils/hooks/components), in-process integration (pages/acceptance), contract | jsdom; API client mocked | ✓ mostly | `vite.config.ts` |
| `smoke-test.mjs` | 1 | 1 script | E2E | Published app on :8080, seeded JSON | ✓ | `build.yml` smoke |
| `TestUtilities` | 21 | 0 | Support library | n/a | n/a | |

### Ghost directories

These seven directories under `Tests/` contain only bin/obj. `git ls-files` returns 0 for each, and none is in `Financial.slnx`:

- `Financial.CashFlowBankMigration.Tests`
- `Financial.CashFlowBankOpeningBalanceMigration.Tests`
- `Financial.CashFlowIncomeMigration.Tests`
- `Financial.CashFlowPaymentStateMigration.Tests`
- `Financial.GoogleFinancialSupport.Tests`
- `Financial.InvestmentCurrencyBackfill.Tests`
- `Financial.InvestmentTransactionIncomeVocabularyMigration.Tests`

They have no CI impact. They are local clutter left from the migration-tool consolidation, and they mislead directory-based inventories.

### Mismatches and mixed layers

- `Investment.Infrastructure.Tests/Services/*` tests Application services (`AssetPriceLookupService`, `NavigationService`, `CreditService`, `TransactionService`) through real `LocalJsonStorage`.
- The Credit/Transaction CRUD tests there **never reload from disk**: the only `LoadSync` is in `CreateService()`. So they are neither real persistence tests nor distinct from `Investment.Application.Tests/Services/CreditServiceTests.cs`.
- `Api.Tests` mixes ~94 pure unit tests with host-level integration tests and has no trait to split them.
- `Shared.Infrastructure.Tests` tests `Financial.Shared.Abstractions` types (`UsdBasedExchangeRateProvider`, `SyncState`). No `Financial.Shared.Abstractions.Tests` exists, so `CompensatingSaveHelper` is tested only indirectly.

### Missing test levels

- No automated WPF E2E (manual UI Automation only).
- Browser E2E covers no write workflow.
- No tests of the production-only paths: SPA fallback with `wwwroot` present, `UseExceptionHandler` 500 body.

---

## 5. Coverage and TDD Assessment

### Available metrics and their limits

Source: run 36844680000, the same commit as audited.

| Job | Line | Branch | Method | Gate |
|---|---:|---:|---:|---|
| backend (15 assemblies) | 95.5% (11229/11746) | 87.8% | 97.3% | 90% line, enforced |
| WPF | 94.6% | 80.7% | 90.6% | 90% line, enforced |
| web | 94.6% | 87.5% | 85.7% | 90% line, enforced |

Weak assemblies inside the backend figure:

| Assembly / class | Line | Branch | Note |
|---|---:|---:|---|
| `Financial.Integrations.GoogleDrive` | 21.1% | 10.7% | `GoogleDriveClient` 13.5% line, 3/28 branches. No test references `GoogleDriveClient`. **This is the production storage provider.** |
| `Financial.Integrations.GoogleCalendar` | 33.7% | — | |
| `Financial.Integrations.WebPageParser` | 47.8% | — | `GoogleFinance` 2.1% line, 0/80 branches |

Domain and Application assemblies are 97–99.8% line and 90.9–93.6% branch.

Limitations:

1. The reports are not split by test level, so a line counts as covered whether a unit test or an API integration test reached it.
2. Hundreds of execution-only tests inflate both line and method coverage.
3. Web counts `src/test/**` and `src/test-utils/**` as product code.
4. Exclusions remove `DecimalInputBehavior` (money input) and, by documented design, all WPF Views/Controls/Components.

### High-branch-risk classes (low branch coverage, decision-heavy)

| Layer | Class | Branch coverage |
|---|---|---|
| Backend | `AssetAdminService` | 75% (24/32) |
| Backend | `CorporateActionReplay` | 79.5% (35/44) |
| Backend | `SummaryController` | 75% (9/12) |
| Backend | `AssetPriceHistoryService` | 75% |
| WPF | `ReservaViewModel` | 50% (24/48) |
| WPF | `CorporateActionsTabViewModel` | 57.8% (74/128) |
| WPF | `CardsWorkflowViewModel` | 57.1% |
| WPF | `TransferWorkflowViewModel` | 66.1% |
| WPF | `IncomeSplitViewModel` | 54.1% |
| WPF | `ReportingCurrencyViewModel` | 35.7% |
| Web | `CreditsTab.tsx` | 64% (48/75) |
| Web | `EditMovementForm.tsx` | 71.4% |
| Web | `ExpenseForm.tsx` | 77.2% |
| Web | `TransactionsTab.tsx` | 77.9% |

The WPF lows are mostly delete/state-change **failure paths**, e.g. `ReservaViewModel.DeleteMovementAsync`, `CardsWorkflowViewModel.Mark/UnmarkStatementPaidAsync`. Branch coverage matters more than line coverage here.

### Areas reviewed against risk

| Area | Status | Evidence |
|---|---|---|
| **Domain rules and calculations** | Strong. XIRR, tax-year, lot tracking, corporate actions, round-up, bucket rounding mode, overdraft confirmation. | `XirrCalculatorTests.cs`, `TaxYearCalculatorTests.cs`, `ReserveBucketTests.cs:CalculateSplitAmount_OnAnExactMidpoint_RoundsAwayFromZeroNotToEven`, `BankTests.cs:SetOpeningBalance_WithNegativeValue_ThrowsAndLeavesPriorValuesUntouched` |
| **Data integrity: persistence** | Strong for writes (atomic stage/rename, write gate, migrations, idempotent backfill). | `LocalJsonStorageTests.cs`, `InvestmentJsonRepositoryTests.cs`, `InvestmentSerializerAdapterTests.cs:Deserialize_RecordAlreadyHavingCurrency_IsNotOverwritten` |
| Data integrity: reads during writes | **Gap.** | See below |
| Data integrity: split totals | **Gap.** | See below |
| Data integrity: import carry-over | **Gap.** | See below |
| **Security** | Single-user, no auth; boundary is input validation plus error-body leakage. | Body-less `BadRequest()` (71 sites) and `ValidationProblemDetails` shape unpinned; no Production 500 body test. `DomainExceptionLoggingTests:RejectedWithdrawal_LogsTheExceptionType_WithoutTheFinancialValuesInItsMessage` is a good privacy test. |
| **Error handling: Frankfurter** | Thorough: malformed, non-2xx, fallback window. | `FrankfurterExchangeRateProviderTests.cs` |
| Error handling: API mapping | `ArgumentException` → 400 hides server faults; `TransientStorageException` unmapped (→ 500, not 503); no table test over the 8 mapped types. | `DomainExceptionMappingMiddleware.cs:43-62` |
| **Concurrency** | Investment has an enumeration-safety test and write-gate tests; CashFlow has only the write-gate test. | `AssetTests.cs:SetPrice_WhilePriceSnapshotsIsBeingEnumerated_DoesNotDisturbTheEnumeration` |
| **External integration** | Frankfurter well covered. GoogleDrive client, OAuth exchange/refresh `invalid_grant`, and GoogleFinance selector extraction are untested. | `GoogleCalendarOAuthClient.cs:61,79,254`; 0 test refs to `GoogleDriveClient`/`GoogleFinance.TryRead*` |
| **Critical workflows** | API endpoint + Acceptance tests are rich. Browser smoke covers 2 reads. | `ReserveEndpointsTests.cs:PostWithdrawal_BankPathOverdraftUnconfirmed_ReturnsConflictAndSavesNothing` |

**Data-integrity gaps in detail**

- **Reads during writes.** `ItemCollection` returns the live list (`CashFlowJsonRepository.cs:26`: `GetExpenses() => _data.Expenses`), so a GET can enumerate while a POST mutates it.
- **Split totals.** `ReserveService.cs:345-349` (`CreateSplitMovements`) rounds per bucket with no residual. The only test that touches it is a mirror test: `IncomeServiceTests.cs` ~line 360 computes its expected value with `bucket.CalculateSplitAmount`.
- **Import carry-over.**
  - `Tools/CashFlowSpreadsheetImport/Program.cs:33-36` defaults the output to the live `data/data-cashflow.json`.
  - `CarryOverDataTheSpreadsheetDoesNotOwn` (`Program.cs:199-236`) omits Transfers, BalanceAdjustments, Tithe carry-forwards and user Categories.
  - None of this is tested. Intent needs confirming.

### Good TDD-aligned examples

- `useExpenseForm.test.ts`: "stops recalculating once the round-up field is edited manually, even as the value keeps changing"
- `MonthlyPage.test.tsx`: "does not unmark a paid statement when the user cancels the confirmation" (uses fake timers plus `setSystemTime`)
- `ExpenseEndpointsTests.cs`: `GetExpensesByMonth_AfterMarkStatementPaid_CardChargeReappears`
- `CreditCardReferenceMigratorTests.cs`: `Migrate_ExpenseWithUnresolvableCardName_AbortsWithoutWritingOrBackingUpTheFile`
- `PaymentsDueServiceTests.cs`: `GetPaymentsDue_MensaisDueDayBeyondMonthLength_ClampsToLastDayOfMonth`

### Implementation-coupled or unable to catch regressions

**Rethrow tests (35).** For example, `ReserveServiceTests.cs:GetBucketBalances_WhenRepositoryThrowsUnexpectedly_Rethrows` still passes if the `catch` is deleted. The catch exists to mark the span failed, and only 2 `*FailedSpan*` tests check that.

**Mirror tests.** These compute the expected value with the production logic:
- `IncomeServiceTests.cs` around lines 360 and 463.
- Web tests that build the expected value with `new Date().toISOString().slice(0,10)`, and `UkExpensePromptDialog.test.tsx:53` comparing against `todayIsoDate()`.

**Bugs pinned by tests.**
- `useExpenseForm.test.ts` expects `'0.60'`, while `ExpenseWorkflowViewModelTests` expects `"0.6"`. Each locks in one side of a parity divergence.
- `api/__tests__/config.test.ts` "API_BASE_URL_WhenUnset_ReturnsEmptyString" positively asserts the empty base URL that CLAUDE.md forbids.

**Coupled to Fluent internals.** `App.test.tsx` "renders_in_light_mode_by_default_regardless_of_os_preference" walks `document.styleSheets` for `/^fui-FluentProvider_r_\d+_$/`.

**Assert that something did *not* complete within 300 ms.** `InvestmentJsonRepositoryTests.cs:192` and `CashFlowJsonRepositoryTests.cs:121` (`Task.WhenAny(…, Task.Delay(300))`) can only false-pass. Acceptable, but weak.

### Will tests survive refactoring?

Domain, Application and API tests: mostly yes. They assert outcomes through public APIs, and hand-written fakes avoid interaction coupling.

Weaker spots:
- Web component tests that mock their own hook (`TransactionsTab`, `CreditsTab`, `PriceHistoryTab`).
- WPF tests synchronised with `Task.Delay(50)`.
- The 4 XAML regex source-text guards. These are deliberate and documented by past bugs, but regex-fragile.

### Where branch coverage matters more than line coverage

- WPF failure paths (listed above).
- `CorporateActionReplay`.
- `AssetAdminService`.
- `PaymentsDueService` due-date construction (month rollover).
- `AnnualAverageMonthsCalculator`: January → 0 and the 2017 special case have no direct test.
- `UsdBasedExchangeRateProvider` today/future branch.
- Web form validators.

### Intentionally not recommended for testing

- WPF Views/Controls/Components code-behind (documented exclusion).
- Generated `src/api/generated/**`.
- GoogleSheets SDK wrapper.
- `GoogleFinanceVerifier` (manual diagnostic).
- More constructor null-guards, getters, DI registration or camelCase serializer echoes (`WatchlistEndpointsTests.cs:GetWatchlist_JsonUsesGroupAndNameProperties`).
- Snapshot or static-markup tests.

### Does the CI gate reward meaningful tests?

No. It can be satisfied by execution-only tests, and the codebase shows that happening: the 34 tests in `ControllerGuardClauseTests.cs` cover what the file's own header calls paths "unreachable via real HTTP calls".

---

## 6. Test Quality Findings and Removal Plan

| Action | Count (est.) | Reason |
|---|---:|---|
| Remove | ~125 | Unreachable guard tests (34); duplicate Id tests (~27); plain `Create_*_Assigns*` echoes (~20); `ConvertBack_*Throws*` (19); static/stylesheet web tests (~10–15); duplicate constructor/observability/Swagger tests (~6); `SyncState` member-count test; Infrastructure Credit/Transaction CRUD tests if not rewritten |
| Merge / parameterize | ~580 → ~110 | 170 constructor null-guards; 35 rethrow; 47 `CashFlowDataTests`; 18 ReferenceConverter; 16 + 6 migrator generics; ~35 React dialog contracts; ~36 list-tab templates; converter cases; finance-service triplets |
| Rewrite | ~125–170 | Wall-clock tests → pinned clock; mirror tests → literals; ~30 web mock-call-only where outcome is observable; `Task.Delay` sync; locale-dependent assertions; weak assertions (`DataQualityReportFormatterTests`, `ResolvesPastFileCheck`); `config.test.ts` empty-URL expectation; Swagger 404 test |
| Move / convert | ~200 | Investment.Infrastructure `Services/*` (~79) → Integration category; `UsdBasedExchangeRateProviderTests` → new `Shared.Abstractions.Tests`; Api.Tests unit tests (~94) → `Category=Unit` trait; round-up and split-tolerance rules (~25 front-end tests) → Domain/Application; 9 Skip'd live tests → `Category=Live` trait |
| Retain | ~7,400 | Unique behavioural protection |

### Duplicate coverage

| Classification | Duplicate test(s) to remove | Test to retain | Overlapping behaviour | Unique risk retained test covers | Why removal is safe | Action |
|---|---|---|---|---|---|---|
| Duplicate test | `Tests/Financial.Investment.Infrastructure.Tests/Services/CreditServiceTests.cs`: `AddCredit_WithValidRequest_ReturnsDetailsWithNewCredit`, `UpdateCredit_WithValidRequest_UpdatesCredit`, `DeleteCredit_WithValidRequest_RemovesCredit`, `AddCredit_NegativeValue_PersistsAsACorrection` | `Tests/Financial.Investment.Application.Tests/Services/CreditServiceTests.cs`: `AddCreditAsync_ValidRequest_AddsCreditAndReturnsAssetDetails`, `UpdateCreditAsync_ExistingId_…`, `DeleteCreditAsync_ExistingId_…`, `AddCreditAsync_NegativeValue_AddsAsACorrection` | Credit CRUD result DTO | Application rule | The Infrastructure versions never reload from disk (only `LoadSync` is at lines 182–184), so they prove no persistence | **Rewrite** to reload (persistence round-trip), otherwise Remove. Same for `TransactionServiceTests.cs` (4) |
| Duplicate test | `Investment.Infrastructure.Tests/Services/NavigationServiceTests.cs`: `Constructor_WithNullHoldingValuationService_ThrowsArgumentNullException`, `Constructor_WithNullTracer_ThrowsArgumentNullException` (+1) | `Investment.Application.Tests/Services/NavigationServiceTests.cs`: `Constructor_WithNullHoldingValuationService_Throws`, `Constructor_WithNullTracer_Throws` | Null guard | Same | Identical behaviour | Remove |
| Duplicate test | `Tests/Financial.Observability.Tests/BackendConfigurationTests.cs`: `AddObservability_LangfuseBackendWithBothKeys_RegistersTheRealTracer` | `ObservabilityServiceCollectionExtensionsTests.cs`: `AddObservability_WhenEnabledWithLangfuseBackend_RegistersOpenTelemetryTracer` | Same configuration, same assertion | — | Identical | Remove |
| Duplicate test | `Tests/Financial.Api.Tests/SwaggerEndpointsTests.cs`: `GetOpenApiDocument_InDevelopment_ReturnsValidJson` | `OpenApiContractTests.cs`: `OpenApiDocument_MatchesTheCommittedSnapshot` | Fetching `/openapi/v1.json` | Full contract | Strict subset | Remove |
| Duplicate test | 5 ReferenceConverter classes, 18 tests (`Read_KnownId_ResolvesToTheLookupInstance` ×5, `Read_IdAbsentFromLookup_ThrowsNamingTheMissingId` ×5, `Read_NullToken_ReturnsNull` ×3) | One generic `ReferenceConverterTests<T>` | Generic base `ReferenceConverter.cs`; subclasses are one-liners | — | One code path | Parameterize (18 → ~4) |
| Duplicate test | `CashFlow.Domain.Tests/Entities/CashFlowDataTests.cs` (47 Add/Remove/RemoveUnknown) | `Entities/Collections/IdCollectionTests.cs` + one `MemberData` wiring theory | Collection add/remove | `IdCollection<T>` semantics | Logic lives in `IdCollection<T>` | Parameterize (47 → ~6) |
| Duplicate test | `CashFlowSpreadsheetImport.Tests/Migrations/{Category,CreditCard,Entity,ReserveBucket}References/*Tests.cs` × `Migrate_CreatesABackupBeforeWriting`, `Migrate_FileDoesNotExist_ReturnsNoOpSummary`, `Migrate_FileAlreadyInCurrentShape_…`, `Migrate_SecondRunOnAlreadyMigratedFile_…`; plus `Migrate_WithNullData_Throws` ×6 | One theory per behaviour over the migrators | Shared migrator scaffolding | Per-migrator specifics retained | Same logic | Parameterize (22 → ~5) |
| Duplicate test | `StatusInvestFinanceServiceTests`, `RedentiaFinanceServiceTests`, `DicionarioDoInvestidorFinanceServiceTests` (`GetAssetValue_BlankName_ThrowsArgumentException`, `Constructor_WithNull…`, `GetAssetValue_ValidName_DelegatesToLookup`) | One theory per behaviour | Identical shape | — | — | Parameterize (9 → 3) |
| Duplicate test | Web: "calls onCancel when Cancel is clicked" ×12 dialogs; "shows a server error and re-enables Save when the submit rejects" ×10; "disables Save … when the name is blank" ×9 | One shared `describe.each` over the dialog components | Dialog contract | Per-dialog field validation retained | Same Fluent `Dialog` pattern | Parameterize (~31 → ~3 tables) |
| Duplicate test | Web: `CreditsTab`/`PriceHistoryTab`/`TransactionsTab` share ~12 identical tests (`renders_loading_state`, `renders_error_state_with_retry`, `save_button_disabled_while_saving`, …) | Shared table-driven suite | List-tab template | Tab-specific columns/rules retained | Same structure | Parameterize (~36 → ~12) |
| Duplicate test | `GoogleIntegrations.Tests/GoogleRetryPolicyTests.cs`: 5 sync/async pairs | Theory over sync/async | Retry decision | — | Mirror pairs | Merge |

### Meaningless or framework-focused tests

| Classification | Test to remove | Why it is meaningless | Regression it cannot detect | Existing meaningful coverage | Replacement needed? | Action |
|---|---|---|---|---|---|---|
| Meaningless test | `Tests/Financial.Api.Tests/Controllers/ControllerGuardClauseTests.cs`: 34 × `*_Null{Request,Body}_ReturnsBadRequest` (e.g. `AssetPricesController_SetPrice_NullRequest_ReturnsBadRequest`) | The file header says these paths are unreachable via HTTP; `[ApiController]` short-circuits a null body | Any real 400 regression | Endpoint tests (91 `BadRequest` assertions) | No. Delete the dead guards too. | Remove |
| Meaningless test | Same file: 50 `*_Null*_Throws` constructor tests; plus 170 `Constructor_WithNull*` repo-wide | Test `?? throw` boilerplate; DI never passes null | None meaningful | — | No | Merge into one reflection theory per assembly, or Remove |
| Meaningless test | 16 × `Create_Two*_HaveDifferentIds` + `Create_AssignsANonEmptyId` etc. (e.g. `CashFlow.Domain.Tests/Entities/BankTests.cs:Create_TwoBanks_HaveDifferentIds`) | Tests `Guid.NewGuid()` | Nothing | One retained per aggregate root | No | Remove (~27) |
| Meaningless test | ~20 plain echoes, e.g. `CreditTests.cs:Create_WithJcpType_AssignsType`, `Create_WithCouponType_AssignsType`, `CategoryTests.cs:Create_WithIsTitheTrue_AssignsIsTithe` | Getter echo | — | Keep default-rule tests such as `ExpenseTests.cs:Create_ForCreditCardExpense_DefaultsInvoiceDateToFirstOfChargeMonth` | No | Merge into one `Create_AssignsAllFields` per entity |
| Meaningless test | `Shared.Infrastructure.Tests/Sync/SyncStatusTests.cs:SyncState_Should_Have_Exactly_Four_Members` | Enum shape mirror | — | Behaviour tests of sync | No | Remove |
| Meaningless test | `Investment.Domain.Tests/Domain/OpenLotTrackerTests.cs:61` reflection line `GetProperty("GainLoss").Should().BeNull()` | Static structure | — | Rest of the test | No | Remove that line |
| Meaningless test | `Presentation.Tests/Converters/*`: 19 × `ConvertBack_*Throws*` (e.g. `BillStatusToBrushConverterTests.cs:ConvertBack_Always_ThrowsNotImplementedException`) | Framework stub | — | — | No | Remove |
| Meaningless test | `Financial.Web/src/__tests__/App.test.tsx`: "renders_in_light_mode_by_default_regardless_of_os_preference" | Inspects Fluent-generated CSS class text | Breaks on Fluent upgrades, not on regressions | `colourModeStorage.test.ts` | Optionally yes: assert the theme token or attribute on the provider | Replace |
| Meaningless test | `BankOperationsSection.test.tsx`: "no longer renders the old select-based Bank filter"; `BalanceAdjustmentForm.test.tsx`: "renders the create form title"; `CardsGrid.test.tsx`: "does not render the Next Invoice Due Date/Active columns" | Static presence | — | Interaction tests in the same files | No | Remove |
| Meaningless test | `WatchlistEndpointsTests.cs:GetWatchlist_JsonUsesGroupAndNameProperties`, `AssetPriceFetchEndpointsTests.cs:GetAssetPriceFetch_JsonUsesBrokerNameAndPortfolioNameProperties` | Re-test global camelCase per endpoint | — | OpenAPI snapshot pins property names | No | Remove |
| Weak assertion | `InvestmentDataQualityReport.Tests/DataQualityReportFormatterTests.cs:Format_SalesExceedPurchases_NamesHoldingAndShortfall` | `Contain("3")` / `Contain("5")` | A wrong shortfall value | — | — | Rewrite to exact lines |
| Weak assertion | 35 × `*_WhenRepositoryThrowsUnexpectedly_Rethrows` | Pass even with the catch deleted | Failed span not recorded | 2 `*FailedSpan*` tests | — | Rewrite to assert the failed span; merge per service |

### Excessive mocking

No mocking framework is used anywhere, and no test is wiring-only with 4 or more doubles.

| Classification | Test | Mock count | What is mocked | Why this is weak or valid | Recommended action | Proposed retained/replacement test |
|---|---|---:|---|---|---|---|
| Excessive mocking (investigated) | `CashFlow.Application.Tests/Services/CreditCardCalendarSyncServiceTests.cs` | 6 | Calendar provider, connection store, integration service, repository, clock, tracer | Asserts sync state and provider events (outcomes) | Retain; replace the 2 s polling with a deterministic signal | — |
| Excessive mocking (investigated) | `Shared.Infrastructure.Tests/Persistence/DebouncedJsonStorageTests.cs` | 5 | Storage, 2 clocks, logger, tracer | Asserts debounce behaviour | Retain; remove `Task.Delay(200)` at line 137 | — |
| Excessive mocking (investigated) | `CashFlowRepositoryFactoryTests.cs`, `InvestmentRepositoryFactoryTests.cs` | 5 | Remote file client and factory | Mostly wiring plus upload behaviour | Retain; rewrite `Create_WithGoogleDriveProvider_CredentialsPathExists_ResolvesPastFileCheck` (negative-message assertion) | — |
| Excessive mocking (investigated) | `Investment.Application.Tests/Services/SummaryServiceTests.cs` | 5 | Repository, FX, reporting currency, clock, tracer | Asserts computed totals | Retain | — |
| Excessive mocking (pattern) | Web `TransactionsTab.test.tsx` (mocks `recharts`, `useTransactions`, `useOpenLots`), `CreditsTab`, `PriceHistoryTab` | 3 | Own hook + chart | The component's state machine is replaced, so "new_button_calls_show_new_form" never observes the form | Rewrite: render with the real hook over a mocked API client (the same boundary the pages use) | — |

### Incorrect test layer

| Classification | Test | Current layer | Recommended layer | Why current placement is wrong | Action |
|---|---|---|---|---|---|
| Incorrect test layer | `Investment.Infrastructure.Tests/Services/AssetPriceLookupServiceTests.cs` (32), `AssetPriceHistoryServiceTests.cs` (11), `NavigationServiceTests.cs` (~24) | "Infrastructure unit" | Application integration (explicit category) | Application services exercised over real `LocalJsonStorage` | Move to an `Integration/` folder or trait; push pure rules down to `Investment.Application.Tests` |
| Incorrect test layer | `Shared.Infrastructure.Tests/.../UsdBasedExchangeRateProviderTests.cs` (11), `SyncStatusTests` | Shared.Infrastructure | New `Financial.Shared.Abstractions.Tests` | Types live in `Shared.Abstractions` | Move |
| Incorrect test layer | `Api.Tests/Controllers/ControllerGuardClauseTests.cs` (survivors), `DomainExceptionLoggingTests.cs` (8), `DividendsControllerLoggingTests.cs` (2) | Integration project | Unit (trait) | No host; `DefaultHttpContext` only | Retain with `[Trait("Category","Unit")]` |
| Incorrect test layer | Web `useExpenseForm.test.ts` round-up suggestion tests (~12); WPF `ExpenseWorkflowViewModelTests` round-up (~7) | Front-end hook/VM | Domain or Application (single implementation) | Same domain rule implemented twice, with divergent formatting | Move the rule into the Domain/Application layer; keep one interaction test per front end |
| Incorrect test layer | Reserve split ±0.01 tolerance: `src/utils/reserveBucketSplit.ts` (via `useReserva.test.ts`), `ReservaViewModel.cs:70`, `ReserveBucketsViewModel.cs:71`, `ReserveBucketService.cs:118` | 4 implementations | Application only | Rule duplicated, with two different message texts | Move; front ends display the server result |
| Incorrect test layer | `WebPageParser.Tests/*VerificationTests.cs` (9, `Skip=`) | Unit project | Live/manual category | Permanent Skip hides them | Replace Skip with `[Trait("Category","Live")]` and run them with `--filter` |

### Flaky or non-deterministic tests

| Source | Evidence | Risk | Action |
|---|---|---|---|
| **Live network in CI** | `ApiTestFactory.cs:55-62` keeps the real FX chain unless an override is passed. With `ReportingCurrency` defaulting to GBP (`Investments.cs:24`) and the XPI broker in BRL, transaction/credit POSTs reach `FrankfurterExchangeRateProvider`. Example: `TransactionEndpointsTests.cs:AddTransaction_ReturnsOk`. *The live call is inferred from the DI chain; it was not observed on a CI run.* | Outbound CI traffic; up to 11 sequential requests with no timeout (`HttpClient` default 100 s); masked because failures return null | Stabilise: default deterministic FX stub in `ApiTestFactory`; opt in to the real chain |
| Wall clock in production | 17 sites bypass `TimeProvider`: `CardStatementService.cs:111`, `ControleMaeService.cs:51`, `InvestmentSnapshotService.cs:35`, `TitheService.cs:95`, `AssetPriceLookupService.cs:228,248,282`, `DividendService.cs:67`, `PortfolioAssetSummaryService.cs:50`, `XirrCalculationService.cs:18`, `UsdBasedExchangeRateProvider.cs:26`, `FxRateJsonStore.cs:34`, `DisposalRecord.cs:76`, `TaxClassification.cs:73,87,98`, `AssetPriceSnapshot.cs:68` | Midnight/month-end races; date-branching tests | Inject `TimeProvider`, then pin it in tests |
| Wall clock in tests | ~250 backend and 294 WPF `DateTime.Now/Today/UtcNow` uses; 14 web files without fake timers (`useMonthly.test.ts`, `UpcomingIncomePanel.test.tsx`, …). Month-branching tests: `InvestmentAnnualResultServiceTests.cs:GetInvestmentAnnualResultForYear_CurrentYear_FullYearNetChangeUsesCurrentMonthNotDecember`. | Different paths each month; January degenerates | Rewrite with a pinned clock |
| Fixed delays | WPF `Task.Delay(50)` at 10 sites (`InvestmentSnapshotsViewModelTests.cs:345,376,399,404`, `IncomeWorkflowViewModelTests.cs:288`, …), worked around by `ThreadPoolWarmup.cs`; real 2 s backoff in `GoogleRetryPolicyTests` (`RetryPolicy.cs:5,9,38`); polling deadlines in `CreditCardDueDateEventSyncAcceptanceTests.cs:41-52` and `EndToEndTraceTests.cs:110-119` | Flaky on slow runners; slow suite | Await the command `Task`; inject a delay seam |
| Locale | `formatters.ts:5-7` `Intl.NumberFormat(undefined, …)`; WPF `decimal.TryParse` with the current culture (~30 sites, e.g. `ExpenseWorkflowViewModel.cs:484`); `GoogleSheetValueParser.cs:14-15`; `GoogleFinanceParsing.cs:25`. No test pins a culture except `DecimalInputHelperTests`. | On a pt-BR host, tests fail and production misparses (`"9.40"` → 940) | Pin `LANG`/`TZ` in vitest and CI; add explicit pt-BR tests; use invariant culture for machine-format parsing |
| Enlarged `waitFor` timeouts | `DetailPanel.test.tsx:266,358,369` (5000 ms), `MonthlyPage.test.tsx:1002,1150` (3000 ms) | Hide slow or racy async | Investigate; wait on state |
| Shared mutable static fixtures | 47 `private static readonly` entity fixtures (e.g. `ExpenseServiceTests.cs:17`) | Latent only: no mutation observed | Convert to factory methods when touched |
| Live-data hazard | `smoke-test.mjs:12` default `SMOKE_APP_URL=http://localhost:5173`. The Vite proxy reaches the dev API, whose Development config points at the real `data/` files. | A local `npm run smoke-test` writes 3 seed expenses into live data | Require `SMOKE_APP_URL` (no default), or refuse to run unless the API reports a test data file |

---

## 7. Prioritized Test and Gate Plan

| Priority | Area | Classification | Evidence | Risk / bug prevented | Recommended test level or pipeline stage | Proposed scenario or removal rationale | Suggested action |
|---|---|---|---|---|---|---|---|
| P0 | CashFlow import tool | Missing coverage | `Tools/CashFlowSpreadsheetImport/Program.cs:33-36, 199-236` | A full rebuild silently drops Transfers, BalanceAdjustments and Tithe carry-forwards, and the default output is the live file | Unit/integration over a temp file | Seed a file with app-native records → run the rebuild → assert they survive (or that the tool refuses) | Confirm intent; test + fix; require an explicit output path |
| P1 | CashFlow repository | Missing coverage | `CashFlowJsonRepository.cs:26`, `ItemCollection` | "Collection was modified" on a GET during a POST | Integration (repository) | See charter 1 | Add test, then snapshot reads |
| P1 | Reserve split | Missing coverage + mirror test | `ReserveService.cs:345-349`; `IncomeServiceTests.cs` ~360/463 | Split total ≠ base by ±0.01 | Unit (Application) | See charter 2 | Add test; rewrite the mirror tests with literals |
| P1 | Round-up parity | Incorrect test layer | `useExpenseForm.ts:22-33` vs `ExpenseWorkflowViewModel.cs:482-490` | React `0.60` vs WPF `0.6` | Domain unit | See charter 3 | Move the rule to Domain; delete the per-front-end duplicates |
| P1 | Api.Tests determinism | Flaky / non-deterministic test | `ApiTestFactory.cs:55-62` | Live network, stalls | CI (PR) | Default FX stub | Rewrite the factory |
| P1 | SPA fallback | Missing coverage | `Program.cs:142` | Unknown `/api/*` returns 200 HTML | Integration (host with webroot) | See charter 4 | Add test; exclude `/api` from the fallback |
| P1 | Contract gate | Gate bypass risk | `OpenApiContractTests.cs:41-46` | Snapshot silently rewritten in CI | PR | Fail when `CI` is set | Fix |
| P1 | Context isolation | Missing contract/architecture gate | `Architecture.Tests` (no cross-context rule) | Investment ↔ CashFlow coupling | PR | `NotContain("Financial.CashFlow")` and the reverse, for all layers | Add |
| P1 | `todayIsoDate` | Weak assertion | `formatters.ts:76-78`; mirror tests | Wrong default date 00:00–01:00 BST | Web unit with `setSystemTime` | See charter 5 | Fix to a local date; rewrite the tests |
| P1 | Exception mapping | Missing coverage | `DomainExceptionMappingMiddleware.cs:43-62` | Server faults shown as 400; storage outage 500 not 503; 500 body leak | Integration (Production env host) | Theory over all mapped types + `TransientStorageException` + Production 500 body | Add |
| P1 | GoogleDriveClient | Coverage-reporting gap | `Financial.Integrations.GoogleDrive` 21.1% line / 10.7% branch; 0 test references | Production save/load regressions undetected | Integration with fake `HttpMessageHandler` | Upload/download, 404, retry on 429/5xx | Add a thin contract test, or exclude explicitly like GoogleSheets and document it (uncertain: depends on how much logic it holds) |
| P2 | Coverage gate | Coverage metric mismatch / Missing regression ratchet | `action.yml` | Untested branches merge | PR | Diff branch coverage + ratchet | Add (observe first) |
| P2 | Unsaved changes | Missing coverage (feature absent) | No `beforeunload`/`useBlocker`/`isDirty` in `Financial.Web/src`; `*FormDialog.tsx` `onOpenChange` → `onCancel` | Silent loss of typed input | Web component + WPF VM | Dirty dialog + Escape → confirm prompt | Implement, then test (UI rules `docs/ui/forms-data-and-visualisations.md:175`) |
| P2 | Payments-due rollover | Missing coverage | `PaymentsDueService.cs:116-117` | Bill due on the 2nd hidden on the 29th | Unit | Pinned clock on the 29th; `DueDay=2` | Confirm with the P42 PRD; add test |
| P2 | Low-value tests | Meaningless / duplicate test | §6 | Noise, slow refactors | n/a | — | Remove/merge ~700 |
| P2 | Wall clock | Flaky / non-deterministic test | 17 production sites | Midnight/month races | n/a | — | Inject `TimeProvider`; rewrite tests |
| P2 | Culture | Missing coverage | `GoogleSheetValueParser.cs:14-15`, WPF parsing | Misparsed amounts on pt-BR | Unit | `CultureInfo.CurrentCulture = pt-BR`; parse `"12.5"` → 12.5 | Add; pin locale in CI |
| P2 | Hygiene gate | Missing test-hygiene gate | eslint config; no analyzers | Re-introduced flakiness | PR (diff-only) | — | Add |
| P2 | Frankfurter resilience | Missing coverage | `FrankfurterExchangeRateProvider.cs` (no timeout or cancellation) | 11× amplification; long stalls | Unit | Fake handler that hangs → bounded time; call count on 5xx | Add test + timeout |
| P3 | Mutation | Missing test-strength gate | none | Weak assertions in the domain | Nightly | — | Stryker.NET report-only |
| P3 | Ghost directories | Configuration / CI gap | 7 untracked `Tests/*` folders | Misleading inventory | n/a | — | Delete locally |
| P3 | Architecture double run | Configuration / CI gap | `build.yml` wpf "Run tests" | Minutes of CI time | — | — | Remove from the `wpf` job |
| P3 | Web coverage scope | Coverage-reporting gap | `vite.config.ts` exclude | Helpers inflate coverage | — | — | Exclude `src/test/**` and `src/test-utils/**` |

### Top five test charters

**1. CashFlow reads survive a concurrent write**

| Field | Detail |
|---|---|
| Behaviour | A read of expenses (or a balance computation) never throws while a save mutates the collection. |
| Given | A `CashFlowJsonRepository` over a `ControllableJsonStorage` that blocks mid-write, with 1,000 expenses. |
| When | `ApplyAndSaveAsync` adds or removes an expense while another task enumerates `GetExpenses()` / runs `BankService.GetBankBalancesByMonth`. |
| Then | The enumeration completes without `InvalidOperationException` and sees either the before or the after state. |
| Why this layer | Integration over the repository and a fake storage: the defect lives in repository/collection sharing. Unit-level stubs hide it. |
| Bug it would catch | Intermittent 500 on GET during a POST, as seen in the web app with background `CreditCardCalendarSyncService`. |
| Existing overlap | Only the write-gate test exists. Investment has the equivalent (`AssetTests.cs:SetPrice_WhilePriceSnapshotsIsBeingEnumerated_DoesNotDisturbTheEnumeration`). |
| Unique value | The first CashFlow read/write concurrency test. |
| Dependencies | `ControllableJsonStorage` (`TestUtilities`). |

**2. A reserve split conserves the total**

| Field | Detail |
|---|---|
| Behaviour | The sum of the split movements equals the split base exactly. |
| Given | Active buckets at 33.33% / 33.33% / 33.34% and a net income of £100.01. |
| When | `IncomeService.AddIncomeAsync` with the split enabled (or `CreateSplitMovements` directly). |
| Then | The movement amounts are literal values (e.g. 33.33 / 33.33 / 33.35) and their sum is 100.01. |
| Why this layer | Application unit: a pure calculation over domain rounding. |
| Bug it would catch | A penny lost or gained per split, so reserve balances drift from bank balances. |
| Existing overlap | `ReserveBucketTests.cs:CalculateSplitAmount_OnAnExactMidpoint_RoundsAwayFromZeroNotToEven` (per-bucket only). `IncomeServiceTests` mirrors production. |
| Unique value | The only conservation assertion. |
| Dependencies | `StubCashFlowRepository`. |

**3. One round-up suggestion rule, one format**

| Field | Detail |
|---|---|
| Behaviour | The suggested round-up for a value is `ceil(v) − v` to 2 dp, rendered identically by both front ends. |
| Given | Values 9.40, 10.00, 0.01, 9.995. |
| When | The domain/application suggestion is requested. |
| Then | 0.60, 0.00, 0.99, 0.01 (pinned literals), as a decimal value both clients format with the shared money formatter. |
| Why this layer | Domain unit: it is a domain rule. Front ends keep one interaction test each ("keeps recalculating as typed"). |
| Bug it would catch | Parity drift: React `0.60` vs WPF `0.6` today. |
| Existing overlap | `useExpenseForm.test.ts` and `ExpenseWorkflowViewModelTests` each pin their own variant. Those value tests should be removed after the move. |
| Unique value | A single source of truth. |
| Dependencies | none. |

**4. Unknown API routes are not swallowed by the SPA fallback**

| Field | Detail |
|---|---|
| Behaviour | `GET /api/v1/financial/does-not-exist` returns 404 (JSON or empty), never 200 `text/html`. |
| Given | An `ApiTestFactory` host with a temp `wwwroot/index.html`. |
| When | Requesting an unmapped `/api/...` path. |
| Then | Status 404, and the content type is not `text/html`. |
| Why this layer | Integration: routing and the static-file fallback only exist in the real host pipeline. |
| Bug it would catch | A renamed endpoint makes the SPA receive HTML and show `SyntaxError: Unexpected token '<'`. This is the documented `API_BASE_URL` incident class. |
| Existing overlap | `SwaggerEndpointsTests.cs:GetSwaggerUI_NotInDevelopment_ReturnsNotFound` passes only because no `wwwroot` exists in the test host. |
| Unique value | The first test with production-like static hosting. |
| Dependencies | Temp directory. |

**5. The default date is the user's local date**

| Field | Detail |
|---|---|
| Behaviour | New-entry forms default to the local calendar date. |
| Given | `vi.setSystemTime('2026-07-01T00:30:00+01:00')` and `TZ=Europe/London`. |
| When | `todayIsoDate()` is called and `UkExpensePromptDialog` / `useBalanceAdjustmentForm` open. |
| Then | `'2026-07-01'` (literal), not `'2026-06-30'`. |
| Why this layer | Web unit: pure function plus one hook. |
| Bug it would catch | Entries made between 00:00 and 01:00 BST land on the previous day, or the previous month on the 1st. |
| Existing overlap | Tests compute the expectation with the same UTC expression (mirror). |
| Unique value | Pins time-zone behaviour. |
| Dependencies | Fake timers, `TZ`. |

### CI gate rollout sequence

1. **Immediate P0/P1 enforcement fixes**
   - Snapshot-flag guard (`CI` set → fail).
   - CashFlow ↔ Investment architecture rule.
   - Default FX stub in `ApiTestFactory`.
   - Consider `enforce_admins`.
2. **Fast PR gates**
   - Diff branch/line coverage, advisory for 2–4 weeks.
   - Hygiene script, diff-only.
   - `eslint-plugin-vitest` `no-focused-tests`.
   - Exclusion-change label check.
3. **Main or nightly**
   - Stryker.NET on `Financial.CashFlow.Domain` and `Financial.Investment.Domain`, report-only.
   - AC-traceability report.
   - Optional run of the `Category=Live` verifications.
4. **Release pipeline**
   - None exists. The Docker build is not exercised by CI beyond `dotnet publish` in `smoke`.
   - Recommend a `docker build` on `main` (not a test gate; the risk is deploy breakage).
5. **Baseline, ratchet and threshold tightening**
   - Commit a baseline from run 36844680000 (line and branch per job).
   - Ratchet with a 0.5-point tolerance (suggested starting point).
   - Once diff and ratchet have been stable for a month, keep 90% line only as a tripwire.
6. **Remove or consolidate before adding coverage targets**
   - Delete the 34 unreachable guard tests together with the dead guards.
   - Collapse the constructor, rethrow, Id and duplicate tests.
   - Then take the new baseline, so the ratchet does not lock in noise.

---

## 8. Coverage Commands and Next Steps

### Discovered commands

| Purpose | Command | Source |
|---|---|---|
| Unit + integration (.NET, CI-equivalent backend) | `dotnet build --configuration Release` then `dotnet test --configuration Release --no-build --verbosity normal --filter "FullyQualifiedName!~Financial.Presentation.Tests" --settings coverlet.runsettings --results-directory TestResults` | `build.yml` backend |
| WPF | `dotnet test Tests/Financial.Presentation.Tests --configuration Release --settings coverlet.runsettings --results-directory TestResults` | `build.yml` wpf |
| Architecture | `dotnet test Tests/Financial.Architecture.Tests` | `build.yml` wpf |
| Single project | `dotnet test Tests/Financial.CashFlow.Domain.Tests` | CLAUDE.md |
| Web unit + integration | `cd Financial.Web && npm test` | `package.json` |
| Web coverage (CI) | `npm run test:coverage` | `package.json`, `build.yml` |
| Coverage report (.NET) | `reportgenerator "-reports:TestResults/**/coverage.cobertura.xml" "-targetdir:CoverageReport" "-reporttypes:MarkdownSummaryGithub;JsonSummary;Html" "-assemblyfilters:+*;-Financial.Presentation.App"` | `build.yml` |
| E2E | `cd Financial.Web && SMOKE_APP_URL=http://localhost:<test-port> npm run smoke-test`, with the published API on test JSON (see the `smoke` job) | `build.yml` smoke |
| Contract snapshot update | `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` | CLAUDE.md |
| Mutation testing | **Not identified from repository evidence.** | — |

### Recommended commands (not in the repository)

- `dotnet stryker --project Financial.CashFlow.Domain.csproj --test-project Tests/Financial.CashFlow.Domain.Tests` (report-only, nightly).
- `diff-cover TestResults/**/coverage.cobertura.xml --compare-branch origin/main --fail-under <baseline>`.

### Local dependencies

| Dependency | Notes |
|---|---|
| .NET 10 SDK | Windows for the WPF projects |
| Node 24 | `npm install` |
| Chromium | `npx playwright install chromium`, for the smoke test |
| Database, containers, emulators | None needed |
| Seed data | `Tests/Financial.Api.Tests/TestData/data.test.json`, `data-cashflow.test.json` |
| Environment variables (smoke) | `Investment__Repository__Provider=LocalJson`, `Investment__DataJsonFile`, `CashFlow__Repository__Provider`, `CashFlow__DataJsonFile`, `ASPNETCORE_URLS`, `SMOKE_APP_URL` |
| Credentials | None for CI. Google credentials only for the skipped live checks or the GoogleDrive provider. |
| Network | Api.Tests currently reaches `api.frankfurter.app` (see §6) |

**Warning:** never run `npm run smoke-test` without `SMOKE_APP_URL` pointed at a test-data host, and never smoke-test against port 8080 while the live Docker app is running (CLAUDE.md).

### Smallest high-value sequence

1. **P0.** Confirm intent for `CashFlowSpreadsheetImport` carry-over and the default output path. Add a preservation test or remove the live-file default.
2. **P1, one PR each:**
   1. Default FX stub in `ApiTestFactory`.
   2. CI guard on `UPDATE_OPENAPI_SNAPSHOT`.
   3. Context-isolation architecture rule.
3. **P1, behavioural tests with their fixes:** charters 1–5, then the exception-mapping theory and Production 500 body.
4. **P2, removal and consolidation, before any new coverage target:**
   1. Delete `ControllerGuardClauseTests` null-body tests and the dead guards.
   2. Collapse constructor null-guards into a reflection theory.
   3. Merge Id, `CashFlowDataTests`, ReferenceConverter and migrator tests.
   4. Parameterize the web dialog and tab contracts.
   5. Remove `ConvertBack` throws tests.
   6. Delete the 7 ghost folders.
5. **P2, determinism:**
   1. Inject `TimeProvider` at the 17 production sites.
   2. Migrate the wall-clock tests.
   3. Replace WPF `Task.Delay` with awaited command tasks.
   4. Pin `LANG`/`TZ` in vitest setup and CI, and add pt-BR parse tests.
6. **P2, gates:**
   1. Take a new baseline after step 4.
   2. Add the ratchet (blocking) and diff branch coverage (advisory).
   3. Make diff coverage blocking after 2–4 weeks of observed false-positive rate.
7. **P3:** Stryker.NET nightly on the Domain projects. Set a break threshold only after two or three runs establish the baseline and equivalent-mutant noise.

### Measure before making anything merge-blocking

- Diff-coverage false-positive rate on the last ~20 merged PRs.
- Stryker runtime and score per Domain project.
- Ratchet tolerance vs normal run-to-run variance. Coverage should be deterministic once wall-clock and live-network tests are fixed, which is another reason to do step 5 first.

---

### Evidence notes and verification

- **Verified directly by the main session:**
  - Branch protection (API).
  - CI workflow and gate action contents.
  - Coverage numbers from the run 36844680000 artifacts.
  - `OpenApiContractTests.cs:41-46`.
  - `ApiTestFactory.cs:55-62`.
  - `Program.cs:142`.
  - `DomainExceptionMappingMiddleware.cs` catch list.
  - `ReserveService.cs:345-349`.
  - `PaymentsDueService.cs:116-117`.
  - `CashFlowJsonRepository.cs:26`.
  - `useExpenseForm.ts:22-33` vs `ExpenseWorkflowViewModel.cs:482-490`.
  - `formatters.ts:5-7, 76-78`.
  - `smoke-test.mjs:12`.
  - No dirty-state guard in Web.
  - 3 of 7 ghost directories untracked.
  - Absence of a cross-context architecture rule.
  - 0 test references to `GoogleDriveClient`.
  - 170 `Constructor_WithNull` occurrences.
- **From sub-agent static inspection (grep-based, not individually re-verified):** per-project counts, the duplicate and low-value lists, wall-clock and `Task.Delay` counts, the culture-parsing sites, the import-tool carry-over list, and the live Frankfurter call path. The DI chain was verified; the actual outbound call during CI was not observed.
- **Uncertain:**
  - Whether the `CashFlowSpreadsheetImport` carry-over omissions and the payments-due rollover are intended. Resolve with the P42 PRD and the import tool's design notes.
  - Whether `GoogleDriveClient` holds enough logic to merit tests rather than an explicit exclusion.
