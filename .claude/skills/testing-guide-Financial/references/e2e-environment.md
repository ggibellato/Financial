> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# E2E Environment

Decided in Phase 3 (2026-09-06): **the CI `smoke` job is the E2E environment.** It is the only
place more than one deployed process runs — the published `Financial.Api.dll` (serving the
built SPA from `wwwroot`) and Playwright's Chromium — over real HTTP against real JSON files.

## What is real

| Piece | In the smoke job |
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

No E2E harness exists for `Financial.App` (no FlaUI/WinAppDriver). WPF's top automated layer is
Integration (real services in-process); the manual launch-and-look required by
`docs/rules/ui.md` "Completion requirement" is the substitute. If a harness is added, drive it by
`AutomationProperties.Name` + `GetClickablePoint()`.

## Not the E2E environment

- `ApiEndpointTests` / `WebApplicationFactory<Program>` — one process, Integration.
- `docker-compose up` — the deployment shape, and a fine manual check after a merge, but CI
  does not run tests against it; the smoke job's published-process layout is equivalent.
