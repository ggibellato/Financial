# Implementation Plan: F03. Debounced Storage Decorator

**Prerequisites:**
- F01 (Sync Status Data Shape) and F02 (Transient-Failure Retry Helper), both already implemented in `Financial.Shared.Infrastructure`

### Stage 1: Status/Flush Contract

**1. ISyncStatusProvider** - Add the narrow interface (query current status, force a flush) that `DebouncedJsonStorage` will implement and that later features (F06, F08, F11) will depend on instead of the concrete decorator type.

### Stage 2: Debounced Storage Decorator Core

**2. Dirty Tracking and Debounce Cycle** - Implement `DebouncedJsonStorage`'s `WriteAsync`, queuing the latest JSON and returning immediately, with a debounce-then-save cycle that resets on each new write and defers to the in-flight save (rather than starting a second one) when a write arrives mid-save, per the spec's generation-counter mechanism.
**3. Save Execution and Status Transitions** - Wire the debounce cycle's save step through F02's retry executor against the wrapped storage, updating F01's status shape through `Pending` → `Saving` → `Idle`/`Failed`, including the no-auto-retry-after-failure behavior and the auto-continue-if-still-dirty-after-success behavior described in the spec.
**4. ReadAsync Passthrough** - Implement `ReadAsync()` as a direct passthrough to the wrapped storage.

### Stage 3: Flush Primitive

**5. FlushAsync** - Implement the bounded, debounce-bypassing flush described in the spec, including its behavior when a save is already in-flight and when the bound is exceeded.
**6. Test Seams** - Add the internal constructor overload exposing `maxRetries` and the flush timeout for test speed, per the spec's testability decision, without changing the public constructor's contract.

### Stage 4: Tests

**7. Test Double** - Add `ControllableJsonStorage`, the gated/failure-injecting `IJsonStorage` test double described in the spec's testing strategy.
**8. Behavioral Coverage** - Add `DebouncedJsonStorageTests` covering every F03 acceptance criterion and the F01/F02 cross-feature integration criterion, per the spec's testing strategy.
