# Financial

Personal financial management tool for consolidating investment transactions across brokers in Brazil and the United Kingdom.

## Prerequisites

- .NET 10.0 SDK
- Node.js 22+

## Data file

The Investment and CashFlow domains each load their data from their own JSON file. Both example files are tracked under `data/` — copy them locally before first run (the real data files themselves are git-ignored):

- Investment: copy `data/data-investment.example.json` to `data/data-investment.json`.
- CashFlow: copy `data/data-cashflow.example.json` to `data/data-cashflow.json`.

Configure the paths via environment variable or `appsettings.json`. Each domain's storage settings live under their own JSON element — `Investment` and `CashFlow` — never at the config root:

- Investment: `Investment:DataJsonFile` (env: `Investment__DataJsonFile`). Defaults to `data-investment.json` in the application directory if unset.
- CashFlow: `CashFlow:DataJsonFile` (env: `CashFlow__DataJsonFile`). Defaults to `data-cashflow.json` in the application directory if unset.

Each domain has its own distinct default filename, so leaving either one unset no longer risks the two domains sharing a file.

### Storage providers

The Investment and CashFlow domains each select their storage backend independently, under their own config element.

**Investment** — via `Investment:Repository:Provider` (env: `Investment__Repository__Provider`):

- **`LocalJson`** (default) — reads/writes the file set by `Investment:DataJsonFile`.
- **`GoogleDrive`** — reads/writes a JSON file stored in Google Drive. Requires `Investment:GoogleDrive:CredentialsPath` (path to the service-account credentials JSON) and `Investment:GoogleDrive:FilePath` (the Drive file ID or path).

**CashFlow** — via `CashFlow:Repository:Provider` (env: `CashFlow__Repository__Provider`):

- **`LocalJson`** (default) — reads/writes the file set by `CashFlow:DataJsonFile`.
- **`GoogleDrive`** — requires `CashFlow:GoogleDrive:CredentialsPath` and `CashFlow:GoogleDrive:FilePath`.

### Application configuration

Personalise the following sections in `appsettings.json` (or via environment variable overrides) before first use:

- **`Watchlist:Items`** — grouped list of tickers shown in the Dividend Check page combobox.
- **`AssetPriceFetch:Portfolios`** — list of `{ BrokerName, PortfolioName }` pairs used by the bulk price-fetch page (also covers cryptocurrency price fetching).
- **`Dividends:DefaultExchange`** — exchange used when fetching dividend data (defaults to `BVMF`).
- **`Cors:AllowedOrigins`** — array of allowed origins for the API's CORS policy (`Financial.Api/Program.cs`). Not present in the base `appsettings.json` (only `AllowedHosts`, an unrelated ASP.NET Core key) or in `appsettings.Production.json` — it's set in `appsettings.Development.json` to the Web dev server's URLs. If unset, all cross-origin requests are blocked. You only need to touch this if running the API and Web dev servers separately instead of via Docker (where both are served from the same origin, so CORS doesn't apply).

## Run

### API (Financial.Api)

```bash
dotnet run --project Financial.Api
```

Listens on `http://localhost:5190`.

**Diagnostics.** `GET /api/v1/financial/health` reports liveness plus, for each bounded context, the
storage provider, sync state, last error and last successful save. It answers `200` whenever the
process is serving - a storage fault shows in the body, not the status code, so a probe does not
restart the container over an outage a restart cannot fix.

There is deliberately **no endpoint that returns repository paths**. One existed, gated on
`ASPNETCORE_ENVIRONMENT` - a guard one environment variable away from being off, and
`docker-compose.yml` runs `Development`, so anyone starting the app that way published their
data-file path on a port with no authentication in front of it. Where an install stores its data is
answerable from its own compose file and environment; the API does not need to answer it.

### Web (Financial.Web)

```bash
cd Financial.Web
npm install
npm run dev
```

Listens on `http://localhost:5173`. Copy `Financial.Web/.env.example` to `Financial.Web/.env` and set `API_BASE_URL` to point at the API.

### Desktop (Financial.App)

```bash
dotnet run --project Financial.App
```

`dotnet run` and Visual Studio set `DOTNET_ENVIRONMENT=Development` automatically via `launchSettings.json`, which loads `Financial.App/appsettings.Development.json` with a relative path to the shared `data/data-investment.json`. Running the compiled `.exe` directly requires setting `DOTNET_ENVIRONMENT=Development` in your system environment variables.

## Docker

The API and web frontend are packaged into a single image. The .NET server serves both the REST API and the React SPA from the same port.

### Build the image

```bash
docker build -t financial .
```

### Run the container

```bash
docker run -p 8080:8080 \
  -v ./data:/app/data \
  -e Investment__DataJsonFile=/app/data/data-investment.json \
  -e Investment__Repository__Provider=LocalJson \
  -e CashFlow__DataJsonFile=/app/data/data-cashflow.json \
  -e CashFlow__Repository__Provider=LocalJson \
  financial
```

Open `http://localhost:8080` in your browser.

### Run with Docker Compose (recommended)

```bash
docker compose up --build
```

