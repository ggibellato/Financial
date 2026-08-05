# Implementation Plan: F06. WPF: Show Unpaid Card Expenses in Credit Card Tab

**Prerequisites:**
- F04 (backend `GetUnpaidCardChargesByMonth`) merged and available
- .NET solution builds on the current branch

### Stage 1: Fetch and Expose the Unpaid Card Charges Collection

**1. Monthly View Model** - Add a collection for the month's unpaid card charges to the shared Monthly view model, populated alongside its existing expense fetch during refresh, so both the new list and the existing edit/delete commands can operate on it. Reference the spec's Component Overview for the exact member name and refresh integration.

### Stage 2: Render the List in the WPF Credit Card Tab

**2. Credit Card Tab Expense List View** - Add a new view showing the unpaid card charges as a list below the existing per-card totals grid, reusing the existing New Expense button, edit/delete commands, and shared expense form unchanged. Wire it into the Credit Card tab alongside the existing totals grid. Reference the spec's Technical Decisions for why this is a new dedicated view rather than a parameterized reuse of the existing Expense tab's view.
