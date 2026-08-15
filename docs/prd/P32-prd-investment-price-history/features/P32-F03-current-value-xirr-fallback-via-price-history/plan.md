# Implementation Plan: Current-Value/XIRR Fallback via Price History

**Prerequisites:**
- F01 (Price History Recording) and F02 (Price History Tab & Chart) merged to `main`.
- No new tools, libraries, or environment variables.

---

### Stage 1: Application-layer fallback orchestration

**1. Price DTOs** - Extend the current-price DTOs to carry the identity needed to resolve an asset's Price History and the flag needed to badge a manually-sourced value.

**2. Current-price orchestration service** - Extend the price service so that requesting a current price first attempts the existing live fetch, records a successful result into Price History (skipping the write when today's entry is already an unchanged automatic one), and falls back to today's Price History entry when the live fetch fails or no identity is supplied to look one up. A live fetch success always takes precedence over a stale manual entry for the same day.

### Stage 2: API endpoint

**3. Current-price endpoint** - Route the existing "get current price" endpoint through the new orchestration instead of the raw live-fetch service, accepting the asset's portfolio/asset identity as optional query parameters so existing callers that don't supply it keep working unchanged.

### Stage 3: WPF single-asset current value

**4. Single-asset current-value tracking** - Update the single-asset "Current Value" panel's refresh flow to go through the new orchestration and carry forward whether the returned price was manually sourced.

**5. Single-asset badge** - Show a "Manual" indicator next to the single-asset Current Value display when the value came from a manual Price History entry, reusing the existing manual/automatic visual convention from the Price History tab.

### Stage 4: WPF portfolio-row current value

**6. Portfolio-row current-value tracking** - Update the portfolio summary grid's per-row price fetch to go through the new orchestration, passing the asset's portfolio identity, and carry the manual flag onto each row.

**7. Portfolio-row badge** - Show the same "Manual" indicator next to a portfolio row's current value/price cell when that row's price came from a manual entry.

### Stage 5: Web single-asset current value

**8. Web API client and types** - Extend the current-price API client call and its response type to carry the asset's portfolio/asset identity on the request and the manual flag on the response.

**9. Single-asset hook and badge** - Update the single-asset summary hook to pass the asset's identity and surface the manual flag, and show a "Manual" badge next to the current-value section, reusing the same visual convention as the Price History tab.

### Stage 6: Web portfolio-row current value

**10. Portfolio-row hook and badge** - Update the portfolio summary hook's per-row price fetch to pass each item's identity and track the manual flag per row, and show the same "Manual" badge next to a row's current value/price cell in the portfolio summary table.
