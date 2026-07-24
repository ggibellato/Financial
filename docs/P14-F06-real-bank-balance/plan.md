# Implementation Plan: Real Bank Balance

**Prerequisites:**
- .NET 10 SDK and Node/npm toolchain already configured
- No new dependencies

### Stage 1: Backend Calculation

**1. Bank Balance DTO and Service** - Add the per-bank balance read model and the calculation itself: for each bank, sum income and expenses dated from its opening balance date through the end of the requested month, net of round-up amounts, added to the opening balance.

**2. Bank Balances API Endpoint** - Add the read-only HTTP endpoint that returns every bank's running balance for a given month, following the existing controllers' routing and response conventions.

### Stage 2: Frontend Integration

**3. Fetch and Wire Bank Balances** - Add the corresponding type and API client method, fetch bank balances alongside the Monthly page's other month-scoped data, and source the Banks grid's balance figures from this new data instead of the client-side month-only reduction. Keep the Round-Up column's existing computation unchanged.

**4. Relabel the Balance Column** - Update the Banks grid's column header and summary line to read "Bank Balance".
