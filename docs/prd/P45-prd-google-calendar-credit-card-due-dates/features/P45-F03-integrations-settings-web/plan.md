# Implementation Plan: F03. Integrations Settings — Web

**Prerequisites:**
- F01 (Google Calendar Account Connection) merged - `CalendarIntegrationController`'s `status`/`connect`/`disconnect` endpoints exist.
- F02 (Credit-Card Due-Date Event Sync) merged - the `credit-cards/sync-status` and `credit-cards/{id}/resync` endpoints exist.
- A connected Google Calendar (via F01) to observe the connected/per-card states during manual verification; a not-connected state is the default starting point.

Each phase is sized to land as its own commit on this feature's single PR, following the project's vertical-slice-per-phase convention (this feature ships as one PR per the project's established precedent, not one PR per phase).

### Phase 1: Backend - Expose the Dedicated Calendar ID

**1. Wire field addition** - Add `CalendarId` to `CalendarConnectionStatusDTO` and populate it in `CalendarIntegrationService.ToStatusDto`, so the Web page can build a precise deep link to the dedicated calendar.

**2. Contract snapshot** - Regenerate the OpenAPI snapshot and `Financial.Web/src/api/generated/openapi.ts` for the new field.

### Phase 2: Web - API Client and Data Hooks

**3. DTO aliases and client methods** - Add the three new DTO type aliases to `src/api/types.ts` and the five new methods (`getCalendarStatus`, `disconnectCalendar`, `getCalendarSyncStatuses`, `resyncCreditCardCalendar`, `resyncAllCalendars`) plus the `buildCalendarConnectUrl()` helper to `financialApiClient.ts`, following the existing `request`/`requestVoid` pattern.

**4. Connection hook** - Add `useCalendarConnection`, covering initial load, retry, `connect()` (opens the consent flow in a new tab and arms a window-focus listener that re-polls status until connected), and `disconnect()` with its own in-flight/error state.

**5. Sync-status hook** - Add `useCalendarSyncStatuses`, loading and joining `GET /credit-cards` with `GET .../credit-cards/sync-status` into one per-card row list, with per-row retry state for the resync action.

### Phase 3: Web - Status Badge and Page Composition

**6. Sync status badge** - Add `CalendarSyncStatusBadge`, rendering the PRD's exact Synced/Syncing…/Sync failed accessible text for each state, text always visible (never color-alone).

**7. Integrations page** - Add `IntegrationsPage` (and its stylesheet), composing both hooks to render every state from the PRD's Experience section: initial/loading, not-connected (empty state with Connect button), connecting (progress state), connected (account/calendar/connected-since plus the per-card table with per-row Retry), server error (retry affordance), and the Disconnect button's disabled-while-in-flight state.

**8. Disconnect confirmation** - Add the inline confirmation dialog to `IntegrationsPage`, warning that the dedicated calendar and its events will be deleted, proceeding only on explicit confirmation.

### Phase 4: Web - Navigation Wiring and Documentation

**9. Route and sidebar entry** - Register `/settings/integrations` in `routes.tsx` and `lazyPages.tsx`, and add the "Integrations" child under the existing `settings` category in `navTree.ts`.

**10. Documentation** - Update `README.md` to mention the Settings > Integrations page alongside the existing Calendar integration section.
