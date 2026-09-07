# Implementation Plan: F01. Google Calendar Account Connection

**Prerequisites:**
- `Google.Apis.Calendar.v3` NuGet package (new to this codebase; version aligned with the existing `Google.Apis.*` 1.64.0 family already used by `GoogleCore`/`GoogleDrive`)
- A Google Cloud project with a Calendar API OAuth 2.0 Client ID (Web application type) and its client secret — provided by the user out of band, read from `CashFlow:GoogleCalendar:ClientId`/`ClientSecret`/`RedirectUri`
- `CashFlow:GoogleCalendar:CredentialsPath` configuration value pointing at a local path (e.g. under `data/`, already git-ignored)

Each phase below is sized to land as its own pull request (≤8 non-test code files), following the project's Domain → Application → Infrastructure → API vertical-slice-per-PR convention. F01 has no Domain changes, so the sequence starts at the vendor SDK wrapper.

### Phase 1: Google Calendar SDK Wrapper (`Integrations/GoogleCalendar`)

**1. Project scaffolding** - Create the new `Integrations/GoogleCalendar` project, referencing `GoogleCore` and the `Google.Apis.Calendar.v3`/`Google.Apis.Auth` packages, and register it in `Financial.slnx` alongside its test project.

**2. OAuth + Calendar client** - Implement the generic, CashFlow-agnostic client that builds the consent URL, exchanges/refreshes authorization codes, revokes tokens, looks up the connected account's email, and creates/deletes a calendar — all via primitive parameters, per the spec's interface.

**3. DI registration** - Add the service-collection extension that registers the client and its backing `HttpClient`.

### Phase 2: Application Contracts (`Financial.CashFlow.Application`)

**4. Service and store interfaces** - Declare the Application-owned abstractions for the Calendar/OAuth client and the local connection store, and the orchestration service's own contract, per the spec.

**5. Connection model and DTOs** - Add the internal persisted connection shape and the three response DTOs (status, callback result, disconnect result).

**6. Configuration options** - Add the settings POCO for client id/secret/redirect URI/credentials path.

### Phase 3: Application Orchestration (`Financial.CashFlow.Application`)

**7. Connection service** - Implement the orchestration service: authorization URL + state issuance and validation, callback handling (including replace-previous-connection and revoke-on-calendar-creation-failure), status reporting (including transparent refresh and revoked-token tombstoning), and disconnect (always clearing local state regardless of remote call outcomes).

**8. DI registration** - Register the new service in the Application service-collection extension, alongside the existing CashFlow services.

### Phase 4: Infrastructure (`Financial.CashFlow.Infrastructure`)

**9. Configuration keys and project reference** - Add the `CashFlow:GoogleCalendar:*` configuration key constants and the project reference to `Integrations/GoogleCalendar`.

**10. Connection store** - Implement the local-file-backed `IGoogleCalendarConnectionStore`.

**11. Client adapter** - Implement the `IGoogleCalendarClient` adapter that forwards configured credentials into the Integrations project's client.

**12. DI registration** - Bind the new configuration options and register the store and adapter in the Infrastructure service-collection extension.

### Phase 5: API and Configuration (`Financial.Api`)

**13. Controller** - Add the four endpoints (status, connect, callback, disconnect) per the spec's contracts, including the callback's minimal HTML success/failure response.

**14. Startup wiring and configuration** - Wire the new project reference and DI registration into `Program.cs`, and add the placeholder `CashFlow:GoogleCalendar` configuration block to `appsettings.json` (and `appsettings.Development.json` if it carries its own `CashFlow` section).

**15. OpenAPI snapshot and documentation** - Regenerate the OpenAPI contract snapshot for the new endpoints, and document the new `CashFlow:GoogleCalendar:*` configuration keys in `README.md` alongside the existing `CashFlow:GoogleDrive` entry.
