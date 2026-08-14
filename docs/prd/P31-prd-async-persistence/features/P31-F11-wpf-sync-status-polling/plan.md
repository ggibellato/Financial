# Implementation Plan: F11. WPF Sync Status Polling

**Prerequisites:**
- F04 (CashFlow debounced instance) and F05 (Investment debounced instance) merged to `main`
- No new libraries — uses `System.Windows.Threading.DispatcherTimer` and the existing `ISyncStatusProvider` casting pattern from `SyncStatusController` (F08)

### Stage 1: SyncStatusViewModel

**1. SyncStatusViewModel** - Create a ViewModel that resolves both bounded contexts' current sync status directly from their repository instances, refreshing on construction and every 15 seconds via a `DispatcherTimer`, following the same `ISyncStatusProvider` resolution pattern already used by the API's sync-status endpoint.

**2. ViewModel Test Coverage** - Cover initial-poll-on-construction, per-context status resolution (including the not-a-sync-status-provider default), independence between contexts, and constructor guard clauses, using hand-written stub repositories.

### Stage 2: DI Registration and Shell Wiring

**3. DI Registration and MainWindow Wiring** - Register `SyncStatusViewModel` as a DI singleton, inject it into `MainWindow`, and thread it through `MainShellViewModel` as a new exposed property, so it's reachable for F12's future binding with no additional plumbing.
