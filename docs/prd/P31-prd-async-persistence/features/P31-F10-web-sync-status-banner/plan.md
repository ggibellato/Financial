# Implementation Plan: F10. Web Sync Status Banner

**Prerequisites:**
- F09 (`useSyncStatus` hook) merged to `main`
- No new libraries

### Stage 1: Date Formatting Utility

**1. formatDateTime Utility** - Add a local date/time formatter to `utils/formatters.ts` for rendering a sync status's last-successful-save timestamp, following the existing `formatShortDate` guard/format conventions, with test coverage for valid, null/undefined, and invalid inputs.

### Stage 2: Banner Component

**2. SyncStatusBanner Component** - Create a component that calls `useSyncStatus()` and renders a warning banner naming each context currently in a `Failed` state, its last error, and its last successful save time (or an explicit "no prior save" indicator) — rendering nothing when no context is `Failed`.

**3. Global Wiring** - Render `SyncStatusBanner` once in `App.tsx` above the routed `Outlet`, so it appears on every route.

**4. Component Test Coverage** - Cover the visibility rule (hidden/visible transitions), correct per-context naming including the simultaneous-failure case, and the displayed error/save-time content.
