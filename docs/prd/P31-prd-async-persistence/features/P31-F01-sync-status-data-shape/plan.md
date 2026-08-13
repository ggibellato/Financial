# Implementation Plan: F01. Sync Status Data Shape

**Prerequisites:**
- None — this is a standalone data-contract feature with no dependencies

### Stage 1: Sync Status Contract

**1. SyncState and SyncStatus Types** - Add the `SyncState` enum and the immutable `SyncStatus` value type to a new `Sync/` folder in `Financial.Shared.Infrastructure`, per the spec's shape and folder placement decisions. Confirm neither type takes a dependency on either bounded context's projects.

**2. Unit Tests** - Add `SyncStatusTests` under `Tests/Financial.Shared.Infrastructure.Tests/Sync/` covering the enum's exact membership, construction with populated and null optional fields, and value equality, per the spec's testing strategy.
