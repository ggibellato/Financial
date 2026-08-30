# Implementation Plan: F04 Asset CRUD

**Prerequisites:** None beyond what F01-F03 already established (Admin nav placeholder route, Fluent Table/Dialog conventions, `IDialogService`).

### Stage 1: Domain Rules

**1. Asset/Portfolio identity rules** - Add `Asset.UpdateIdentity` and `Portfolio.RegisterAsset`/`Portfolio.UpdateAssetIdentity`, each enforcing the same-portfolio duplicate-name guard, with unit tests.

### Stage 2: Application, API, Contract

**2. Asset Admin service, DTOs, ISIN validator** - Add `IAssetAdminService`/`AssetAdminService` (list/create/update), the ISIN format validator, and DI registration.

**3. AssetsController endpoints + OpenAPI** - Extend `AssetsController` with list/create/update endpoints; regenerate the OpenAPI snapshot and frontend generated types.

### Stage 3: Web UI

**4. Asset list, create/edit, delete-confirm (Web)** - Add `AssetsPage`, `AssetFormDialog` (Broker→Portfolio cascading picker, identity fields, ISIN inline validation), and `useAssets`, wired into the existing `admin/investment/assets` placeholder route.

### Stage 4: WPF UI

**5. Asset list, create/edit, delete-confirm (WPF)** - Mirror the Web screens as `AssetsView`/`AssetFormDialog` + view-models under `Views/Admin`/`ViewModels/Admin`.

### Stage 5: Verification

**6. Cross-feature E2E coverage + final verification** - Add/extend E2E tests proving the F03→F04 picker scoping and the F04→F03 delete-guard cross-feature criteria, then run the full solution/frontend validation suite and update the PRD acceptance-criteria checkboxes.
