# Implementation Plan: F01. Google Calendar Account Connection

**Prerequisites:**
- `Google.Apis.Calendar.v3` NuGet package (new to this codebase; version aligned with the existing `Google.Apis.*` 1.64.0 family already used by `GoogleCore`/`GoogleDrive`)
- A Google Cloud project with a Calendar API OAuth 2.0 Client ID (Web application type) and its client secret — provided by the user out of band, read from `CashFlow:GoogleCalendar:ClientId`/`ClientSecret`/`RedirectUri`
- `CashFlow:GoogleCalendar:CredentialsPath` configuration value pointing at a local path (e.g. under `data/`, already git-ignored)

Each phase below is sized to land as its own pull request (≤8 non-test code files), following the project's Domain → Application → Infrastructure → API vertical-slice-per-PR convention. F01 has no Domain changes, so the sequence starts at the vendor SDK wrapper. The core correction from the first pass: Application defines a provider-agnostic calendar contract, not a Google-specific one — Phase 2 has no Google awareness at all, and Google-specific settings move to Phase 4.

### Phase 1: Google Calendar SDK Wrapper (`Integrations/GoogleCalendar`)

**1. Project scaffolding** - Create the new `Integrations/GoogleCalendar` project, referencing `GoogleCore` and the `Google.Apis.Calendar.v3`/`Google.Apis.Auth` packages, and register it in `Financial.slnx` alongside its test project.

**2. OAuth + Calendar client** - Implement the generic, CashFlow-agnostic client that builds the consent URL, exchanges/refreshes authorization codes, revokes tokens, looks up the connected account's email, and creates/deletes a calendar — all via primitive parameters.

**3. DI registration** - Add the service-collection extension that registers the client and its backing `HttpClient`.

### Phase 2: Application Contracts (`Financial.CashFlow.Application`) — provider-agnostic

**4. Standard calendar contract** - Declare `ICalendarProvider` (the calendar operations any provider implementation must support), `ICalendarConnectionStore` (local connection persistence), and `ICalendarIntegrationService` (the orchestration contract) — none referencing Google or any vendor SDK type.

**5. Connection model and DTOs** - Add the internal persisted connection shape and the three response DTOs, all named generically (no provider name).

**6. Revoked-token exception** - Add the provider-agnostic `CalendarTokenRevokedException`, the signal `ICalendarProvider.RefreshAccessTokenAsync` uses when a provider rejects the stored refresh token.

### Phase 3: Application Orchestration (`Financial.CashFlow.Application`)

**7. Connection service** - Implement `CalendarIntegrationService` against `ICalendarProvider`/`ICalendarConnectionStore` alone: authorization URL + state issuance and validation, callback handling (replace-previous-connection, revoke-on-calendar-creation-failure), status reporting (transparent refresh, revoked-token tombstoning), and disconnect (always clearing local state regardless of remote call outcomes).

**8. DI registration** - Register `ICalendarIntegrationService` → `CalendarIntegrationService` in the Application service-collection extension.

### Phase 4: Infrastructure (`Financial.CashFlow.Infrastructure`) — where Google becomes concrete

**9. Configuration and project reference** - Add the Google-specific settings options and its `CashFlow:GoogleCalendar:*` configuration keys, and the project reference to `Integrations/GoogleCalendar`.

**10. Connection store** - Implement the local-file-backed `ICalendarConnectionStore` (provider-agnostic).

**11. The first `ICalendarProvider` implementation** - Implement `GoogleCalendarProviderAdapter`, forwarding configured Google credentials into the Integrations project's client and translating its revoked-token signal into the Application-level exception.

**12. DI registration** - Bind the settings and register the store and adapter (`ICalendarProvider` → `GoogleCalendarProviderAdapter`) in the Infrastructure service-collection extension.

### Phase 5: API and Configuration (`Financial.Api`)

**13. Controller** - Add the four endpoints (status, connect, callback, disconnect), depending only on `ICalendarIntegrationService`.

**14. Startup wiring and configuration** - Wire the new project reference and DI registration into `Program.cs`, and add the placeholder `CashFlow:GoogleCalendar` configuration block to `appsettings.json` (and `appsettings.Development.json`).

**15. Architecture rule, OpenAPI snapshot, and documentation** - Add the architecture-rule test pinning that `Financial.CashFlow.Application` never references `Financial.Integrations.GoogleCalendar`, regenerate the OpenAPI contract snapshot for the new endpoints, and document the new `CashFlow:GoogleCalendar:*` configuration keys in `README.md`.
