# Implementation Plan: F11. Reserve Bucket CRUD

**Prerequisites:**
- F01 (Admin Navigation Foundation) merged — provides the Admin > CashFlow > Reserve Buckets nav leaf and placeholder route.
- `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` and `npm run generate-api-types` (Financial.Web) available for the API-contract phase.
- This is the final feature of the P39 Admin Entity CRUD PRD.

### Stage 1: Domain and Application

**1. ReserveBucket domain update rule** - Add `ReserveBucket.Update(name, splitPercentage, isActive)`, reusing `Create`'s blank-name and 0-100 range guards. No remove/deactivate method is added — "delete" is an ordinary Update call with `isActive: false`.

**2. ReserveBucket repository plumbing** - Add `AddReserveBucket` to `ICashFlowRepository` (missing today even though `CashFlowData.AddReserveBucket` already exists) and implement it in `CashFlowJsonRepository`. No delete member is added.

**3. ReserveBucket Application service and DTOs** - Extend `IReserveBucketService`/`ReserveBucketService` with Create/Update, a name-uniqueness check, and a `ComputeActiveSplitWarning` helper (sums `SplitPercentage` across active buckets post-save, ±0.01 tolerance of 100, returning a message naming the actual total when outside tolerance). Add `ReserveBucketCreateDTO`/`ReserveBucketUpdateDTO`; extend `ReserveBucketDTO` with a nullable `Warning`, reusing the established pattern from `CardStatementDTO.Warning`.

### Stage 2: API and Contract

**4. Reserve Buckets API endpoints** - Extend `ReserveBucketsController` with POST/PUT (no DELETE), following the established `BanksController`/`InvestmentAccountsController` conventions, including 400/409/404 responses; update its class/GET XML doc since it is no longer read-only.

**5. OpenAPI contract regeneration** - Regenerate the pinned OpenAPI snapshot and the generated frontend TypeScript types, and confirm `tsc -b` is clean.

### Stage 3: Web UI

**6. Reserve Buckets admin screen (Web)** - Build `ReserveBucketsPage` (Fluent `Table` plus a persistent `MessageBar intent="warning"` banner above it showing the current active-bucket split total whenever it isn't ~100%, computed client-side from the fetched list), `ReserveBucketFormDialog` (Name, SplitPercentage, Active toggle; on save, shows the response's `Warning` as a non-blocking notice without blocking the dialog's close), and `useReserveBuckets` (list/create/update, no delete action — the page's "delete" button calls `updateReserveBucket` with `isActive: false`). Add `createReserveBucket`/`updateReserveBucket` to `financialApiClient.ts`. Wire the Reserve Buckets nav leaf to this page in place of the F01 placeholder. Leave the existing `ReservaPage`/`useReserva.ts` untouched.

### Stage 4: WPF UI

**7. Reserve Buckets admin screen (WPF)** - Build `ReserveBucketsViewModel` (list VM with its own `SplitPercentageWarning` computed property, no Delete command — the row action calls Update with `IsActive=false`), `ReserveBucketFormDialogViewModel`, `ReserveBucketsView`, and `ReserveBucketFormDialog`, mirroring the Web screen's workflow, field order, and validation. Add `ShowReserveBucketFormDialog` to `IDialogService`/`DialogService`. Register the view in `MainWindow.xaml.cs`/`App.xaml.cs`. Leave the existing `ReservaView`/`ReservaViewModel` untouched.

### Stage 5: Verification

**8. Cross-feature and final verification** - Remove the now-obsolete `ReserveBuckets_UnsupportedVerbs_DoNotSucceed` test and add full Create/Update coverage, including cases where the active-bucket split sum is and isn't ~100%, and a case proving a `ReserveMovement` created against a bucket that is later deactivated via Update keeps its reference valid. Run the full solution build and test suite (all .NET projects, Financial.Web lint/build/vitest) and confirm every F11 acceptance criterion holds before marking the feature — and the P39 PRD as a whole — complete.
