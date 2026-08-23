# Implementation Plan: F02. Sync Abstraction Extraction

**Prerequisites:**
- F01 (Persistence Abstraction Extraction) merged to `main`
- .NET 10 SDK, existing solution builds and all tests pass on `main` before starting

### Stage 1: Move the Sync contracts into Financial.Shared.Abstractions

**1. New Sync namespace** - Create a `Sync/` folder under `Financial.Shared.Abstractions` and move `ISyncStatusProvider`, `SyncState`, `SyncStatus`, and `JsonStorageSyncExtensions` into it unchanged, then delete the now-empty `Financial.Shared.Infrastructure/Sync/` folder.

### Stage 2: Fix up every consumer so the build stays green

**2. Financial.Shared.Infrastructure's own consumers** - Update the `using` statements in `DebouncedJsonStorage` and `ShutdownFlushHostedService` so they resolve the relocated types from `Financial.Shared.Abstractions.Sync`.

**3. CashFlow and Investment Infrastructure** - Update the `using` statements in `CashFlowJsonRepository` and `InvestmentJsonRepository`.

**4. Financial.Api and Financial.App** - Update the `using` statements in `DiagnosticsController`, `SyncStatusController`, and `SyncStatusViewModel`.

**5. Test doubles and existing tests** - Update the `using` statements in every test file and test double listed in the spec, with no assertion changes.

### Stage 3: Full verification

**6. Full verification** - Run a full solution build and the full test suite (with coverage settings) to confirm no project's behavior changed and the solution remains in a deployable state.
