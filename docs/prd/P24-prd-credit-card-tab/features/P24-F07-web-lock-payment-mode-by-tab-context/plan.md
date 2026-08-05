# Implementation Plan: F07. Web: Lock Payment Mode by Tab Context

**Prerequisites:**
- F05 (Web Credit Card tab expense list) merged and available
- Node dependencies installed in `Financial.Web` (`npm install`)

### Stage 1: Lock the Create Form's Mode by Tab and Remove the Toggle

**1. Monthly Data Hook** - Make the create form's opening action take an explicit payment mode, resetting the mode-dependent fields to the right defaults for that mode at open time, and remove the now-unused mode-switching actions this replaces. Reference the spec's Component Overview and Technical Decisions for the exact reducer changes.

### Stage 2: Remove the Toggle and Wire Each Tab's Trigger

**2. Expense Form and Page Wiring** - Remove the payment-mode toggle from the shared expense form so it always shows exactly one field group for whichever mode it was opened in, and update each tab's "New Expense" trigger to open the form with that tab's fixed mode. Reference the spec's Testing Strategy for the acceptance-mapped scenarios to verify per tab.
