# Implementation Plan: F01. Optional Bank & Description for Income

**Prerequisites:**
- .NET SDK / existing Financial.slnx build toolchain
- Node/npm for Financial.Web
- No new libraries, no new environment variables, no data migration

### Stage 1: Domain and Application

**1. Income Entity** - Make `Bank` optional on the `Income` domain entity and add a new optional `Description` field, following the same nullable-navigation and length-validation approach already used by `Expense.PaymentSourceBank` and `Expense.Description`.

**2. Income DTOs and Service** - Update the Create/Update/Read DTOs so bank is optional and description is present, and update `IncomeService` to resolve the bank only when supplied, validate description length, normalize blank descriptions, and map a null bank through to the read DTO.

**3. Bank Balance Calculation** - Update `BankService`'s balance computation so an income with no bank is excluded from every bank's balance, mirroring how an unsettled expense with no payment source bank is already excluded.

### Stage 2: Presentation

**4. Web Income Form and List** - Update the Income create/edit form to make bank selection optional and add a description input, update the form's validation hook to drop the bank-required check and include description in the payload, and update the incomes list to display description and render a blank bank gracefully.

**5. Web Types** - Update the client-side Income DTO types to reflect the nullable bank fields and new description field.

**6. WPF Income Form and Grid** - Update the WPF income form to let the user select "no bank" (defaulting new incomes to no bank selected) and add a description field, update the corresponding validation helper to drop the bank-required check, and add description to the incomes grid.

### Stage 3: Testing

**7. Domain and Application Tests** - Update and extend Income entity and service tests to cover the now-optional bank and new description field, replacing any test that asserted the old bank-required behavior.

**8. Bank Balance and Tithe Tests** - Add coverage confirming a bank-less income is excluded from bank balance calculations and still included in the tithe calculation.

**9. API and WPF Presentation Tests** - Extend Income endpoint integration tests for the omitted-bank and over-length-description cases, and update the WPF validation helper's unit tests to match the relaxed bank rule.
