# Financial
Financial investments control

## data.json configuration
The apps load investment data from a JSON file path configured via `DataJsonFile` (environment variable or `appsettings.json`).
If not set, the default is `data.json` in the application directory.
Use `data.example.json` as a template and copy it to `data.json` locally (the real file is ignored by git).

## Development run instructions

### API (Financial.Api)
1. Ensure `Financial.Api\appsettings.Development.json` has the correct `DataJsonFile` path (or set `DataJsonFile` as an environment variable).
2. Run:
   `dotnet run --project Financial.Api`
3. The API listens on `http://localhost:5190` (see `Financial.Api\Properties\launchSettings.json`).
4. Health check: `http://localhost:5190/api/v1/financial/health`.

### Web (Financial.Web)
1. Ensure Node.js 24.13+.
2. Run:
   `cd Financial.Web`
   `npm install`
   `npm run dev`
3. Optional: set `VITE_API_BASE_URL` (default: `https://localhost:7256/api/v1/financial`).
4. The dev server listens on `http://localhost:5173`.

### WPF (Financial.App)
1. Ensure `Financial.App\appsettings.json` has the correct `DataJsonFile` path.
2. Run:
   `dotnet run --project Financial.App`
