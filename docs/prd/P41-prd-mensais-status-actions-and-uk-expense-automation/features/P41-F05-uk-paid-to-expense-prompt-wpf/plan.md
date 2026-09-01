# Implementation Plan: F05. UK Paid-to-Expense Prompt (WPF)

**Prerequisites:**
- F03 merged to `main` (`ChangeStatusCommand`/`ChangeStatusAsync` available to extend)
- F04 merged to `main` (React reference behavior to mirror)
- No new packages required (`IExpenseService`/`IBankService`/`ICategoryService` already registered via `AddFinancialCashFlowApplication()`)

### Stage 1: Expense Prompt Dialog

**1. Dialog ViewModel and Window** - Build the new confirmation dialog following the project's existing form-dialog pattern (a `*DialogViewModel` with Confirm/Cancel-style `CloseRequested`, hosted by a `Window` via the shared dialog-closing helper), extended with a third action (Skip) and a way for the caller to tell which of the three was chosen. Pre-fill it from the bill's description, value, and today's date, with required bank and category selection gating confirmation. Cover it with view model tests for pre-fill, validation gating, and each action's outcome.

**2. Dialog Service Registration** - Add a method for showing the new dialog to the shared dialog-hosting service and its test double, matching the shape of every existing form-dialog method there.

### Stage 2: Mensais View Model Orchestration

**3. UK Transition Interception and Orchestration** - Extend the Mensais view model's status-change flow so a UK bill transitioning into Paid shows the new dialog before committing anything, then drives the three outcomes: creating the expense and committing the status, committing the status alone, or leaving everything unchanged. Handle the case where the expense is created but the status commit fails by offering a retry through the view model's existing confirmation mechanism, without ever creating a second expense. Cover every branch and failure mode with view model tests using hand-written stub services.

**4. Composition Wiring** - Update the application's dependency registration for the Mensais view model to supply its new dependencies, following the same pattern already used for the Monthly view model's registration.

### Stage 3: Documentation

**5. WPF Dialog Pattern Documentation** - Note in the WPF UI rules that this is the first three-action modal dialog in the app and how its result is represented, so the next one follows the same approach instead of reinventing it.
