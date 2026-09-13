# Implementation Plan: F05. WPF — Reporting Currency

**Prerequisites:**
- P49 F02, F03 and F04 merged — this feature consumes the in-process `IReportingCurrencyProvider`
  and `ISummaryService` (F03), and the `Currency`/`FxRateSnapshot` fields F04 added to
  `TransactionDTO`/`CreditDTO` (shared in-process with WPF, no backend work needed here).
- No new external services or configuration.

### Stage 1: Reporting Currency Settings Page

**1. Add the Reporting Currency ViewModel** - A new ViewModel offering the three supported
currencies, seeded synchronously from the current setting and persisting a change immediately,
tracking a save-specific error.

**2. Build the Settings view and register it** - Add the corresponding view, wire it into DI and
the view-key routing, and register it under the existing Settings navigation category.

### Stage 2: Converted Totals in the Summary Views

**3. Extend the summary ViewModel with the converted fields** - Add every converted figure, the
reporting currency label, and the Partial/Unavailable flags to the ViewModel class that already
backs every broker/portfolio summary binding, populated alongside the existing native fields.

**4. Render the converted figures on both summary surfaces** - Add the converted-figures block,
labelled with the reporting currency, to the broker summary template and the portfolio totals bar,
including the Partial inline warning and the Unavailable state that hides the converted block while
leaving native figures untouched.

### Stage 3: Per-Record FX Provenance Affordance

**5. Build the provenance converters** - Small converters that turn a record's currency and
(possibly absent) FX snapshot into an icon's visibility and its tooltip text.

**6. Wire the provenance column into the Transactions and Credits grids** - Add the affordance to
each row of both grids, appearing only for records that captured a snapshot.
