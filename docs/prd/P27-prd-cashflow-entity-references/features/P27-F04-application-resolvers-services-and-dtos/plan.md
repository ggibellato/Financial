# Implementation Plan: F04. Application Resolvers, Services, and DTOs

**Prerequisites:**
- .NET SDK matching the existing `Financial.CashFlow.*`/`Financial.Api`/`Financial.App` projects
- No new NuGet packages or environment variables

### Stage 1: Resolvers and DTO Shapes

**1. Id-Based Resolvers** - Change `BankNameResolver` and `IncomeSourceNameResolver` to resolve by `Guid` instead of name, and add the new `InvestmentAccountResolver` mirroring the same shape.

**2. DTO Field Changes** - Update every Create/Update/Read DTO for Income, Expense, Transfer, BalanceAdjustment, InvestmentSnapshot, Bank, and the card-statement payment DTO to submit and return Ids, with read DTOs also carrying the denormalized display name for each reference.

### Stage 2: Service Layer

**3. Create/Update Path Resolution** - Update `IncomeService`, `ExpenseService`, `TransferService`, `BalanceAdjustmentService`, and `CardStatementService` so their create/update paths resolve the submitted Id via the appropriate resolver and pass the resolved object into the Domain factory, rejecting an unresolvable Id the same way the current name-based validation does.

**4. Bank-Scoped Query and Mapper Updates** - Change `TransferService.GetTransfersByBank`, `BalanceAdjustmentService`'s bank-scoped methods, and `BankService.UpdateOpeningBalanceAsync`/`GetBankBalanceAsOf` to take a `Guid` instead of a name; update every affected `ToDto` mapper to emit both the Id and denormalized name for each reference; update `InvestmentSnapshotService` to use the new `InvestmentAccountResolver`.

### Stage 3: Compile-Preserving Ripple

**5. Spreadsheet Import and Web API Minimal Fixes** - Give `MonthlyExpenseSheetImporter` its own inline by-name Bank lookup now that the shared resolver is Id-based, and update `BanksController`/`TransfersController` to resolve their existing name-based route segments to the Guid the Application layer now requires, keeping every route and response shape externally unchanged.

**6. WPF Minimal Fix** - Update `MonthlyViewModel` so every place it builds a create/update request or calls a bank-scoped service method resolves the already-selected name to the required Guid against the already-loaded Banks/income-source lists, and every place it reads a DTO's reference field switches to the new denormalized name property - forms, bindings, and displayed values stay exactly as they are today.

### Stage 4: Test Coverage

**7. Resolver and Service Test Updates** - Update the resolver and service unit tests to the Id-based signatures, add tests for the new `InvestmentAccountResolver`, and confirm the bank-scoped query methods return the same results as their pre-change name-based equivalents for a fixed set of test records.

**8. Integration Test Confirmation** - Confirm the existing Web API integration tests for banks, transfers, and balance adjustments still pass unchanged, proving the external route/JSON contract was not altered by this feature.
