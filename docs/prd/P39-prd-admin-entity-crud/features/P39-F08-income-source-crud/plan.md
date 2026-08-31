# Implementation Plan: F08. Income Source CRUD

**Prerequisites:**
- F01 (Admin Navigation Foundation) merged — provides the Admin > CashFlow > Income Sources nav leaf and placeholder route.
- `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` and `npm run generate-api-types` (Financial.Web) available for the API-contract phase.

### Stage 1: Domain and Application

**1. IncomeSource domain update rule** - Add `IncomeSource.Update(name, group, isActive, autoSplitToReserve)`, reusing `Create`'s blank-name guard. Add `RemoveIncomeSource` to `CashFlowData`, mirroring `RemoveBank`/`RemoveCategory`.

**2. IncomeSource repository plumbing** - Add `AddIncomeSource`/`DeleteIncomeSource` to `ICashFlowRepository` and implement them in `CashFlowJsonRepository`, mirroring the existing Bank/Category pairs.

**3. IncomeSource Application service and DTOs** - Extend `IIncomeSourceService`/`IncomeSourceService` with Create/Update/Delete, name-uniqueness and reference-guard checks (IncomeSource referenced by an Income), and a `HasReferences`-bearing `IncomeSourceDTO`. Add `IncomeSourceCreateDTO`/`IncomeSourceUpdateDTO`, parsing `Group` from string via `Enum.Parse<IncomeGroup>`.

### Stage 2: API and Contract

**4. Income Sources API endpoints** - Extend `IncomeSourcesController` with POST/PUT/DELETE, following the established `BanksController`/`CategoriesController` conventions, including 400/404/409 responses; update its class/GET XML doc since it is no longer read-only.

**5. OpenAPI contract regeneration** - Regenerate the pinned OpenAPI snapshot and the generated frontend TypeScript types, and confirm `tsc -b` is clean.

### Stage 3: Web UI

**6. Income Sources admin screen (Web)** - Build `IncomeSourcesPage`, `IncomeSourceFormDialog` (Name field, Group dropdown, Active + AutoSplitToReserve toggles), and `useIncomeSources`, following the Bank/Category admin screens' structure, states (loading/empty/validation/server-error/saving/success), and Fluent UI components. Add `createIncomeSource`/`updateIncomeSource`/`deleteIncomeSource` to `financialApiClient.ts`. Wire the Income Sources nav leaf to this page in place of the F01 placeholder.

### Stage 4: WPF UI

**7. Income Sources admin screen (WPF)** - Build `IncomeSourcesViewModel`, `IncomeSourceFormDialogViewModel` (with a `GroupOptions`/`Group` ComboBox binding mirroring `AssetFormDialog`'s string-backed pattern), `IncomeSourcesView`, and `IncomeSourceFormDialog`, mirroring the Web screen's workflow, field order, and validation. Add `ShowIncomeSourceFormDialog` to `IDialogService`/`DialogService`. Register the view in `MainWindow.xaml.cs`.

### Stage 5: Verification

**8. Cross-feature and final verification** - Remove the now-obsolete `IncomeSources_UnsupportedVerbs_DoNotSucceed` test and add full CRUD integration coverage proving an IncomeSource referenced by an Income blocks deletion and an unreferenced one deletes cleanly. Run the full solution build and test suite (all .NET projects, Financial.Web lint/build/vitest) and confirm every F08 acceptance criterion holds before marking the feature complete.
