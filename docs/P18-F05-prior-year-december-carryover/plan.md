# Implementation Plan: F05. Prior-Year December Carryover for January

**Prerequisites:**
- .NET 10 SDK and Node/npm (existing solution targets)
- F01, F02, F03, F04 merged to main
- No new NuGet/npm packages, environment variables, or configuration files required
- Branch `feat/P18-F05-prior-year-december-carryover`, already created from `main`

### Stage 1: Backend Computation Change

**1. DTO Shape Update** - Widen the monthly-diffs field on both the per-account and net-position yearly diff read models from an 11-entry array to a 12-entry, nullable-capable array so January can carry a real value or an explicit absence.

**2. January Carryover Computation** - Update the yearly investment diff calculation to look up each account's December value from the prior year and derive January's diff from it, falling back to zero for an account not yet open in the prior year and to a blank value system-wide when no prior-year data exists at all, while leaving the February-through-December calculation untouched. Compute the aggregate net position's January diff as the sum of the resolved per-account January diffs, keeping it consistent with the accounts actually shown that year.

### Stage 2: Frontend Consumption Update

**3. Yearly Summary Page** - Stop constructing a client-side blank January cell and instead render whatever the API now returns for that month, updating the response typing to match, and adjust the average/sum summary figures to skip a blank January while including it whenever it has a real value.

### Stage 3: Test Suite Alignment

**4. Backend Test Updates** - Update the yearly summary service test suite for the new 12-entry diff shape and add coverage for the three January scenarios: prior-year data present, no prior-year data at all, and an account absent from an otherwise-populated prior year, plus a regression check that February-December are unchanged.

**5. Frontend Test Updates** - Replace the existing blank-January test with one asserting a real rendered January value, and add a case for a null-returning API response still rendering a blank cell.

**6. Full Suite Verification** - Run the complete backend and frontend test suites to confirm the new behavior and that nothing else regresses.
