# Implementation Plan: Bank Balance Calculation Engine

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- No new external dependencies or environment variables

### Stage 1: Balance Formula Extension

**1. Shared Balance Calculation Helper** - Extract the existing per-bank balance formula in `BankService` into a single private helper that both the existing month-scoped balances method and a new as-of-date method call, then extend that formula with transfer amounts (added for the destination bank, subtracted for the source bank) and balance adjustment deltas, all scoped to the bank's opening-balance date through the requested as-of date.

**2. As-Of-Date Balance Method** - Add the new `IBankService` method for computing a single bank's balance as of an arbitrary date, including an optional way to exclude one specific balance adjustment's contribution from the sum.

### Stage 2: Balance Adjustment Refactor

**3. Balance Adjustment Service Refactor** - Replace `BalanceAdjustmentService`'s interim, duplicate balance calculation with a call to the new shared `BankService` method, removing the duplicate formula entirely so balance arithmetic exists in exactly one place.

**4. Regression and Edge-Case Coverage** - Re-verify every existing balance-adjustment delta test still passes unchanged through the new code path, and add coverage for editing an adjustment's date to a value earlier than its previous date, the scenario the prior interim approach could not have handled correctly.
