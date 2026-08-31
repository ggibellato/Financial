# Implementation Plan: F09. Investment Account CRUD

**Prerequisites:**
- F01 (Admin Navigation Foundation) merged — provides the Admin > CashFlow > Investment Accounts nav leaf and placeholder route.
- `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` and `npm run generate-api-types` (Financial.Web) available for the API-contract phase.

### Stage 1: Domain and Application

**1. InvestmentAccount domain update rule** - Add `InvestmentAccount.Update(name, isActive, isLiability, aliases)`, reusing the existing `AddAlias` method's blank-guard and case-insensitive dedup for a full-replace of the alias list. Add `RemoveInvestmentAccount` to `CashFlowData`, mirroring `RemoveIncomeSource`.

**2. InvestmentAccount repository plumbing** - Add `DeleteInvestmentAccount` to `ICashFlowRepository` and implement it in `CashFlowJsonRepository` (the `Add`/`Get` pair already exists).

**3. InvestmentAccount Application service and DTOs** - Extend `IInvestmentAccountService`/`InvestmentAccountService` with Create/Update/Delete, a name-uniqueness check, and a `GetLatestBalance` helper (most recent `InvestmentSnapshot` by Year/Month, defaulting to 0) driving both `InvestmentAccountDTO.LatestBalance` and the delete guard (`EntityInUseException` when non-zero). Add `InvestmentAccountCreateDTO`/`InvestmentAccountUpdateDTO`; extend `InvestmentAccountDTO` with `Aliases` and `LatestBalance`.

### Stage 2: API and Contract

**4. Investment Accounts API endpoints** - Extend `InvestmentAccountsController` with POST/PUT/DELETE, following the established `BanksController`/`IncomeSourcesController` conventions, including 400/404/409 responses; update its class/GET XML doc since it is no longer read-only.

**5. OpenAPI contract regeneration** - Regenerate the pinned OpenAPI snapshot and the generated frontend TypeScript types, and confirm `tsc -b` is clean.

### Stage 3: Web UI

**6. Investment Accounts admin screen (Web)** - Build a new `AliasesInput` component (Fluent `TagGroup`/`InteractionTag` chips with an add `Input`+`Button`, case-insensitive dedup mirroring the domain rule) since no tag-style input exists yet in this codebase. Build `InvestmentAccountsPage`, `InvestmentAccountFormDialog`, and `useInvestmentAccounts`, following the Bank/IncomeSource admin screens' structure, states (loading/empty/validation/server-error/saving/success), and Fluent UI components, showing `latestBalance` in the list and the delete-confirmation dialog. Add the four missing `financialApiClient.ts` methods. Wire the Investment Accounts nav leaf to this page in place of the F01 placeholder.

### Stage 4: WPF UI

**7. Investment Accounts admin screen (WPF)** - Build a chip-list Aliases control (an `ItemsControl` of removable items in a `WrapPanel` plus an add `TextBox`/`Button`) mirroring the Web interaction, since no tag/chip control exists in WPF-UI either. Build `InvestmentAccountsViewModel`, `InvestmentAccountFormDialogViewModel` (with `AddAliasCommand`/`RemoveAliasCommand`), `InvestmentAccountsView`, and `InvestmentAccountFormDialog`, mirroring the Web screen's workflow, field order, and validation. Add `ShowInvestmentAccountFormDialog` to `IDialogService`/`DialogService`. Register the view in `MainWindow.xaml.cs`/`App.xaml.cs`.

### Stage 5: Verification

**8. Cross-feature and final verification** - Remove the now-obsolete `InvestmentAccounts_UnsupportedVerbs_DoNotSucceed` test and add full CRUD integration coverage proving an account with a non-zero latest `InvestmentSnapshot` value blocks deletion (seeded via the existing `/investment-snapshots/{year}/{month}` GET+PUT flow) while a zero-or-no-snapshot account deletes cleanly. Run the full solution build and test suite (all .NET projects, Financial.Web lint/build/vitest) and confirm every F09 acceptance criterion holds before marking the feature complete.
