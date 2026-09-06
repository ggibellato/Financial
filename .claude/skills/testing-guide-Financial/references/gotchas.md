> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Stack-Specific Gotchas

## .NET / xUnit 2.9.3 / ASP.NET Core 10

- **`--filter` on traits needs both key and value**: `dotnet test --filter "AC=P42-…"` or
  `"AC~P42-F01"`; a bare `--filter AC` is a `FullyQualifiedName` contains-match, not a trait
  filter. Dry-run with `--list-tests` before wiring a filter into a script.
- **`UPDATE_OPENAPI_SNAPSHOT` left set rewrites the snapshot on every later run** instead of
  checking it — in PowerShell always `Remove-Item Env:\UPDATE_OPENAPI_SNAPSHOT` afterwards
  (bash's one-shot prefix has no such trap).
- **`WebApplicationFactory` configuration ordering**: options `AddObservability` binds inline
  while `Program.cs` executes, so `ConfigureAppConfiguration` callbacks are too late — use
  `builder.UseSetting("Observability:Enabled", "true")` (`ObservabilityBackendUnreachableTests`).
  `ConfigureTestServices` overrides must be chained before the first `CreateClient()`.
- **One host per test class instance is the isolation model**: `ApiEndpointTests` builds a
  fresh `ApiTestFactory` and temp files per test (xUnit constructs a new instance per test).
  Building a second factory inside a test is fine (`await using var factory = new ApiTestFactory(...)`)
  — as a static field it leaks temp files and state.
- **Temp file cleanup after a failed assertion**: put `File.Delete` in `finally` or in
  `Dispose`; `ApiTestFactory.Dispose(bool)` deletes its own files, `LocalJsonStorageTests`
  uses `try/finally`.
- **`DebouncedJsonStorage` queues the cycle onto the thread pool**, so the timer is not armed
  when `WriteAsync` returns. Advancing `FakeTimeProvider` before that moves the clock past a
  timer that does not exist yet and the test hangs. Wait on `ObservableFakeClock.TimersArmed`
  (a monotonic counter) before `Advance` — the reason that class exists.
- **2-core CI runners starve fire-and-forget `Task.Run` tests**: `ThreadPoolWarmup.cs`
  (`[ModuleInitializer]`) raises `ThreadPool.SetMinThreads` to 32 in `Financial.Presentation.Tests`.
  Do not add `Thread.Sleep`/`Task.Delay` to "fix" a timeout; wait on a condition.
- **`Financial.Architecture.Tests` and `Financial.Presentation.Tests` are `net10.0-windows`** —
  they do not run on the Ubuntu jobs; `backend` filters `FullyQualifiedName!~Financial.Presentation.Tests`
  and the `wpf` job runs both.
- **`InternalsVisibleTo`**: `Financial.Api` exposes internals to `Financial.Api.Tests`,
  `Financial.App` to `Financial.Presentation.Tests` — a new test project name must be added to
  the csproj or internal classes (`ApiTestFactory`, middleware) will not compile.
- **`[MemberData("Name")]` strings break silently on rename** — always `nameof(...)`
  (`SharedInfrastructureIsolationRuleTests.IsolatedProjects`).
- **`[InlineData(null)]` for a `string?` parameter** works; for value types use `MemberData`.
- **FluentAssertions stops at the first failure** — wrap multi-property checks in
  `using (new AssertionScope())` (`AssetTests.Create_SetsProperties`).
- **`ClosedXML` `XLWorkbook` holds unmanaged resources** — dispose it (`IDisposable` on the test
  class in `MonthlyExpenseSheetImporterTests`).
- **Never log/assert a domain message**: messages embed financial values
  (`"exceeds Ariana's balance of 654.27"`). Assert `Message.Contains(nameof(ExceptionType))` and
  `NotContain("654.27")`, per `docs/rules/implementation.md`.
- **Fake `HttpMessageHandler` ignores the URL by default** — if the provider's URL/query has
  branching, capture the `HttpRequestMessage` in the responder and assert it.
- **`UnsupportedAssetClassException` first-chance noise** in the debugger for two unpriceable
  holdings is known and handled (maps to 422); not a test failure.
- **WPF `Run.Text` bindings** default to TwoWay and crash on private setters — never
  `<Run Text="{Binding …}"/>`; use `TextBlock` + `StringFormat`. A regex test over `Views/**`
  is the cheap guard.
- **Select through `TreeNodeViewModel.IsSelected`, not `SelectedNode = …`** — a directly
  assigned selection bypassed command re-evaluation and shipped a permanently disabled button
  through 743 green tests.
- **`rg` is not on PATH on this machine** (the `rtk` hook reports "Binary 'rg' not found") —
  audit commands in this guide use `grep -rnE`.

## Financial.Web / Vitest 4.1 / RTL 16 / Fluent UI v9

- **Fluent UI v9 + Vitest**: `@fluentui/react-components` imports `tabster` through a CJS
  export-getter loop Vitest's module runner cannot analyse; `src/setupTests.ts` re-mocks the
  two Fluent entry points to the real packages via `createRequire`. Do not add another
  `vi.mock('@fluentui/…')` in a test file, and do not "fix" the failing import by stubbing the
  package.
- **Fluent components need a `FluentProvider` ancestor** for real styles and `useId` label
  association — render through `src/test/renderWithFluent.tsx` or wrap in
  `<FluentProvider theme={webLightTheme}>`; a bare `render` yields fallback styles and
  mismatched labels.
- **Focus management (tabster) is unreliable in jsdom** — assert keyboard handlers fire
  (`user.tab(); user.keyboard('{Enter}')`) and accessible names exist; verify visible focus in
  the browser per `docs/rules/ui.md`.
- **`ResizeObserver` and `matchMedia` are shimmed once in `setupTests.ts`** (recharts and
  Fluent need them) — a second shim in a test file conflicts.
- **`vi.mock` is hoisted above imports; use `vi.hoisted` for the mocks it references**
  (`const { getBanksMock } = vi.hoisted(() => ({ getBanksMock: vi.fn<FinancialApiClient['getBanks']>() }))`).
  Importing inside `vi.hoisted` is discouraged.
- **`--tagsFilter` requires tags declared in `test.tags`** (`strictTags` defaults to true;
  undeclared → "The Vitest config does't define any tags"). Tag names cannot contain spaces or
  `! ( ) * | &`. That is why AC ids live in the `it` title and are filtered with `-t`.
- **`mockReset()` vs `mockClear()`**: `mockReset` clears the implementation too; the suite uses
  `mockReset()` in `beforeEach` then re-arms `mockResolvedValue`.
- **`findBy*`/`waitFor` for anything after a resolved promise**; re-read `result.current` after
  each `waitFor` in `renderHook` tests — a destructured value is a stale snapshot.
- **Fake timers with RTL**: `vi.useFakeTimers({ shouldAdvanceTime: true })` keeps `waitFor`
  polling alive while `vi.advanceTimersByTimeAsync` drives the timeout; restore in `afterEach`
  (`usePaymentsDue.test.ts`).
- **`localStorage`/`sessionStorage` persist across tests in one file** — clear in `afterEach`
  (`App.test.tsx`, `ColourModeContext.test.tsx`).
- **`API_BASE_URL` empty is a broken deployment, not a fallback** — `config.ts` does `?? ''`
  only because `vite.config.ts` `define`s the real value; an empty base makes API calls hit the
  SPA fallback and receive HTML. Never assert the empty string as valid.
- **`openapiFreshness.test.ts` compares ignoring CRLF** — on Windows checkouts the committed
  file is CRLF; a raw byte comparison would fail for the wrong reason. Keep that normalisation.
- **`vitest run` does not type-check** — run `npm run build` (`tsc -b`) after each change; a
  field rename passes vitest and fails the Docker build (`feedback_typescript_build_validation`).
- **Vitest 4.1's `-t` matches the full name** (`describe > it` joined with ` > `) — escape the
  `[` in `-t "\[AC P42-F02"`.

## E2E / CI

- **Port 8080 is the live Docker app** — smoke-test locally on another port after `netstat`.
- **`--no-launch-profile` drops `ASPNETCORE_ENVIRONMENT=Development`** and CORS vanishes,
  presenting as a 503-looking failure in the SPA — use the published-build recipe in
  `e2e-environment.md`.
- **Data files load once at startup** — a test that edits the JSON on disk after the host
  started proves nothing; restart the host / build a new factory.
- **Restarting the container never runs a migration** — verify migrations against a temp copy
  with the tool itself.
