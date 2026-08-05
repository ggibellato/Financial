# Implementation Plan: F01. Exclude Unpaid Card Charges from Expense List

**Prerequisites:**
- .NET solution builds and existing test suite passes on the current branch
- No new tools, libraries, or environment variables required

### Stage 1: Filter Unsettled Card Charges Out of the Monthly Expense List

**1. Expense List Query** - Update `ExpenseService.GetExpensesByMonth` so it excludes expenses currently charged to a credit card and not yet settled, while leaving bank-paid and already-settled expenses in the result exactly as returned today. Reference the spec's Component Overview and Technical Decisions for the exact predicate and its placement.
