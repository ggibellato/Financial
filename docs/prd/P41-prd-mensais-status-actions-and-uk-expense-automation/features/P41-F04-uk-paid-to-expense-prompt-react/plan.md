# Implementation Plan: F04. UK Paid-to-Expense Prompt (React)

**Prerequisites:**
- F02 merged to `main` (status control and `updateBillStatus` available to extend)
- No new packages required (`Dialog`, `Select`, `MessageBar` already used elsewhere in `Financial.Web`)

### Stage 1: Data Prerequisites

**1. Bank and Category Data** - Extend the Mensais data hook's initial fetch to also load banks and categories alongside bills, following the same combined-fetch pattern already used elsewhere in this app, so the prompt has what it needs without a separate loading state.

### Stage 2: Prompt State Machine

**2. UK Transition Interception** - Extend the existing status-change function so that a UK bill transitioning into Paid opens a prompt instead of immediately calling the status endpoint, while every other case (Brasil bills, non-Paid targets, already-Paid bills) keeps updating exactly as before. Cover it with hook tests for each branch.

**3. Confirm, Skip, and Retry Actions** - Add the three outcomes the prompt can produce: creating the Expense and then committing the status, committing the status alone, and — specifically when the Expense was already created but the status commit failed — retrying only the status commit without creating a second Expense. Cover each path and its failure modes with hook tests.

### Stage 3: Dialog and Page Integration

**4. Expense Prompt Dialog** - Build the confirmation dialog: pre-filled description/value/date, required bank and category selection, and the retry-only reduced view for the partial-failure case. Cover it with component tests for rendering, validation-gated confirmation, and each action's callback.

**5. Mensais Page Wiring** - Render the new dialog from the Mensais page when a prompt is open, passing through the bank/category lists and the new hook actions, with no change to how the existing status control or edit form trigger status changes. Cover it with page-level integration tests confirming the dialog opens only for the intended transition.
