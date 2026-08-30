# Implementation Plan: F06. Category CRUD

**Prerequisites:**
- F01 (Admin Navigation Foundation) merged — provides the Admin > CashFlow > Categories nav leaf and placeholder route.
- `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` and `npm run generate-api-types` (Financial.Web) available for the API-contract phase.

### Stage 1: Domain and Application

**1. Category domain update rule** - Add an `Update` method to `Category` that sets Name/Active/IsInvestment/IsTithe, rejecting a blank name, mirroring `Bank.Update`. Add `RemoveCategory` to `CashFlowData`, mirroring `RemoveBank`.

**2. Category repository plumbing** - Add `AddCategory`/`DeleteCategory` to `ICashFlowRepository` and implement them in `CashFlowJsonRepository`, mirroring the existing Bank pair.

**3. Category Application service and DTOs** - Extend `ICategoryService`/`CategoryService` with Create/Update/Delete, name-uniqueness and reference-guard checks (Category referenced by an Expense), and a `HasReferences`-bearing `CategoryDTO`. Add `CategoryCreateDTO`/`CategoryUpdateDTO`.

### Stage 2: API and Contract

**4. Categories API endpoints** - Extend `CategoriesController` with POST/PUT/DELETE following the existing `BanksController` conventions, including 400/404/409 responses.

**5. OpenAPI contract regeneration** - Regenerate the pinned OpenAPI snapshot and the generated frontend TypeScript types, and confirm `tsc -b` is clean.

### Stage 3: Web UI

**6. Categories admin screen (Web)** - Build `CategoriesPage`, `CategoryFormDialog`, and `useCategories`, following the Bank admin screen's structure, states (loading/empty/validation/server-error/saving/success), and Fluent UI components. Wire the Categories nav leaf to this page in place of the F01 placeholder.

### Stage 4: WPF UI

**7. Categories admin screen (WPF)** - Build `CategoriesViewModel`, `CategoryFormDialogViewModel`, `CategoriesView`, and `CategoryFormDialog`, mirroring the Web screen's workflow, field order, and validation. Register the view in `MainWindow.xaml.cs`.

### Stage 5: Verification

**8. Cross-feature and final verification** - Add integration coverage proving a Category referenced by an Expense blocks deletion and an unreferenced one deletes cleanly. Run the full solution build and test suite (all .NET projects, Financial.Web lint/build/vitest) and confirm every F06 acceptance criterion holds before marking the feature complete.
