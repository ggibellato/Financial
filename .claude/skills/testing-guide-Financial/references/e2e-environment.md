> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# E2E Environment

Decided in Phase 3 (2026-09-06): **the CI `web-e2e` job is the E2E environment.** It is the only
place more than one deployed process runs — the published `Financial.Api.dll` (serving the
built SPA from `wwwroot`) and Playwright's Chromium — over real HTTP against real JSON files.

## What is real

| Piece | In the web-e2e job |
|---|---|
| API | `dotnet publish Financial.Api/Financial.Api.csproj --configuration Release --output publish`, then `dotnet Financial.Api.dll` with `ASPNETCORE_URLS=http://localhost:8080` |
| SPA | `npm run build` with `.env` = `API_BASE_URL=/api/v1/financial`, copied into `publish/wwwroot/` |
| Data | `Tests/Financial.Api.Tests/TestData/data.test.json` → `/tmp/data.smoke-test.json`; `data-cashflow.test.json` → `/tmp/data-cashflow.smoke-test.json`; `Investment__Repository__Provider=LocalJson`, `CashFlow__Repository__Provider=LocalJson` |
| Readiness | `curl -sf http://localhost:8080/api/v1/financial/health` polled up to 30 s |
| Driver | `npm run smoke-test` in `Financial.Web` — `playwright test --grep @smoke` (`@playwright/test`, Chromium headless, `SMOKE_APP_URL=http://localhost:8080`); on failure the job uploads `playwright-report/` and `test-results/` (trace, screenshot, video) |
| Providers | Frankfurter/Yahoo/Google are simply unreachable or unused; nothing is faked — the seeded data avoids live prices and observability is disabled |

Trigger rules live in `.github/scripts/detect-changes.sh`: the job runs when either side of
the HTTP boundary changes (backend, web, `types.ts`, the OpenAPI snapshot) and on every push
to `main`. It `needs: [changes, backend, web]` and tolerates them being skipped.

## Running it locally

Same steps as CI, on a port that is not the live Docker app's 8080 — check first
(`feedback_never_smoke_test_against_live_port`):

```powershell
netstat -ano | Select-String ":8080"          # must be empty, or pick another port
cd Financial.Web; "API_BASE_URL=/api/v1/financial" | Set-Content .env; npm run build; cd ..
dotnet publish Financial.Api/Financial.Api.csproj -c Release -o publish
Copy-Item Financial.Web/dist/* publish/wwwroot -Recurse -Force
Copy-Item Tests/Financial.Api.Tests/TestData/data.test.json $env:TEMP/data.smoke-test.json
Copy-Item Tests/Financial.Api.Tests/TestData/data-cashflow.test.json $env:TEMP/data-cashflow.smoke-test.json
$env:Investment__Repository__Provider='LocalJson'; $env:Investment__DataJsonFile="$env:TEMP/data.smoke-test.json"
$env:CashFlow__Repository__Provider='LocalJson';   $env:CashFlow__DataJsonFile="$env:TEMP/data-cashflow.smoke-test.json"
$env:ASPNETCORE_URLS='http://localhost:8081'; $env:SMOKE_APP_URL='http://localhost:8081'
Start-Process dotnet -ArgumentList 'Financial.Api.dll' -WorkingDirectory publish
cd Financial.Web; npm run smoke-test
```

Do not use `--no-launch-profile` with `dotnet run` for a dev-server variant: it drops
`ASPNETCORE_ENVIRONMENT=Development` and CORS silently breaks
(`feedback_local_api_smoke_test_env`). The published build above needs no CORS because the SPA
is same-origin.

Data is seeded by copying the test JSON files and torn down by the OS temp directory / the CI
runner. The suite never cleans up: write specs use a per-run unique description and the one
spec that seeds expenses skips seeding on a retry. **Restart the API on fresh copies before each
local run** — a second run on the same data seeds the Historic Summary Average twice.

## Guard against the live data

`tests/e2e/global-setup.ts` runs before any browser starts and aborts unless:

