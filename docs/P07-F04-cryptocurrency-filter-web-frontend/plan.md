# Implementation Plan: Cryptocurrency Filter — Web Frontend

**Prerequisites:**
- Node/npm toolchain already configured for `Financial.Web` (Vite, Vitest, ESLint)
- No new dependencies

### Stage 1: Asset Class Filter

**1. Cryptocurrency Filter Option** - Add a new "Cryptocurrency" entry to the Investment Tree's asset-class filter options, appended after the existing 8 entries, using the value already assigned to it on the backend.

**2. Filter Test Coverage** - Extend the Investment Tree's existing test suite with coverage confirming the new option renders in the dropdown and correctly filters the tree to only cryptocurrency assets, following the same pattern as the suite's existing asset-class filter tests.
