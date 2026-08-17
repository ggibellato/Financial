# Build & Deployment

See legend in [README.md](README.md).

## CI (`.github/workflows/build.yml`)

**CONFIRMED — three jobs run on every PR:**

1. **.NET build+test** (Windows).
2. **Web lint+test+build** (Ubuntu, Node 24) — `npm run lint`, `npm test` (vitest), `npm run build` (`tsc -b && vite build` — catches type errors the vitest run alone would miss).
3. **`browser-smoke-test`** — publishes the real API and the built SPA into the API's `wwwroot`, boots the published API against seeded test JSON data (copies of `Tests/Financial.Api.Tests/TestData/data.test.json` and `data-cashflow.test.json`), waits on `GET /api/v1/financial/health`, then runs the Playwright smoke script against the live instance. Gated on the first two jobs passing.

`.github/workflows/semantic-pr.yml` enforces Conventional Commit PR titles (`feat|fix|docs|chore|refactor|test|perf|ci|build`).

## Docker

**CONFIRMED.** `Dockerfile` + `docker-compose.yml` build a single image: the React SPA and the API are built together, and the API serves both from one port (8080) in production — `MapFallbackToFile("index.html")` handles the SPA fallback route. `./data` is volume-mounted for the JSON files.

```
docker build -t financial .
docker run -p 8080:8080 -v ./data:/app/data \
  -e Investment__DataJsonFile=/app/data/data-investment.json \
  -e Investment__Repository__Provider=LocalJson \
  -e CashFlow__DataJsonFile=/app/data/data-cashflow.json \
  -e CashFlow__Repository__Provider=LocalJson \
  financial
```

Recommended path: `docker compose up --build` (wires the above automatically).

## Local (non-Docker) run

```
dotnet run --project Financial.Api        # http://localhost:5190
cd Financial.Web && npm run dev            # http://localhost:5173, proxies /api to 5190
dotnet run --project Financial.App         # WPF, in-process, own composition root
```

Dev config points `Investment:DataJsonFile`/`CashFlow:DataJsonFile` at `../../../../data/*.json` (repo-root `data/`) and CORS allows the Vite dev server ports. Do not run against ports that overlap a live Docker deployment (default 8080) — check `netstat` before smoke-testing locally.

## Local deploy tooling (`scripts/deploy.ps1`) — manual, not part of CI/CD

**CONFIRMED.** Publishes the current local state of `Financial.App` and `Financial.Api` (framework-dependent — build machine must match run machine) into a git-ignored `deploy/` folder:

- Stops any currently running instances launched from `deploy/`.
- Publishes both apps, deploys each project's `appsettings.Production.json` (excluded from `dotnet publish` output by default) and stamps in the machine-local Google Drive credentials path.
- Builds the React SPA into `deploy/Financial.Web/wwwroot`.
- Copies launcher scripts from `scripts/deploy-templates/`.
- Writes `deploy/deploy-info.txt` with the deployed branch/commit.

Both deployed apps run against `GoogleDrive` storage, fixed via checked-in `appsettings.Production.json` files. First run creates `scripts/deploy.local.json` from the tracked `.example.json` (git-ignored except the example) — must be edited to set `GoogleDriveCredentialsPath` before starting. Start everything with `deploy/start-all.ps1` (or `start-app.ps1`/`start-web.ps1` individually).

## Import tooling — not part of the runtime

**CONFIRMED, per `README.md`.** One-off tools, not deployed:

- `Tools/CashFlowSpreadsheetImport` — reads `Despesas.xlsx`, populates `data-cashflow.json`. Backs up the output file automatically (timestamped) if it already exists.
- `Tools/ImportGoogleSpreadSheets` — legacy WPF utility for a one-time Investment portfolio import from Google Sheets. Not runnable headless.

Never run either against the live data file — verify against a temp copy first (see [07-data-persistence.md](07-data-persistence.md)).
