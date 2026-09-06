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
| Driver | `node Financial.Web/scripts/smoke-test.mjs` — `chromium.launch()`, `SMOKE_APP_URL=http://localhost:8080` |
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
node Financial.Web/scripts/smoke-test.mjs
```

Do not use `--no-launch-profile` with `dotnet run` for a dev-server variant: it drops
`ASPNETCORE_ENVIRONMENT=Development` and CORS silently breaks
(`feedback_local_api_smoke_test_env`). The published build above needs no CORS because the SPA
is same-origin.

Data is seeded by copying the test JSON files and torn down by the OS temp directory / the CI
runner; the script itself seeds three expenses through `POST /expenses` and never cleans them
(fresh copies each run).

## What the script proves today, and what to add

Today (`smoke-test.mjs`): app loads with no console errors, the investment tree renders
`XPI`, `/cashflow/annual-summary` → "Historic Summary Average" tab shows `Mercado = 25.00`
for the seeded year. That is one success journey across API + SPA — it exists because a
bundle that compiles can still render blank data when the frontend types drift.

Per the fundamentals, E2E owes each critical flow one success and one failure journey.
Missing today and to be added in the same script (or a `@playwright/test` suite, see
`../artifacts/future-types.md`):

- One rejected submission surfaced in the UI (`negative-path-testing.md` lists three candidates).
- One keyboard-only completion of a critical workflow (`cross-cutting-concerns.md`, Accessibility).

Keep journeys to the critical set — this is a single-user tool; Integration is where breadth
lives.

## WPF

No E2E harness exists for `Financial.App` (no FlaUI/WinAppDriver). WPF's top automated layer is
Integration (real services in-process); the manual launch-and-look required by
`docs/rules/ui.md` "Completion requirement" is the substitute. If a harness is added, drive it by
`AutomationProperties.Name` + `GetClickablePoint()`.

## Not the E2E environment

- `ApiEndpointTests` / `WebApplicationFactory<Program>` — one process, Integration.
- `docker-compose up` — the deployment shape, and a fine manual check after a merge, but CI
  does not run tests against it; the smoke job's published-process layout is equivalent.
