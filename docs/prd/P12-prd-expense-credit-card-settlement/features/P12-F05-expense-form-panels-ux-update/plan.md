# Implementation Plan: F05. Expense Form & Panels UX Update

**Prerequisites:**
- F01 and F02 merged (expense payment-state contract and statement settlement endpoints live)
- Node/npm for `Financial.Web`; no new packages

### Stage 1: Form Mode State

**1. Hook mode state and payloads** - Add the per-form payment-mode state to the monthly hook, with mode-switch actions that clear the irrelevant field, mode-shaped create/edit payloads, the card-required check, and settled-expense read-only handling derived from the server-computed status. See spec Section 3.

### Stage 2: Form UI and Panel Contracts

**2. Form UI** - Render the mode radio control and conditionally show exactly one picker per mode, with the read-only payment display and explanatory note for settled expenses. See spec Section 4.

**3. Panel acceptance tests** - Add the bank-totals and form-mode tests that pin the panels and form to the PRD's acceptance criteria. See spec Section 7.

**4. Full verification** - Run the web test suite and TypeScript build check, plus the .NET suite to confirm nothing regressed.
