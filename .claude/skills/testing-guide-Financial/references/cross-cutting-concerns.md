> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Cross-Cutting Concerns

One subsection per recurring category from the fundamentals, each with this project's concrete
pattern or an explicit "not applicable".

## 1. Time, clock and concurrency-dependent code — covered

- **Where**: `TimeProvider` injected into `PaymentsDueService(ICashFlowRepository, ITelemetryTracer, ILogger<PaymentsDueService>, TimeProvider? timeProvider = null, TimeZoneInfo? timeZone = null)`,
  `CategorySummaryService`, `HistoricAverageService`, `IncomeSummaryService`,
  `InvestmentAnnualResultService`, and `DebouncedJsonStorage(IJsonStorage inner, TimeSpan debounceWindow, TimeProvider? timeProvider = null, …)`;
  registered in `CashFlowApplicationServiceCollectionExtensions`. Web: the banner auto-dismiss
  timer in `usePaymentsDue`; WPF: `TodayInfoTracker`.
- **Unit**: `new FakeTimeProvider(new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero))`
  (`Tests/Financial.TestUtilities/FakeTimeProvider.cs`, fixed `GetUtcNow`) — assert the exact
  fire boundary (due in 5 days included, 6 excluded; clamped `DueDay` in February).
- **Integration**: `ObservableFakeClock` (wraps `Microsoft.Extensions.Time.Testing.FakeTimeProvider`,
  counts armed timers) drives `DebouncedJsonStorage` — write during window resets it, write
  during in-flight save starts a follow-up, retries on `TransientStorageException`
  (`DebouncedJsonStorageTests`). Host level: `ApiEndpointTests(timeProvider: …)` replaces
  `TimeProvider` via `RemoveAll<TimeProvider>()`.
- **Web**: `vi.useFakeTimers({ shouldAdvanceTime: true })` + `vi.advanceTimersByTimeAsync(PAYMENT_DUE_BANNER_DISMISS_MS)`
  (`usePaymentsDue.test.ts`); `vi.setSystemTime` for `currentYearMonth` in `formatters.test.ts`.
- **Concurrency**: `ThreadPoolWarmup.cs` for WPF fire-and-forget; `DebouncedJsonStorage`'s
  in-flight/follow-up cycle is the one real concurrent-writer scenario and is tested with
  `ControllableJsonStorage.HoldWritesUntilReleased()`. Never `Task.Delay`/`Thread.Sleep` in a
  test; poll a monotonic counter.

## 2. UI state matrix — covered (partially asserted; mandatory for new work)

