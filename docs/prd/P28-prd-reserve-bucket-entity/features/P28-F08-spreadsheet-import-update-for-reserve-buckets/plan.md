# Implementation Plan: Spreadsheet Import Update for Reserve Buckets

**Prerequisites:**
- F01 (ReserveBucket entity + seed migration) merged
- F02 (ReserveMovement entity reference + `ReserveBucketNameResolver`) merged — already wired `Program.cs`'s migrator ordering and `ReservasSheetImporter`'s name resolution as a minimal compile-time adaptation

### Stage 1: Importer

**1. Soft-fail unresolved bucket columns** - Change `ReservasSheetImporter.Import` to accept an `ImportReport`, and change `ResolveBucketColumns` to log a `ValidationWarning` and drop the column instead of throwing when a column's expected bucket name isn't seeded. Update `Program.cs`'s single call site to pass `report` through.

### Stage 2: Tests

**2. Update test coverage** - Replace the throw-based unresolved-bucket test with one asserting the soft-fail behavior (no movements for the unresolved column, other columns unaffected, warning recorded).
