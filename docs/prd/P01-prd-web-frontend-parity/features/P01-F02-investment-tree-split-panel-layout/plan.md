# Implementation Plan: F02 — Portfolio Navigator - Investment Tree & Split Panel Layout

**Prerequisites:**
- F01 implemented (three-section shell and routes in place — confirmed via commit `a209a60`)
- `Financial.Web` dependencies installed (`npm install`)
- `Financial.Web/src/api/types.ts` available for extension
- Backend navigation tree endpoint confirmed to return all required metadata (BrokerName, Currency, PortfolioName, AssetCount, AssetName, Ticker, Exchange, IsActive, GlobalAssetClass) — no backend changes needed

---

### Phase 1: Type Definitions and Selected Node Context

**1. Extend types.ts with new interfaces** — Add `NodeType` union type (`'Asset' | 'Portfolio' | 'Broker'`), the `SelectedNode` interface (nodeType, brokerName, and optional portfolioName, assetName, ticker, exchange, currency, isActive), and `SelectedNodeContextValue` (selectedNode plus setSelectedNode) to `Financial.Web/src/api/types.ts`.

**2. Create SelectedNodeContext** — Create `src/context/SelectedNodeContext.tsx` exporting `SelectedNodeContext`, `SelectedNodeProvider` (holds `useState<SelectedNode | null>` and provides both value and setter to children), and `useSelectedNode()` hook (throws if called outside provider). Write `src/context/__tests__/SelectedNodeContext.test.tsx` covering the default null value, state update after calling setSelectedNode, and the out-of-provider guard.

---

### Phase 2: SplitPanel Component

**3. Create SplitPanel** — Create `src/components/SplitPanel.tsx` accepting `left` and `right` ReactNode props. Manage `leftWidth` state defaulting to 300px. Render a flex-row container with a fixed-width left div, a narrow drag handle div in the centre, and a right div that fills the remainder with `flex: 1`. Attach mousedown on the handle, then mousemove/mouseup on `document` while dragging to update leftWidth, clamping between 300px and 50% of `window.innerWidth`. Create `SplitPanel.css` with the flex layout, resize cursor on the handle, and `overflow: auto` on each panel for independent scrolling. Write `src/components/__tests__/SplitPanel.test.tsx` covering children rendering, handle presence, and default left width.

---

### Phase 3: InvestmentTree Component

**4. Delete NavigationTreePanel** — Remove `src/components/NavigationTreePanel.tsx` and its co-located test file. The routing-based tree is fully replaced by InvestmentTree.

**5. Create InvestmentTree** — Create `src/components/InvestmentTree.tsx`. On mount, fetch `GET /navigation/tree` via `createFinancialApiClient().getNavigationTree()`; show `<LoadingState>` while loading and `<ErrorState onRetry>` on failure. On success, render the "Investments" heading, the "Asset class" label with a filter dropdown (options: "All" + GlobalAssetClass labels for values 1–8, excluding 0=Unknown), and the node tree. Broker nodes render `node.displayName` (pre-formatted by backend as `"{name} ({currency})"`), expanded by default with a collapse/expand chevron. Portfolio nodes render `node.displayName` (pre-formatted as `"{name} ({N} assets)"`), collapsed by default. Asset nodes render `(isActive ? '●' : '○') + ' ' + node.displayName`, extracting `IsActive` from metadata. Clicking any non-root node calls `setSelectedNode` (from `useSelectedNode()`) with the correctly typed `SelectedNode` — metadata fields (BrokerName, Currency, PortfolioName, AssetName, Ticker, Exchange, IsActive) are propagated down through recursive rendering as a context object. Apply a `selected` CSS class to the node whose identity matches `selectedNode` from context, styled with `background: #007ACC; color: white`. The asset class filter is applied client-side after load: retain only Asset nodes whose `GlobalAssetClass` metadata matches the selected value, keeping their Broker and Portfolio ancestors; "All" restores the full unfiltered tree. Create `InvestmentTree.css`. Write comprehensive unit tests per the Testing Strategy section in the spec.

---

### Phase 4: DetailPanel Component

**6. Create DetailPanel** — Create `src/components/DetailPanel.tsx`. Read `selectedNode` via `useSelectedNode()`. When null, render a centred "Select an item to view details" message. When a node is selected, render a header containing: the node name bold at 20px; a clipboard copy icon button visible only for Asset nodes (calls `navigator.clipboard.writeText(assetName)` silently with no dialog); a breadcrumb line below the name (empty for Broker, broker name for Portfolio, `{ticker} · {exchange} · {brokerName} · {portfolioName}` for Asset); and a status indicator top-right visible only for Asset nodes (`● Active` in green or `○ Inactive` in grey at 11px). Below the header, render a tab bar with three buttons (Summary, Transactions, Credits). Manage `activeTab` state; use `useEffect` watching `selectedNode` to reset to `'summary'` on node change. Render a tab content area with placeholder text for each tab to be replaced by F03, F05, and F06. Create `DetailPanel.css`. Write unit tests per the Testing Strategy, mocking `navigator.clipboard.writeText`.

---

### Phase 5: Page Assembly and Integration Tests

**7. Rewrite PortfolioNavigatorPage** — Replace the placeholder in `src/pages/PortfolioNavigatorPage.tsx` with a component that wraps `SelectedNodeProvider` around a full-height container, rendering `SplitPanel` with `<InvestmentTree>` passed as the `left` prop and `<DetailPanel>` passed as the `right` prop. Create `src/pages/PortfolioNavigatorPage.css` applying a full-height flex-column layout for the page container.

**8. Write integration tests** — Update `src/pages/__tests__/PortfolioNavigatorPage.test.tsx` to mock `createFinancialApiClient` with a minimal test tree (one broker, one portfolio, one asset). Render the full page inside `MemoryRouter` and assert: the split panel renders with empty detail state on load; clicking an asset node causes the asset name to appear in the right panel header; clicking a broker node causes the broker name to appear in the right panel header.

**9. Full test suite validation** — Run `npm test` to confirm all new tests pass and no regressions exist in `App.test.tsx`, `DividendCheckPage.test.tsx`, or `CurrentValuesPage.test.tsx`. Verify the deleted `NavigationTreePanel` tests are removed and do not cause failures.
