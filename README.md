# Financial

Personal financial management tool for consolidating investment transactions across brokers in Brazil and the United Kingdom.

## Prerequisites

- .NET 10.0 SDK
- Node.js 22+

## Data file

The Investment and CashFlow domains each load their data from their own JSON file. Both example files are tracked under `data/` — copy them locally before first run (the real data files themselves are git-ignored):

- Investment: copy `data/data.example.json` to `data/data.json`.
- CashFlow: copy `data/data-cashflow.example.json` to `data/data-cashflow.json`.

Configure the paths via environment variable or `appsettings.json`:

- Investment: `DataJsonFile`. Defaults to `data.json` in the application directory if unset.
- CashFlow: `CashFlow:DataJsonFile` (env: `CashFlow__DataJsonFile`). **This has no domain-specific default** — if left unset, it silently falls back to the same `data.json` used by the Investment domain, so the two domains would end up reading/writing the same file. Always set it explicitly (e.g. to `data-cashflow.json`).

### Storage providers

The Investment and CashFlow domains each select their storage backend independently.

**Investment** — via `Repository:Provider` (env: `Repository__Provider`):

- **`LocalJson`** (default) — reads/writes `data.json` from the local filesystem. Set `DataJsonFile` to the file path.
- **`GoogleDrive`** — reads/writes a JSON file stored in Google Drive. Requires `GoogleDrive:CredentialsPath` (path to the service-account credentials JSON) and `GoogleDrive:FilePath` (the Drive file ID or path).

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

Listens on `http://localhost:5190`. Health check: `http://localhost:5190/api/v1/financial/health`.

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

`dotnet run` and Visual Studio set `DOTNET_ENVIRONMENT=Development` automatically via `launchSettings.json`, which loads `Financial.App/appsettings.Development.json` with a relative path to the shared `data/data.json`. Running the compiled `.exe` directly requires setting `DOTNET_ENVIRONMENT=Development` in your system environment variables.

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
  -e DataJsonFile=/app/data/data.json \
  -e Repository__Provider=LocalJson \
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
dotnet run --project Integrations/CashFlowSpreadsheetImport -- <path-to-Despesas.xlsx> [output-json-path] [--mensais-only]
```

Defaults to reading `Despesas.xlsx` from the Downloads folder and writing to `data/data-cashflow.json`. If the output file already exists, it's backed up automatically (timestamped sibling file) before being overwritten.

### ImportGoogleSpreadSheets

Legacy WPF desktop utility for a one-time import of Investment portfolio data from Google Sheets into `data.json`. Not runnable headless — open and run it from Visual Studio.
