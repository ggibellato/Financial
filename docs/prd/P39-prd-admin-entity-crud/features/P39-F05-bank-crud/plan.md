# Implementation Plan: F05 Bank CRUD

**Prerequisites:**
- No new libraries — reuses existing xUnit/FluentAssertions, Vitest/RTL, and WPF-UI tooling already in the solution.
- No environment/configuration changes.

### Stage 1: Domain

**1. Bank and CashFlowData mutations** - Add `Bank.Update(name, roundUpEnabled)` and `CashFlowData.RemoveBank(Guid id)`, per the spec's Component Overview.

### Stage 2: Application and API

**2. New `EntityInUseException`** - Add `Financial.CashFlow.Application.Exceptions.EntityInUseException`, and map it to 409 in `DomainExceptionMappingMiddleware`, alongside the existing `OverdraftConfirmationRequiredException`/`ReserveMovementLinkedToIncomeException` entries.

**3. Bank Application service and DTOs** - Add `BankCreateDTO`/`BankUpdateDTO`; extend `ICashFlowRepository` with `AddBank`/`DeleteBank` and `CashFlowJsonRepository` to implement them; extend `IBankService`/`BankService` with `CreateBankAsync` (name-uniqueness guard), `UpdateBankAsync` (uniqueness-excluding-self guard), and `DeleteBankAsync` (reference scan across `Income`/`Expense`/`Transfer`/`BalanceAdjustment`, throwing `EntityInUseException` on a hit), following the existing `BankService`/`MensaisService` conventions (repository-scoped mutation inside `ApplyAndSaveAsync`, tracing spans, required-field guards).

**4. Banks REST endpoints** - Extend `BanksController` with `POST /banks`, `PUT /banks/{id}`, `DELETE /banks/{id}`, relying on `DomainExceptionMappingMiddleware` for 404/409 mapping.

**5. OpenAPI snapshot and frontend types** - Regenerate `Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json` and `Financial.Web/src/api/generated/openapi.ts`, reviewing the diff for the new Bank create/update/delete contract.

### Stage 3: Financial.Web

**6. API client and data hook** - Extend `financialApiClient.ts` with `createBank`/`updateBank`/`deleteBank` and add `useBanks.ts`, covering loading, saving, and error states.

**7. Banks list and dialogs** - Add `BanksPage.tsx` (list, create action, per-row edit/delete) and `BankFormDialog.tsx` (shared create/edit form: Name + RoundUpEnabled toggle), including the delete-confirmation dialog's disabled state when the bank is still referenced.

**8. Route wiring** - Point the existing `admin/cashflow/banks` route at `BanksPage` instead of the F01 placeholder, with no other route/nav changes.

### Stage 4: Financial.App

**9. Bank admin ViewModels** - Add `BanksViewModel` and `BankFormDialogViewModel`, matching the existing `BrokerFormDialogViewModel` shape for the form and the app's existing async-command-plus-`IDialogService` pattern for list actions.

**10. Bank admin views and dialog service** - Add `BanksView`/`BankFormDialog` XAML, extend `IDialogService`/`DialogService` with `ShowBankFormDialog`, and replace the `admin-banks` placeholder registration in `MainWindow.xaml.cs` with the real view.

### Stage 5: Cross-Platform Verification

**11. Regression and parity check** - Confirm the F01 placeholder for every other Admin entity is untouched, the nav/route sync test still passes, both platforms present the same fields/validation/delete-guard behavior for Bank, and the full solution build + full test suite (not just new tests) is green.
