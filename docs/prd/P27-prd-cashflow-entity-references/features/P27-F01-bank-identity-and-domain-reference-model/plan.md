# Implementation Plan: F01. Bank Identity and Domain Reference Model

**Prerequisites:**
- .NET SDK matching the existing `Financial.CashFlow.*` projects
- No new NuGet packages or environment variables

### Stage 1: Domain Entities

**1. Bank Identity** - Give `Bank` a `Guid Id`, assigned in `Create`, exactly mirroring how `IncomeSource`/`InvestmentAccount` already assign theirs.

**2. Income and Expense Reference Conversion** - Convert `Income.IncomeSource`/`Income.Bank` and `Expense.PaymentSource` (renamed `PaymentSourceBank`) from strings to real object references, updating every factory method and internal state check (`PaymentStatus`, `Settle`, `Unsettle`, `ValidatePaymentShape`) that currently keys off the string.

**3. Transfer, BalanceAdjustment, and InvestmentSnapshot Reference Conversion** - Convert `Transfer.SourceBank`/`DestinationBank`, `BalanceAdjustment.Bank`, and `InvestmentSnapshot.Account` from strings to real object references, updating the "source and destination must differ" check to compare by `Id`.

### Stage 2: Application Layer Call Sites

**4. Bank-Balance and CRUD Service Updates** - Update `BankService`, `IncomeService`, `ExpenseService`, `TransferService`, `BalanceAdjustmentService`, and `InvestmentSnapshotService` so every place that used to flatten a resolved entity down to its name now passes the resolved object straight into the Domain factory, and every string-equality comparison against a reference field becomes an `Id` comparison.

**5. Migration and Import Tool Updates** - Update `BankMigrator`, `IncomeSourceMigrator`, and `InvestmentAccountMigrator`'s audit logic to compare by `Id` instead of by name, and update `IncomeBackfillImporter` and `MonthlyExpenseSheetImporter` to resolve a raw name against the seeded collections before constructing an `Income`/`Expense`, keeping both tools compiling without changing their existing name-resolution behavior.

### Stage 3: Test Coverage

**6. Domain Entity Tests** - Update the existing unit tests for `Bank`, `Income`, `Expense`, `Transfer`, `BalanceAdjustment`, and `InvestmentSnapshot` to construct real `Bank`/`IncomeSource`/`InvestmentAccount` fixtures instead of plain strings, and add `Bank`'s missing "Id is assigned / two banks have different Ids" tests.

**7. Application and Migration Test Updates** - Update the Application service tests and migration/import tool tests to use object-reference fixtures, confirming every computed figure (bank balances, round-up eligibility, transfers-by-bank, adjustments-by-bank, migration audits) is unchanged from before the conversion.
