# Implementation Plan: Web Dynamic Reserve Bucket UI

**Prerequisites:**
- F03 (income-split computation, dynamic `IncomeSplitResultDto` shape on the backend) merged
- F04 (bucket balances for all buckets) merged
- F05 (`GET /reserve-buckets` endpoint) merged

### Stage 1: API Contract

**1. Types and client method** - Add `ReserveBucketDto` and `BucketSplitAmountDto` to `types.ts`, correct `IncomeSplitResultDto` to the `{ buckets, total }` shape, and add `getReserveBuckets()` to `financialApiClient.ts` following the `getBanks`/`getIncomeSources` pattern.

### Stage 2: Hook

**2. Dynamic bucket state in `useReserva`** - Remove `RESERVE_BUCKETS`, fetch buckets via `Promise.allSettled` alongside balances/movements (buckets-only failure degrades to an empty list instead of the full-page error), default `withdrawalBucket` from the fetched list, compute `splitPercentageWarning`, and add bucket-required validation to `submitWithdrawal`/`saveMovementEdit`.

### Stage 3: Page

**3. Dynamic UI in `ReservaPage`** - Replace the two hardcoded dropdowns with a map over fetched buckets, replace the 4 hardcoded split-result rows with a map over `lastSplitResult.buckets`, and render the split-imbalance warning banner.

### Stage 4: Tests

**4. Update and extend tests** - Update `useReserva.test.ts` and `ReservaPage.test.tsx` fixtures/mocks for the new `getReserveBuckets` call and `IncomeSplitResultDto` shape; add coverage for dynamic dropdowns (including inactive buckets), the dynamic split-result table, the warning banner (balanced/unbalanced/no-data), and the bucket-required validation/fetch-failure-degradation paths.
