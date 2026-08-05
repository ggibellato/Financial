# Implementation Plan: F08. WPF: Lock Payment Mode by Tab Context

**Prerequisites:**
- F06 (WPF Credit Card tab expense list) merged and available
- .NET solution builds on the current branch

### Stage 1: Parameterize the Create-Form Command by Mode

**1. Monthly View Model** - Make the command that opens the create form take an explicit payment mode, setting the form's card/bank state and resetting the mode-dependent fields to the right defaults at open time, and remove the now-unused mode-switching commands this replaces. Reference the spec's Component Overview and Technical Decisions for the exact command signature and reset logic.

### Stage 2: Remove the Toggle and Wire Each Tab's Trigger

**2. Expense Form and Tab Buttons** - Remove the payment-mode toggle from the shared expense form view so it always shows exactly one field group for whichever mode it was opened in, and set each tab's "New Expense" button to pass that tab's fixed mode to the command. Reference the spec's Testing Strategy for the manual verification checklist to run per tab.
