# Implementation Plan: F05. Investment Debounced Wiring

**Prerequisites:**
- F03 (Debounced Storage Decorator), already implemented in `Financial.Shared.Infrastructure`
- F04 (CashFlow Debounced Wiring), already implemented — this feature mirrors its exact pattern for Investment

### Stage 1: Wiring

**1. Wrap GoogleDrive Storage in the Factory** - In `RepositoryFactory.CreateStorage`, wrap the `GoogleDriveJson` branch's result in a `DebouncedJsonStorage` with a 10-second debounce window, mirroring F04. Leave the `LocalJson` branch untouched.
**2. JSONRepository Implements ISyncStatusProvider** - Add the interface implementation, delegating `GetStatus()`/`FlushAsync()` to `_storage` when it's an `ISyncStatusProvider`, with the `Idle`/no-op fallback described in the spec for the unwrapped case.

### Stage 2: Tests

**3. Factory Wrapping Coverage** - Add a stub `IRemoteFileClient`/`IRemoteFileClientFactory` pair to `RepositoryFactoryTests.cs` (this file currently only exercises the real Google client, which never reaches a successfully-constructed repository), then add the GoogleDrive-wrapping behavioral tests (non-blocking save, `Pending` status after a write, two-instance isolation) and the LocalJson `Idle`-status test, per the spec's testing strategy.
**4. Repository Delegation Coverage** - Extend `JsonRepositoryTests.cs` with the `ISyncStatusProvider` delegation and fallback tests, per the spec's testing strategy. Confirm the existing tests in this file still pass unmodified.
