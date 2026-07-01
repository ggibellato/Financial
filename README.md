# Financial

Personal financial management tool for consolidating investment transactions across brokers in Brazil and the United Kingdom.

## Prerequisites

- .NET 10.0 SDK
- Node.js 22+

## Data file

The apps load investment data from a JSON file. Copy `data.example.json` to `data.json` locally (the real file is git-ignored).

Configure the path via the `DataJsonFile` environment variable or `appsettings.json`. If not set, defaults to `data.json` in the application directory.

### Storage providers

Two storage backends are supported, selected via the `Repository:Provider` setting (env var: `Repository__Provider`):

- **`LocalJson`** (default) — reads/writes `data.json` from the local filesystem. Set `DataJsonFile` to the file path.
- **`GoogleDrive`** — reads/writes a JSON file stored in Google Drive. Requires `GoogleDrive:CredentialsPath` (path to the service-account credentials JSON) and `GoogleDrive:FilePath` (the Drive file ID or path).

### Application configuration

Personalise the following sections in `appsettings.json` (or via environment variable overrides) before first use:

- **`Watchlist:Items`** — grouped list of tickers shown in the Dividend Check page combobox.
- **`AssetPriceFetch:Portfolios`** — list of `{ BrokerName, PortfolioName }` pairs used by the bulk price-fetch page.
- **`Dividends:DefaultExchange`** — exchange used when fetching dividend data (defaults to `BVMF`).

## Run

### API (Financial.Api)

```bash
dotnet run --project Financial.Api
```

Listens on `http://localhost:5190`. Health check: `http://localhost:5190/health`.

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
  -v ./data:/app/data:ro \
  -e DataJsonFile=/app/data/data.json \
  financial
```

Open `http://localhost:8080` in your browser.

### Run with Docker Compose (recommended)

```bash
docker compose up --build
```

This mounts `./data` read-only into the container and wires the environment variables automatically. Subsequent starts without code changes:

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
npm test
npm run build
```
