# Implementation Plan: F01. Expense Payment-Date Domain Model Rework

**Prerequisites:**
- .NET SDK matching the solution's target framework (existing `Financial.sln` toolchain — no new tooling)
- No new environment variables or configuration files

### Stage 1: Domain Entity Rework

**1. Expense Entity Rework** - Remove the `SettledAt` field from `Expense`, add `ChargeDate` and `InvoiceDate`, update `Create` so both are populated for any expense with a card tag (defaulting `InvoiceDate` to the charge month, normalized to day 1), rewrite `Settle` and `Unsettle` to swap `Date` instead of writing/clearing `SettledAt`, and add a guarded `SetInvoiceDate` method. Reference the spec's §3 Technical Decisions and §6 Data Model for the exact field behavior.

**2. Domain Unit Tests** - Rewrite the existing `Settle`/`Unsettle` test cases in `ExpenseTests.cs` for the new `Date`-swap behavior, and add coverage for `ChargeDate`/`InvoiceDate` creation defaults, month-normalization, and every `SetInvoiceDate` guard case. Reference the spec's §7 Testing Strategy for the full function list.

### Stage 2: Downstream Compile Fixes (Application Layer)

**3. Application Layer Cleanup** - Remove `SettledAt` from `ExpenseDTO` and its mapping in `ExpenseService`, and adjust `CardStatementService`'s unmark-paid rollback snapshot to capture the pre-unsettle `Date` instead of the removed field, without touching the existing settlement matching key. Reference the spec's §3 and §4 for scope boundaries against F02.

**4. Application and Infrastructure Test Fixes** - Update `ExpenseServiceTests`, `CardStatementServiceTests`, and `CashFlowSerializerAdapterTests` to drop every `SettledAt` reference and assert the new `Date`/`ChargeDate`/`InvoiceDate` behavior where relevant. Update `ExpenseEndpointsTests` and `CardStatementsEndpointsTests` the same way.

### Stage 3: Retire the Obsolete P12 Migrator

**5. Remove ExpensePaymentStateMigrator** - Delete the migrator class and its companion summary type, remove its invocation and console output from the import tool's `Program.cs`, and delete its dedicated test file, per the reasoning in the spec's §3 Technical Decisions.

**6. Fix Remaining Stray References** - Update the one stray `SettledAt` reference in `MonthlyExpenseSheetImporterTests.cs` so the Integrations test project compiles; no new import-time `ChargeDate`/`InvoiceDate` behavior is added here (that is F08's scope).

### Stage 4: Full Verification

**7. Full Solution Build and Test Pass** - Build and test every affected project (Domain, Application, Infrastructure, Api, CashFlowSpreadsheetImport, and their test counterparts) to confirm there are zero remaining `SettledAt` references anywhere in the solution and every suite passes.
