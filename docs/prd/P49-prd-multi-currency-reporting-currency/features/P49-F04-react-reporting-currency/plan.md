# Implementation Plan: F04. React — Reporting Currency

**Prerequisites:**
- P49 F02 and F03 merged — this feature consumes F03's `GET`/`PUT /reporting-currency` endpoints and
  `AggregatedSummaryDTO`'s converted fields, and F02's captured `Currency`/`FxRateSnapshot` domain data.
- No new external services or configuration.

### Stage 1: Backend Provenance Exposure

**1. Add an FX snapshot DTO and extend Transaction/Credit DTOs** - Introduce a small nested DTO for
`ToCurrency`/`Rate`/`Source`/`RetrievedAt`, and add it plus the record's own `Currency` to
`TransactionDTO` and `CreditDTO`.

**2. Populate the new fields in the entity-to-DTO mapping** - Update the mapper that builds these
DTOs from the domain `Transaction`/`Credit` entities so the new fields carry through, with no
snapshot mapped to `null`.

**3. Regenerate the OpenAPI snapshot and frontend types** - Refresh the committed contract snapshot
and the generated TypeScript types so the frontend can consume the new fields.

### Stage 2: Reporting Currency Settings Page

**4. Add the reporting-currency API client methods and a settings hook** - Extend the typed API
client with get/set methods for the setting, and add a hook that loads the current value on mount
and persists a change immediately, tracking load vs. save errors separately.

**5. Build the Settings page and register it in navigation** - Add a new Settings page offering the
three supported currencies, wired to the hook, with loading and error states; register it under the
existing Settings nav section and route table.

### Stage 3: Converted Totals in the Summary Views

**6. Render converted figures in the aggregated summary view** - Extend the shared summary component
(already used by both the broker and portfolio views) to show every converted figure alongside the
existing native ones, each clearly labelled with the reporting currency.

**7. Handle the Partial and ReportingCurrencyUnavailable states** - Add the inline warning for a
partially-converted total and the retry-affordance treatment for an unavailable one, without
affecting the visibility or correctness of the native-currency figures.

### Stage 4: Per-Record FX Provenance Affordance

**8. Build a shared provenance affordance component** - A small reusable element that, given a
record's currency and its (possibly absent) FX snapshot, surfaces the rate, source and retrieved-at
date on demand.

**9. Wire the affordance into the Transactions and Credits tabs** - Add the affordance to each row of
both tabs, appearing only for records that actually captured a snapshot.
