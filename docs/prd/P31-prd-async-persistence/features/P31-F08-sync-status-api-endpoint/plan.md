# Implementation Plan: F08. Sync Status API Endpoint

**Prerequisites:**
- F04 (CashFlow Debounced Wiring) and F05 (Investment Debounced Wiring), both already implemented — provide the `ISyncStatusProvider`-castable `ICashFlowRepository`/`IRepository` this endpoint reads

### Stage 1: Contracts and Endpoint

**1. Response DTOs** - Add `SyncStatusDTO` (per-context shape) and `SyncStatusResponseDTO` (the combined `cashFlow`/`investment` wrapper) to `Financial.Api/DTOs`, per the spec's API contract.
**2. SyncStatusController** - Add the controller with a single `GET` action, resolving both contexts' status via the cast-and-fallback pattern and mapping each to a `SyncStatusDTO`, per the spec.

### Stage 2: Tests

**3. E2E Coverage** - Add `SyncStatusEndpointsTests` using `ApiTestFactory`, covering the default-Idle case, the camelCase JSON contract, and the per-context `Failed` case via a DI-swapped stub repository, per the spec's testing strategy.
