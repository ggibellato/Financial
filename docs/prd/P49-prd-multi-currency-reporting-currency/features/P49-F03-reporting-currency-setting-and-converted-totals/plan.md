# Implementation Plan: F03. Reporting Currency Setting and Converted Totals

**Prerequisites:**
- P49 F01 and F02 merged — this feature consumes the shared `IExchangeRateProvider`,
  `Financial.Shared.Abstractions.Currencies.Currency`, and `Transaction`/`Credit`'s `Currency` field.
- No new external services or configuration.

### Stage 1: Persisted Setting

**1. Add the reporting-currency setting to the aggregate root** - Give `Investments` a
`ReportingCurrency` property (defaulting to GBP) and a way to change it.

**2. Replace F02's interim provider with the persisted one** - Add the setter to
`IReportingCurrencyProvider`, implement it against the repository, and swap it into DI in place of
the fixed-GBP placeholder.

**3. Expose the setting over the API** - Add a small controller with GET/PUT endpoints and the DTO
they share.

### Stage 2: Conversion Engine

**4. Build the per-record conversion engine** - A new service that, given a broker/portfolio's
assets and the current reporting currency, converts every contributing transaction/credit
individually at its own date, sums the flow-based figures, converts market value and unrealised
gain/loss at today's rate, and re-runs the existing XIRR calculation against the converted
cash-flow series - tracking which conversions succeeded so partial/unavailable states can be
reported.

**5. Extend the aggregated summary DTO** - Add the reporting currency and every converted figure,
alongside the existing native ones, unchanged.

**6. Wire the conversion engine into the summary service** - Update `SummaryService` to call it and
attach the results; this requires making both existing summary methods asynchronous.

### Stage 3: Presentation Ripple

**7. Update the summary API endpoints** - Make the two existing controller actions asynchronous to
match the updated service contract; no request/response shape change beyond the new fields already
covered in Stage 2.

**8. Update the WPF composition** - Adjust `Financial.App`'s two call sites of the now-async
summary methods to the codebase's existing fire-and-forget async-loading convention, with no
visible behavior change.

### Stage 4: Tests and Verification

**9. Unit-test the persisted setting** - Cover the aggregate's default and mutation, and the
service that reads/writes it through the repository.

**10. Unit-test the conversion engine** - Cover the same-currency (no-op), differing-currency,
partial-failure and total-failure cases for every converted figure.

**11. Update existing summary-service and WPF ViewModel tests** - Adjust for the async signature
change; add coverage for the new converted fields on the native-figure test suite.

**12. Add the AC-tracing Integration suite** - One tagged test per provable §9 criterion, through
the real API host with the exchange-rate provider stubbed.

**13. Full-suite verification** - Run the complete test suite and a full solution build to confirm
no regressions.