- **Standard**: `docs/rules/ui.md` (initial, loading, empty, validation, server-error,
  saving/progress, success, disabled, unsaved-changes) and `docs/ui/review-checklist.md`
  (grids: "loading, empty, and error states"; forms: "Saving state prevents duplicate
  actions", "Failed saves preserve entered data", "Unsaved changes are protected").
- **Web, Unit/component**: `ErrorState.test.tsx` (`role="alert"`, retry present/absent),
  `LoadingState.test.tsx`, `TransferForm.test.tsx` (`shows Saving... and disables the button while isSaving`,
  error under the named field, general error banner), `PaymentDueBanner.test.tsx` (null/empty
  → nothing rendered).
- **Web, Integration (page)**: `MonthlyPage.test.tsx` (`shows a loading state before data arrives`,
  `shows an error state with retry when the fetch fails`, `closes an open create form when switching tabs away and back`,
  `does not unmark a paid statement when the user cancels the confirmation`), `BanksPage.test.tsx`.
- **WPF, Unit**: ViewModel `IsSaving`/`IsLoading`/error properties and `*FormValidation`
  classes; parity with the React matrix is the acceptance bar (`docs/rules/ui.md` §1).
- **E2E**: no full state-transition journey yet — add with the smoke failure journey
  (`negative-path-testing.md`).
- **Rule for new views**: one test per applicable state, named for the state, before the
  view is "done"; missing states are defects.

## 3. Accessibility — covered at Unit; E2E keyboard journey missing

- **Standard**: `docs/ui/accessibility.md` (WCAG 2.2 AA; keyboard operable; visible focus;
  accessible names for icon-only controls; no colour-only status; WPF `AutomationProperties`).
- **Web, Unit/component**: query by role and accessible name (`getByRole('navigation', { name: 'Main' })`,
  `getByRole('button', { name: 'Collapse sidebar' })`, `getByLabelText(/Due today.*urgent/)`);
  keyboard operability (`PaymentDueBanner.test.tsx: close_button_is_keyboard_operable` —
  `await user.tab(); await user.keyboard('{Enter}')`); `toHaveFocus` used once. Colour-only
  meaning is refuted by asserting the text/icon label alongside the tier.
- **WPF**: no automated `AutomationProperties` assertions today; the XAML-parsing pattern in
  `../artifacts/wpf-views-and-xaml.md` can assert `AutomationProperties.Name` presence per
  icon-only button (`Financial.App` uses `AutomationProperties.Name` 178 times, `HelpText` 35,
  `LiveSetting` 12). Those names are what a future UI-automation harness would drive.
- **E2E**: no keyboard-only completion of a critical workflow — required by the fundamentals;
  add to the smoke script (Tab/Enter through "add expense") alongside the failure journey.
- jsdom limits: focus order and visible focus rings are not trustworthy — assert handlers and
  names in tests, check focus visually per `docs/rules/ui.md` "Completion requirement".

## 4. Contract / generated-type drift — covered

- Snapshot: `Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json` asserted by
  `OpenApiContractTests.OpenApiDocument_MatchesTheCommittedSnapshot` (Integration, real host);
  numeric hygiene pinned by `OpenApiDocument_NumericProperties_DoNotAdvertiseAStringFallback`.
- Generated client types: `Financial.Web/src/api/generated/openapi.ts` via
  `npm run generate-api-types`; freshness asserted by `src/api/generated/__tests__/openapiFreshness.test.ts`;
  `src/api/types.ts` is aliases over it.
- Typecheck step: `tsc -b` in `npm run build` (CI `web` job) — fails at every call site of a
  renamed field.
- Runtime check: the smoke journey (E2E) renders a computed value seeded through the API.
- Workflow and commands: `../artifacts/api-contract-snapshot.md`. Treat "regenerate +
  typecheck" as a required step of any DTO/endpoint change, never cleanup.

## 5. Fitness-function tests — covered

- `Tests/Financial.Architecture.Tests` (7 rule classes, 14 tests): `CashFlowDependencyRuleTests`,
  `InvestmentDependencyRuleTests`, `PresentationDependencyRuleTests`,
  `SharedAbstractionsDependencyRuleTests`, `SharedInfrastructureDependencyRuleTests`,
  `SharedInfrastructureIsolationRuleTests`, `ObservabilityIsolationRuleTests` — reflection over
  the real compiled assemblies through `Infrastructure/ProjectAssembly.cs`.
- Web structural agreement: `src/navigation/__tests__/routes.test.ts` (sidebar ↔ router).
- WPF: `ExpenseGridBindingTests` (XAML ↔ DTO) is a structural rule over the source tree.
- Layer: Integration. Recipe and the "make it fail once" rule: `../artifacts/architecture-rule-tests.md`.

## 6. Test-data contract tests — covered

- `data/data-investment.example.json` and `data/data-cashflow.example.json` linked into the
  Infrastructure test projects and loaded through the real `InvestmentLoader` /
  `CashFlowLoader` + serializer (`ExampleDataFileTests` ×2).
- Seeds: `Tests/Financial.TestUtilities/TestData/data.test.json` (`TestDataPaths.DataJsonFile`),
  `Tests/Financial.Api.Tests/TestData/data-cashflow.test.json` (smoke seed),
  `ApiTestFactory.SeededBanksJson` (mirrors the migration tool's output; ids reused by every
  endpoint test and by `smoke-test.mjs`).
- Layer: Integration. Recipe: `../artifacts/example-and-seed-data.md`.

## 7. Same-repo frontend faking its own backend — sanctioned exception, documented

`Financial.Web`'s hook/component/page tests replace `financialApiClient` with `vi.mock`; its
client tests replace `fetch`. This is the fundamentals' exception for a separate deployable
reached over HTTP, not a boundary violation — see `mock-health-rules.md`. `Financial.App`
does not qualify (in-process composition).
