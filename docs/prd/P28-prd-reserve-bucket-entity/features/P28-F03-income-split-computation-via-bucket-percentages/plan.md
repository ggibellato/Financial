# Implementation Plan: Income Split Computation via Bucket Percentages

**Prerequisites:**
- F01 (ReserveBucket entity + seed migration) and F02 (ReserveMovement entity reference) merged

### Stage 1: Domain Split Math

**1. ReserveBucket.CalculateSplitAmount** - Add the percentage-based split calculation to the entity itself, and delete `ReserveSplitCalculator`/`ReserveSplitResult` now that nothing needs them.

### Stage 2: Application Response Shape

**2. IncomeSplitResultDTO Reshape** - Replace the 4 fixed fields with a per-bucket list DTO plus a total, sized to however many buckets actually participate.

### Stage 3: Income Split Computation

**3. PostIncomeSplitAsync Rewrite** - Iterate active buckets from the repository instead of the 4 hardcoded canonical names, compute each bucket's share via the new domain method, and reject the request when no bucket is active.

### Stage 4: Tests

**4. Domain and Service Tests** - Add `ReserveBucket.CalculateSplitAmount` coverage, update `ReserveServiceTests` for dynamic bucket participation and the no-active-buckets error path, and update `ReserveEndpointsTests` for the new response shape.
