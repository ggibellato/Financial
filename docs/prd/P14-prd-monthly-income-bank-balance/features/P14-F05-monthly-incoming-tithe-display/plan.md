# Implementation Plan: Monthly Incoming and Tithe Display

**Prerequisites:**
- Node/npm toolchain already configured for `Financial.Web`
- No backend changes and no new dependencies — F01 and F03's API contracts already exist

### Stage 1: Fetch Tithe Data and Derive Income Totals

**1. Tithe Summary Client Call and Income Grouping** - Add the tithe summary type and API client method for F03's existing endpoint, fetch it alongside the Monthly page's other month-scoped data, and derive a per-income-source summary (net total, and gross total where applicable) from the income entries already being fetched for the income form and list.

### Stage 2: Incoming Card

**2. Render the Incoming Card** - Add a new card to the Monthly page's grid row showing one row per income source present that month, plus a summary line with the total incoming amount, the calculated tithe, and the tithe balance.
