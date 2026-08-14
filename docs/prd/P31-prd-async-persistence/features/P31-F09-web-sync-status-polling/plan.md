# Implementation Plan: F09. Web Sync Status Polling

**Prerequisites:**
- F08 (`GET /api/v1/financial/sync-status`) merged to `main`
- No new libraries — uses the existing `fetch`-based `financialApiClient` and native `setInterval`

### Stage 1: API Client Surface

**1. Sync Status Types** - Add the `SyncStatusDto` and `SyncStatusResponseDto` TypeScript interfaces to `api/types.ts`, mirroring the API's camelCase response shape for both bounded contexts.

**2. API Client Method** - Add a `getSyncStatus` method to the `FinancialApiClient` interface and its implementation, calling the F08 endpoint and returning the parsed response.

### Stage 2: Polling Hook

**3. useSyncStatus Hook** - Create a hook that starts polling on mount, following the existing reducer + effect pattern from `useAggregatedSummary`, and exposes the latest successfully-polled combined status to consumers.

**4. Hook Test Coverage** - Cover the mount-time fetch, the 15-second interval cadence, resilience to a failed poll (retaining the previous status and continuing to poll), and interval cleanup on unmount.
