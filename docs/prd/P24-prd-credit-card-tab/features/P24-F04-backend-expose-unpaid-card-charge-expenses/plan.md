# Implementation Plan: F04. Backend: Expose Unpaid Card Charge Expenses

**Prerequisites:**
- .NET solution builds and existing test suite passes on the current branch
- No new tools, libraries, or environment variables required

### Stage 1: Add the Unpaid Card Charges Query and Endpoint

**1. Unpaid Card Charges Query and Endpoint** - Add a new read-only query returning the month's unsettled credit card charge expenses, mirroring the existing monthly expense query but filtered to the opposite payment status, and expose it through a new endpoint on the existing expenses resource. Reference the spec's Component Overview and API Contracts for the exact method signature, route, and response shape.
