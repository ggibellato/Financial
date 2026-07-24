# Implementation Plan: F03. Expense Form Bank Picker & Round-Up UX

**Prerequisites:**
- F01 merged (`Bank` entity, `ICashFlowRepository.GetBanks()`) and F02 merged (`Expense.RoundUpAmount`/`RoundUpSuggestion`, DTO fields)
- .NET SDK and Node toolchain; no new packages

### Stage 1: Backend Banks Endpoint

**1. Bank read model and service** - Add the DTO and a thin service that maps the repository's bank list to it, following the same constructor-injected, singleton-registered pattern as every other list-returning service in this layer. See spec Sections 3 and 4.

**2. Banks controller** - Add the read-only endpoint exposing the bank list, following the existing controller conventions (route naming, `ProducesResponseType`, no business logic). See spec Section 5.

### Stage 2: Frontend Data Layer

**3. Frontend types and API client** - Add the bank type and the round-up fields to the expense types, and the new `getBanks` client method, matching the existing simple GET-list pattern. See spec Sections 4 and 5.

**4. `useMonthly` bank state and form fields** - Fetch banks alongside the existing month data, replace the hardcoded bank list export with the fetched state everywhere it's used (bank pickers, bank totals), and add the round-up amount fields to the create/edit form state. See spec Sections 3 and 4.

### Stage 3: Form UX

**5. Bank pickers and round-up field** - Wire the expense form's bank picker and the card-statement mark-paid picker to the fetched bank list, and add the round-up input with its visibility rule, create-time auto-suggestion, and edit-time saved-value pre-fill. See spec Sections 3 and 4.

**6. Submit wiring and client-side validation** - Update the create/update submit logic to send the round-up amount under the full-replace rule, including the mode/eligibility-based nulling and the client-side range check. See spec Section 3.

### Stage 4: Verification

**7. Full-solution verification** - Run the complete .NET and frontend test suites, and exercise the expense form manually (bank picker, round-up field appearance/disappearance, create and edit flows) against a running instance to confirm the UX matches the spec.
