# P52-F05 — React — Portfolio Dashboard — Implementation Plan

## Prerequisites
- F01, F02, F03, F04 (backend) merged to `main` — confirmed: `GET /dashboard`,
  `/allocation-breakdown`, `/data-quality-report`, `/upcoming-income` all exist in
  `Financial.Web/src/api/generated/openapi.ts` today.
- On branch `feature/p52-f05-react-portfolio-dashboard`, branched from `main`.
- No backend change in this feature; no OpenAPI snapshot regeneration needed.

## Phase 1 — API client, types, nav/route wiring, and an empty page shell

1. **DTO type aliases.** Add the twelve new `Schema<'...DTO'>` aliases (§3/§5 of spec.md) to
   `Financial.Web/src/api/types.ts`, grouped near the existing Investment-context aliases.
2. **API client methods.** Add `getDashboard`, `getAllocationBreakdown`, `getDataQualityReport`,
   `getUpcomingIncome` to the `FinancialApiClient` interface and its `createFinancialApiClient`
   implementation in `financialApiClient.ts`, each a plain `request<T>('/path')` call with no query
   string, following every existing no-parameter method's exact shape.
3. **Nav entry and route.** Insert the "Dashboard" child first in `navTree.ts`'s `investments`
   category; add `DashboardPage` to `lazyPages.tsx`; add its route first among `investments/*` in
   `routes.tsx`.
4. **Empty page shell.** Create `Financial.Web/src/pages/DashboardPage.tsx` rendering a page title
   and four placeholder `<section>` containers (no data yet) plus `DashboardPage.css` with the
   responsive grid (KPI tiles full width; allocation + warnings side by side ≥1024px, stacked
   below; upcoming income full width) from §1 Decision 16/§6.1.
5. **Routing verification.** Confirm the existing `routes.test.ts` (NAV_TREE/PAGE_ROUTES agreement)
   passes unmodified with the new entries; add a `DashboardPage.test.tsx` smoke test asserting the
   route renders and the sidebar link exists.

## Phase 2 — F01 KPI tiles panel

6. **`useDashboardSummary` hook.** Add the `useAsyncResource`-based hook per §3, with its unit test
   suite (loading/success/error/retry).
7. **`KpiTileSkeleton` component.** Add the Fluent `Skeleton`/`SkeletonItem`-based placeholder plus
   its unit test (renders, no misleading accessible name).
8. **`DashboardKpiTiles` component.** Build the 8-tile grid (order, formatting, `signClass`
   colouring, null-XIRR "—"), the `IsPartial` inline notice with its "View affected holdings"
   callback prop, and the reporting-currency secondary block (enabled/partial/unavailable) per §6.2,
   reusing `formatN2`/`formatPercentFraction`/`signClass` verbatim. Add
   `DashboardKpiTiles.css` per §1 Decision 6.
9. **Wire into `DashboardPage`.** Replace the KPI placeholder section with the real hook + component
   pair, including its own loading/error rendering independent of the other three sections.
10. **Component tests.** `DashboardKpiTiles.test.tsx` covering every state in §6.2 and the testing
    strategy's own list (tile order, skeleton, colouring, partial notice, reporting-currency block,
    error+retry).

## Phase 3 — F02 allocation breakdown panel

11. **`useAllocationBreakdown` hook** + unit tests.
12. **`AllocationPieChart` component.** Dimension-agnostic pie chart (reusing
    `BrokerBreakdownCharts`' `CATEGORICAL_PALETTE`/tooltip pattern) + legend table (Label/Market
    Value/Percentage, right-aligned numerics, no re-sort) per §1 Decision 11, with its own unit
    tests.
13. **`AllocationBreakdownPanel` component.** `TabList`/`Tab` dimension switcher defaulting to
    Class, per-dimension entry-shape mapping to `AllocationPieChart`'s props, empty-dimension
    message, read-only (no click handlers) per §6.3, plus `AllocationBreakdownPanel.css`.
14. **Wire into `DashboardPage`** and add `AllocationBreakdownPanel.test.tsx` covering tab default,
    tab switching, empty state, and the read-only assertion.

## Phase 4 — F03 data-quality warnings panel and tree click-through integration

15. **Click-through utilities.** Add `utils/holdingNavigation.ts` (`resolveHoldingLocation`,
    active-then-historic tree search) and `hooks/useHoldingNavigation.ts`
    (`navigateToHolding` wrapping `useNavigate`), each with its own unit test suite per §7.
16. **`useDataQualityReport` hook** + unit tests.
17. **`DataQualityWarningsPanel` component.** Severity-ordered `Accordion` (`multiple`),
    hide-if-zero categories, all-zero success `MessageBar`, stale-valuation count-only row, per-row
    click-through calling `navigateToHolding`, the inline resolution-failure warning, and the
    `expandCategory` imperative handle, per §6.4 and §1 Decisions 8/12–15. Add
    `DataQualityWarningsPanel.css`.
18. **`InvestmentTree.tsx` change.** Add the `pendingSelection` consumption effect (match node,
    `setSelectedNode`, expand ancestors, best-effort scroll, clear router state) per §1 Decision 13
    step 3; extend `InvestmentTree.test.tsx` with the three new cases from §7.
19. **Wire into `DashboardPage`.** Real warnings panel + the KPI panel's
    `onViewMissingPriceHoldings` callback wired to the warnings panel's exposed `expandCategory`
    ref and a scroll-into-view on its container, per §1 Decision 8.
20. **Component + page tests.** `DataQualityWarningsPanel.test.tsx` per §7's full list; extend
    `DashboardPage.test.tsx` with the click-through-to-Active and click-through-to-Historic
    scenarios and the partial-notice cross-link scenario.

## Phase 5 — F04 upcoming income panel, page-level error state, and AC-tracing

21. **`utils/upcomingIncomeWindow.ts`.** Window option type, default, and
    `isWithinUpcomingIncomeWindow` predicate, with its own unit test suite (boundary, past-date,
    all three window values) per §1 Decision 9/§7.
22. **`useUpcomingIncome` hook** + unit tests.
23. **`UpcomingIncomePanel` component.** `FilterTabList` window selector defaulting to 90 days,
    sorted table, client-side re-filter with no re-fetch, empty-state message with the selected N,
    per §6.5, plus `UpcomingIncomePanel.css`. Add `UpcomingIncomePanel.test.tsx`.
24. **Page-level all-4-failed error state.** Implement the four-hooks-failed detection and the
    single `Button appearance="primary"` "Retry" in `DashboardPage.tsx` per §1 Decisions 4/17 and
    §6.1 rules 4–5; add the corresponding `DashboardPage.test.tsx` cases (page-level state only on
    all-4-failure, 1-of-4-failure leaves the other three panels intact, Retry re-issues all four
    requests).
25. **Full state-matrix pass and AC-tracing.** Run the complete `DashboardPage.test.tsx` suite
    against every scenario in spec.md §7; add AC ids `P52-F05-react-portfolio-dashboard-01..03` to
    PRD §9's F05 group (and tag the corresponding tests `[Trait]`-equivalent per this repo's
    frontend AC-tracing convention) in the order the three bullets already appear. Run `npm run
    lint`, `npm test`, `npm run build` (per CLAUDE.md's `Financial.Web` commands) and confirm
    green before finishing.