- `SMOKE_APP_URL` is set (no default; `localhost:5173` used to be one),
- `GET /health` answers within 60 s, and
- `GET /categories` contains the inactive `E2E-TEST-DATA` category that only
  `data-cashflow.test.json` has.

The sentinel replaces a "which data file are you on" endpoint on purpose:
`DiagnosticsController` does not serve data-file paths.

## What the suite proves

Six `@smoke` specs in `Financial.Web/tests/e2e/`, each also failing on any console error:

| Spec | Journey |
|---|---|
| `app-loads` | `/` renders the investment tree (`XPI`) |
| `investment-asset` | XPI → Default → BCIA11 shows the asset summary |
| `add-expense` (3) | add an expense (`e2e-<runId>`) and see it listed; blank value → `Value must be a non-zero number`, nothing POSTed; forced 500 → error shown, form usable |
| `historic-average` | seed three expenses → Historic Summary Average shows `Mercado` `25.00` (catches frontend/API shape drift that still compiles) |

Rules: locate by role, label or visible text; `data-testid` only with a comment saying why; no
CSS classes, XPath, DOM position or fixed waits (`eslint-plugin-playwright` and the F11 hygiene
scan enforce the last). Debug locally with `npx playwright test --headed`, `--ui` or `--debug`.
Still missing: one keyboard-only completion of a critical workflow
(`cross-cutting-concerns.md`, Accessibility).

## WPF

`Tests/Financial.App.E2ETests` (xUnit, FlaUI.Core/UIA3, `net10.0-windows`) drives the real
`Financial.App` executable. The CI `wpf-e2e` job (`windows-latest`, feeds `ci-status`) builds it
and runs `--filter Category=Smoke`.

| Piece | How |
|---|---|
| Process | One fresh app per test (`AppSession.Run`), found at `Financial.App/bin/<Debug\|Release>/net10.0-windows/Financial.Presentation.App.exe` or `FINANCIAL_APP_EXE` |
| Data | Copies of `data.test.json` / `data-cashflow.test.json` in `%TEMP%/financial-app-e2e/<guid>`; env overrides select `LocalJson` for Investment, CashFlow and FxRates and turn observability off; the fixture refuses any data path outside that folder |
| Cleanup | Kill the process tree and delete the folder (the data is disposable, so no graceful close); a run first reaps processes and folders left by an aborted run (recorded pid + start time, only folders older than 10 minutes so a concurrent run is left alone) |
| Lookup | `FindById(AutomationId)`; names only where the label is the contract (tree items such as `XPI (BRL)`, tab headers, combo items). No coordinates, no fixed sleeps: waits are `Retry.WhileNull` with timeouts |
| On failure | `TestResults/e2e-artifacts/<test>/` gets `screen.png`, the latest `app-*.log` and `process.txt`; CI uploads it as `wpf-e2e-artifacts` |
| Serial | assembly-level `DisableTestParallelization` |

Journeys: app starts and the investment tree lists `XPI`; an asset opens with its summary
(`asset-summary-name`); an expense is added and listed; a blank value shows `Value must be a
non-zero number.` and adds nothing; the Monthly summary shows `Category total 0.00` and the
`Barclays` bank row. The app has no deterministic FX source, so nothing asserts a converted value.

Run it locally on a machine with a desktop (it opens and closes the app visibly, so do not use
the machine meanwhile):

```powershell
dotnet test Tests/Financial.App.E2ETests --filter "Category=Smoke"
```

Not a Playwright-style test: a failing run leaves artifacts, not a trace. A UIA-hosted runner that
turns unreliable moves the job to the nightly pipeline (`docs/ci-affected-pipeline.md`).

## Not the E2E environment

- `ApiEndpointTests` / `WebApplicationFactory<Program>` — one process, Integration.
- `docker-compose up` — the deployment shape, and a fine manual check after a merge, but CI
  does not run tests against it; the web-e2e job's published-process layout is equivalent.
