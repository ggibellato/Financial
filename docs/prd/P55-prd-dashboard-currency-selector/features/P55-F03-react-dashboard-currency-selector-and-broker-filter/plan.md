# Implementation Plan: React Dashboard Currency Selector and Broker Filter

**Prerequisites:**
- F01 and F02 implemented and merged (not just spec'd) — this feature's real API client and hook
  changes require `Financial.Web/src/api/generated/openapi.ts` regenerated
  (`npm run generate-api-types`) against their shipped `openapi-v1.snapshot.json`, since
  `displayCurrency`/`brokerCurrency`/`isPartial`/`isUnavailable` don't exist in the generated types
  until then.
- Node/npm toolchain already set up for `Financial.Web` (`npm install` already run).

### Stage 1: Types and API Client

**1. Frontend-only currency types** - Add `Currency` and `BrokerCurrencyFilter` type aliases to
`api/types.ts`, alongside the existing hand-written frontend-only types, so the rest of this
feature has a single shared vocabulary for the two controls' values.

**2. API client query params** - Extend `getDashboard` and `getAllocationBreakdown` in
`financialApiClient.ts` to accept optional display-currency and broker-currency-filter arguments
and append them to the request's query string only when supplied.

### Stage 2: Data Hooks

**3. Dashboard summary hook** - Update `useDashboardSummary` to accept the display currency and
broker-currency filter, pass them through to the API client, and skip fetching entirely while the
display currency has not yet been seeded.

**4. Allocation breakdown hook** - Apply the same signature and fetch-gating change to
`useAllocationBreakdown`.

### Stage 3: Header Controls and Page Wiring

**5. Currency controls component** - Build `DashboardCurrencyControls`, a controlled component
combining the always-one-selected currency selector and the broker-currency filter, using the
existing radio-group and filter-tab-list building blocks.

**6. Dashboard page state and seeding** - Give `DashboardPage` ownership of the display currency
and broker-currency filter state, seed the display currency once per visit from the global
Reporting Currency setting's stored value, default the filter to "All" on every visit, and render
the new controls in the page header above the KPI tiles.

**7. Wire hooks and panels to the new state** - Pass the two state values into the dashboard summary
and allocation breakdown hooks, and pass the broker-currency filter through to the Allocation
Breakdown panel so it can distinguish its own empty-dimension case from a zero-matching-brokers
case.

### Stage 4: KPI Tiles and Allocation Breakdown Presentation

**8. Remove the native KPI row** - Collapse `DashboardKpiTiles` down to a single tile grid driven
entirely by the converted figures, labelling each tile with the resolved display currency, and
retire the now-unused native-value code path.

**9. Currency label and provenance on Allocation Breakdown** - Add a "values shown in" currency
line to `AllocationBreakdownPanel`, and extend it with the same partial-conversion notice and
unavailable-conversion error/retry treatment already established on the KPI tiles.

**10. Broker-filter empty state on Allocation Breakdown** - Give the panel a distinct empty-state
message for the case where the active broker-currency filter matches no brokers at all, separate
from its existing "this dimension has nothing priced" message.

### Stage 5: Verification

**11. Type and build validation** - Run `tsc -b` (via `npm run build`) to confirm every touched
file compiles against the regenerated OpenAPI types with no leftover references to the removed
native-tile code path.

**12. Full frontend test suite** - Run `npm test` and `npm run test:coverage` to confirm every new
and updated test passes and the coverage gate is met before this feature is considered complete.
