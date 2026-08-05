# Implementation Plan: F05. Web: Show Unpaid Card Expenses in Credit Card Tab

**Prerequisites:**
- F04 (backend `unpaid-card-charges` endpoint) merged and available
- Node dependencies installed in `Financial.Web` (`npm install`)

### Stage 1: Fetch and Expose the Unpaid Card Charges List

**1. API Client and Monthly Data Hook** - Add a client method for the new unpaid-card-charges endpoint, fetch it alongside the Monthly page's other month-scoped data, and expose the result from the shared data hook so both existing edit/delete handlers can operate on it. Reference the spec's Component Overview for the exact fields and fetch integration.

### Stage 2: Render the List in the Credit Card Tab

**2. Credit Card Tab List and Form Wiring** - Render the unpaid card charges as a reused expense list below the existing per-card totals grid, wire its edit/delete actions to the already-existing handlers, and make the shared create/edit form available from this tab too, including canceling it on tab switch. Reference the spec's Technical Decisions for how the form JSX is shared between tabs.
