# Implementation Plan: Web Income Form Dynamic Source Picklist

**Prerequisites:**
- F04 merged (`GET /income-sources` endpoint)
- No new npm packages required

### Stage 1: API Client

**1. IncomeSourceDto and API Client Method** - Add the DTO type and a `getIncomeSources()` method to the web API client, mirroring the existing `getBanks()` pattern exactly.

### Stage 2: Data Hook and Form

**2. Fetch and Default Selection** - Fetch the income source list in `useMonthly`'s existing data load, alongside banks, and default the "new income" form's selected source to the first active source once the list arrives, replacing the hardcoded default.

**3. Dynamic Picklist Rendering** - Replace `IncomeForm`'s hardcoded source array with the fetched list, filtered to active sources and ordered to match the current display order, threaded in as a prop from the Monthly page the same way banks already are.
