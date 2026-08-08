# Implementation Plan: Reserve Bucket Balances for All Buckets

**Prerequisites:**
- F01, F02, F03 merged

### Stage 1: Balance Computation

**1. GetBucketBalances Rewrite** - Iterate every seeded bucket from the repository instead of the 4 hardcoded canonical names, so inactive buckets remain visible with their historical balance and the row count isn't hardcoded. Remove the now-fully-unused canonical-name helper.

### Stage 2: Tests

**2. Balance Tests** - Add coverage for inactive-bucket inclusion and a non-4 bucket count; confirm existing balance tests still pass against the new data source.
