# Implementation Plan: F04. CashFlow Debounced Wiring

**Prerequisites:**
- F03 (Debounced Storage Decorator), already implemented in `Financial.Shared.Infrastructure`

### Stage 1: Wiring

**1. Wrap GoogleDrive Storage in the Factory** - In `CashFlowRepositoryFactory.CreateStorage`, wrap the `GoogleDriveJson` branch's result in a `DebouncedJsonStorage` with a 10-second debounce window, per the spec. Leave the `LocalJson` branch untouched.
**2. CashFlowJsonRepository Implements ISyncStatusProvider** - Add the interface implementation, delegating `GetStatus()`/`FlushAsync()` to `_storage` when it's an `ISyncStatusProvider`, with the `Idle`/no-op fallback described in the spec for the unwrapped case.

### Stage 2: Tests

**3. Factory Wrapping Coverage** - Extend `CashFlowRepositoryFactoryTests` with the GoogleDrive-wrapping behavioral tests (non-blocking save, `Pending` status after a write) and the LocalJson `Idle`-status test, per the spec's testing strategy.
**4. Repository Delegation Coverage** - Extend `CashFlowJsonRepositoryTests` with the `ISyncStatusProvider` delegation and fallback tests, per the spec's testing strategy. Confirm the existing `LocalJsonStorage`-backed tests in this file still pass unmodified.
