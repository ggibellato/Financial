# Implementation Plan: Web — Historic Investments Tab

**Prerequisites:**
- F05 (Scoped Navigation & Summary API), F06 (Historic Realized Totals Service), F07 (Historic Broker Breakdown Charts Service), and F08 (Web Active Investments Tab Update) merged — provide the scope-aware backend endpoints and the current-generation `InvestmentTree`/`DetailPanel`/`SelectedNodeContext` this feature threads scope through
- No new tools, libraries, or environment variables required

### Stage 1: Scope Foundation

**1. Scope Type and Context** - Introduce the shared scope type and thread it through the selected-node context so every component and hook downstream of the provider can read which scope the current page represents.

**2. API Client Scoping** - Replace the hardcoded Active-only query constant in the Web API client with an explicit scope parameter on every scope-capable method, per the spec's Component Overview.

### Stage 2: Historic Page, Navigation, and Tree

**3. Historic Investments Page** - Add a new page mirroring the Active Investments composition, wrapping the tree and detail panel in a provider scoped to Historic.

**4. Navigation and Routing** - Add the nav entry and route for the new page.

**5. Tree Scope Wiring** - Update the investment tree component to read scope from context and request the correctly scoped navigation data.

### Stage 3: Summary Data and Rendering

**6. Aggregated and Breakdown Summary Scoping** - Update the broker/portfolio aggregated-summary hook and the broker-breakdown hook to forward scope, so broker- and portfolio-level totals and the nested breakdown charts reflect the selected scope.

**7. Portfolio Asset Summary Scoping** - Update the portfolio-level asset summary hook to forward scope and skip the current-price fetch for Historic selections.

**8. Asset Summary Scoping and Realized Totals** - Update the single-asset summary hook to forward scope, skip current-price/XIRR computation for Historic, and source realized totals for the selected asset from the portfolio-level summary per the spec's chosen approach.

**9. Portfolio Summary Tab Rendering** - Update the portfolio-level summary tab to suppress current-value-derived columns and footer figures for Historic, per the spec's Technical Decisions.

**10. Asset Summary Tab Rendering** - Update the single-asset summary tab to render a realized-totals section in place of the current-value/XIRR section for Historic selections.

### Stage 4: Transactions and Credits Scope Wiring

**11. Asset-Level Lookup Scoping** - Update the credits and transactions hooks' asset-node lookup to forward scope, so selecting a historic asset resolves against the correct collection for editing.

### Stage 5: Test Coverage

**12. Foundation Tests** - Cover the scope-aware context and the API client's scope-parameterized request building.

**13. Tree and Navigation Tests** - Cover the tree's scope-aware fetch and the new nav item/route.

**14. Summary Hook Tests** - Cover scope forwarding across the aggregated-summary, breakdown, portfolio-asset-summary, and asset-summary hooks, including the asset-summary hook's realized-totals lookup and the current-price/XIRR skip behavior for Historic.

**15. Summary Tab Rendering Tests** - Cover the scope-conditional rendering in both summary tab components.

**16. Transactions/Credits Scope Tests** - Cover the scope-correct asset resolution in both hooks.

**17. Historic Page Test** - Cover the new page's tree/detail-panel composition, mirroring the existing Active Investments page test.