This mounts `./data` into the container and wires the environment variables (including the CashFlow ones above) automatically. Subsequent starts without code changes:

```bash
docker compose up
```

Stop and remove containers:

```bash
docker compose down
```

## Observability

Distributed tracing (OpenTelemetry) is built in but **off by default** — a normal `docker compose up` or `dotnet run` needs no observability backend and exports nothing. When enabled, every API request and WPF save command produces a correlated trace spanning controller → Application service → JSON storage save, with Serilog log entries carrying the same trace id. Span attributes are restricted to an allow-list (bounded context, entity type, entity GUID, operation name/result, error type) — never financial values, account names, or exception messages.

Configuration (`appsettings.json` or `Observability__*` environment variables):

| Key | Default | Meaning |
|---|---|---|
| `Observability:Enabled` | `false` | Master switch; when off, a no-op tracer is used |
| `Observability:Backend` | `Jaeger` | `Jaeger` or `Langfuse`; anything else fails startup |
| `Observability:Endpoint` | `http://localhost:4317` | OTLP endpoint (gRPC for Jaeger; HTTP path for Langfuse) |
| `Observability:Langfuse:PublicKey` / `:SecretKey` | empty | Required when `Backend=Langfuse`; missing keys fail startup |

### Local backends (optional overlay)

`docker-compose.observability.yml` is never referenced by the base compose file; it only does anything when a profile is explicitly requested.

Jaeger (UI at `http://localhost:16686`):

```bash
docker compose -f docker-compose.yml -f docker-compose.observability.yml --profile jaeger up
```

Langfuse (UI at `http://localhost:3000`, login `local@financial.dev` / `financial-local`):

```bash
OBSERVABILITY_BACKEND=Langfuse \
OBSERVABILITY_ENDPOINT=http://langfuse-web:3000/api/public/otel/v1/traces \
docker compose -f docker-compose.yml -f docker-compose.observability.yml --profile langfuse up
```

Both stacks are ephemeral (no volumes — trace data dies with the containers) and local-only. The Langfuse profile is Langfuse's official self-hosted stack seeded headlessly with throwaway local keys (`pk-lf-local`/`sk-lf-local`). If the configured backend is unreachable, the app starts and works normally — export failures are retried silently in the background.

Design docs live in `specs/001-application-observability/` (spec, plan, task log); `logging-audit.md` there records the logging-gap audit whose findings the structured-logging work addresses.

## Build and test

```bash
# .NET
dotnet restore
dotnet build Financial.slnx
dotnet test Financial.slnx

# Web
cd Financial.Web
npm install
npm run lint
npm test
npm run build
```

Other useful `Financial.Web` scripts: `npm run test:watch` (watch mode), `npm run preview` (preview a production build), `npm run smoke-test` (Playwright-based smoke test against a running instance, also run in CI).

## Import tooling

One-off console/desktop tools for migrating data from spreadsheets into the JSON data files. Not part of the main app runtime.

### CashFlowSpreadsheetImport

Reads a personal expense-tracking Excel workbook and populates `data-cashflow.json`.

```bash
dotnet run --project Tools/CashFlowSpreadsheetImport -- <path-to-Despesas.xlsx> [output-json-path] [--mensais-only]
```

Defaults to reading `Despesas.xlsx` from the Downloads folder and writing to `data/data-cashflow.json`. If the output file already exists, it's backed up automatically (timestamped sibling file) before being overwritten.

### ImportGoogleSpreadSheets

Legacy WPF desktop utility for a one-time import of Investment portfolio data from Google Sheets into `data-investment.json`. Not runnable headless — open and run it from Visual Studio.

## Local deploy tooling

`scripts/deploy.ps1` publishes the current local state of `Financial.App` (WPF) and `Financial.Api` (API + SPA) to a local `deploy/` folder. Manual, local-only tooling — not part of CI/CD, and `deploy/` itself is git-ignored.

```powershell
./scripts/deploy.ps1
```

Re-run any time to refresh the deployed copies with whatever is currently on disk. On each run it:

- Stops any currently running instances launched from `deploy/`.
- Publishes `Financial.App` and `Financial.Api` (framework-dependent — the build machine must match the run machine) into `deploy/Financial.App` and `deploy/Financial.Web`.
- Deploys each project's `appsettings.Production.json` (excluded from `dotnet publish` output) and stamps in the machine-local Google Drive credentials path.
- Builds the React SPA and copies it into `deploy/Financial.Web/wwwroot`.
- Copies the launcher scripts from `scripts/deploy-templates/` into `deploy/`.
- Writes `deploy/deploy-info.txt` with the deployed branch and commit.

First run creates `scripts/deploy.local.json` from `scripts/deploy.local.example.json` (both git-ignored except the example) — edit it to set `GoogleDriveCredentialsPath` to your service-account credentials file before starting the deployed apps. Both deployed apps run against `GoogleDriveJson` storage, fixed via the checked-in `appsettings.Production.json` files.

After deploying, start everything with `deploy/start-all.ps1` (or `start-app.ps1`/`start-web.ps1` individually).
