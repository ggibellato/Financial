# Implementation Plan: F02. Credit-Card Due-Date Event Sync

**Prerequisites:**
- F01 (Google Calendar Account Connection) merged - `ICalendarProvider`, `ICalendarConnectionStore`, `ICalendarIntegrationService`, `CalendarConnection`, `GoogleCalendarProviderAdapter`, `CalendarIntegrationController` all exist.
- A connected Google Calendar (via F01) to observe real event sync during manual verification.

Each phase is sized to land as its own pull request (≤8 non-test code files), following the project's vertical-slice-per-PR convention. F02 touches no Domain code (`CreditCard`/`CardStatement` entities are unchanged), so the sequence runs Integrations → Application (in three slices, since it's the bulk of the feature) → Infrastructure/API.

### Phase 1: Event Operations (`Integrations/GoogleCalendar`)

**1. Event CRUD on the OAuth client** - Add create/update/delete for a single all-day calendar event with a fixed 1-day-before popup reminder to `IGoogleCalendarOAuthClient`/`GoogleCalendarOAuthClient`, per the spec's contract.

**2. Calendar-not-found signal** - Add `GoogleCalendarNotFoundException`, thrown when Google reports the dedicated calendar no longer exists during any of the new event calls.

### Phase 2: Application - Provider Contract and Connection Model

**3. Extend the standard calendar contract** - Add the same event create/update/delete operations to `ICalendarProvider`, in provider-agnostic terms, and document that they may throw the new Application-level `CalendarNotFoundException`.

**4. Event-ID mapping and status shapes** - Add the `CreditCardId → event ID` mapping to `CalendarConnection`, and add the per-card sync status enum/record used throughout the rest of the feature.

**5. Expose valid-token access** - Add a method to `ICalendarIntegrationService`/`CalendarIntegrationService` that returns a currently-valid access token (reusing the existing refresh/tombstone logic), for the sync orchestrator to reuse rather than duplicate.

### Phase 3: Application - Sync Orchestration

**6. Period-based outstanding total** - Extend `ICardStatementService`/`CardStatementService` with a lookup that computes a credit card's outstanding total for a given year/month directly from expenses, working whether or not a `CardStatement` row exists yet for that period, and reporting separately whether any charges were found.

**7. Per-card sync status store** - Add the in-memory store tracking each card's current sync state, last successful sync time, and last error.

**8. Sync orchestration service** - Implement the service that: builds the event's title/description/date from the card and its period balance; creates or updates the card's persistent event (or removes it when the card no longer qualifies); handles a calendar-not-found failure by recreating the calendar and re-syncing every qualifying card; exposes a fire-and-forget trigger, an awaited per-card resync, an awaited resync-all, and the current status list. Register the new store and service in the Application DI extension.

### Phase 4: Application - Wire the Trigger

**9. Trigger sync from credit-card saves** - Add the sync-trigger dependency to `CreditCardService` and call it after each of create/update/delete succeeds, so a save's own success is never affected by sync outcome.

### Phase 5: Infrastructure, API, and Documentation

**10. Google adapter event operations** - Implement the three new `ICalendarProvider` event methods on `GoogleCalendarProviderAdapter`, forwarding to the Integrations client and translating its calendar-not-found signal.

**11. Resync and status endpoints** - Add the per-card resync, resync-all, and sync-status endpoints to `CalendarIntegrationController`.

**12. Contract snapshot and documentation** - Regenerate the OpenAPI snapshot and frontend generated types for the three new endpoints, and update `README.md` if the new endpoints or behavior warrant it.
