# Implementation Plan: F02 Broker CRUD

**Prerequisites:**
- No new libraries — reuses existing xUnit/FluentAssertions, Vitest/RTL, and WPF-UI tooling already in the solution.
- No environment/configuration changes.

### Stage 1: Domain

**1. Broker and Investments mutations** - Add `Broker.Update(name, currency)`, and `Investments.CreateActiveBroker`, `RenameBroker`, and `DeleteBroker`, covering the uniqueness-across-both-scopes rule, the Active→Historic archive-on-delete behavior (including the same-name-already-in-Historic edge case), and the Historic hard-delete case, per the spec's Component Overview.

### Stage 2: Application and API

**2. Broker Application service and DTOs** - Add `BrokerDTO`/`BrokerCreateDTO`/`BrokerUpdateDTO` and `IBrokerService`/`BrokerService`, following the existing `PortfolioService`/`MensaisService` conventions (repository-scoped mutation inside `ApplyAndSaveAsync`, tracing spans, required-field guards), and register it in DI.

**3. Brokers REST endpoints** - Add `BrokersController` (`GET/POST /brokers`, `PUT/DELETE /brokers/{name}`), relying on the existing `DomainExceptionMappingMiddleware` for 404/409 mapping.

**4. OpenAPI snapshot and frontend types** - Regenerate `Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json` and `Financial.Web/src/api/generated/openapi.ts`, reviewing the diff for the new Broker contract.

### Stage 3: Financial.Web

**5. API client and data hook** - Extend `financialApiClient.ts` with the four Broker calls and add `useBrokers.ts`, covering loading, saving, and error states.

**6. Brokers list and dialogs** - Add `BrokersPage.tsx` (list, create action, per-row edit/delete) and `BrokerFormDialog.tsx` (shared create/edit form), including the delete-confirmation dialog's Active→Historic vs. permanent-removal wording and its disabled state when the broker still has portfolios.

**7. Route wiring** - Point the existing `admin/investment/brokers` route at `BrokersPage` instead of the F01 placeholder, with no other route/nav changes.

### Stage 4: Financial.App

**8. Broker admin ViewModels** - Add `BrokersViewModel` and `BrokerFormDialogViewModel`, matching the existing `MoveAssetDialogViewModel` shape for the form and the app's existing async-command-plus-`IDialogService` pattern for list actions.

**9. Broker admin views and dialog service** - Add `BrokersView`/`BrokerFormDialog` XAML, extend `IDialogService`/`DialogService` with `ShowBrokerFormDialog`, and replace the `admin-brokers` placeholder registration in `MainWindow.xaml.cs` with the real view.

### Stage 5: Cross-Platform Verification

**10. Regression and parity check** - Confirm the F01 placeholder for every other Admin entity is untouched, the nav/route sync test still passes, and both platforms present the same fields, validation, and delete-guard behavior for Broker.
