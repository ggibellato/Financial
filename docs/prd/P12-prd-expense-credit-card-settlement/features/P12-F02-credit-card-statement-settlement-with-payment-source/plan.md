# Implementation Plan: F02. Credit Card Statement Settlement with Payment Source

**Prerequisites:**
- F01 (Expense Payment State Model) merged — `Settle`/`Unsettle` transitions and nullable payment fields available
- .NET SDK and Node/npm for the two test stacks
- No new packages, configuration, or environment variables

### Stage 1: Application Settlement Cascades

**1. Mark-paid request contract** - Add the request DTO carrying the paying bank and extend the card statement service interface with the new mark/unmark operations. See spec Sections 4 and 5.

**2. Settle and unsettle cascades** - Implement the mark-paid cascade (validate the bank first, settle every eligible charge, flip the statement, save once) and the unmark-paid reverse cascade, both with full rollback on save failure. See spec Section 3 for cascade membership, date, and no-op rules.

**3. Outstanding-total rule** - Change the statement DTO's outstanding-total derivation to sum only unsettled charges. See spec Section 3 (Outstanding total).

### Stage 2: API Surface

**4. Controller endpoints** - Update the mark-paid endpoint to accept the request body and translate validation failures, and add the unmark-paid endpoint with the same error conventions. See spec Section 5 for status codes and side effects.

### Stage 3: Web Cards Panel

**5. Client contract** - Update the web API client and types for the mark-paid body and the new unmark operation. See spec Section 4 (Frontend).

**6. Cards panel controls** - Add per-row bank selection and the confirmation-guarded unmark control to the Cards panel, wiring both through the monthly hook with the existing re-fetch pattern. See spec Section 3 (Cards panel controls).

**7. Full-solution verification** - Run the .NET suite, web suite, and TypeScript build check to confirm everything is green end to end.
