# Implementation Plan: F04 — Chart Period Filter — YTD Extension

**Prerequisites:**
- No new libraries or environment configuration required
- No dependency on F01/F02/F03 — this feature is entirely Presentation-layer (Web + WPF) and does not touch the backend

---

### Stage 1: Web — Shared Period Filter Module and Wiring

**1. periodFilter.ts utility** - Add the new shared module in `Financial.Web/src/utils/` exporting the `PeriodFilterOption` type, the `PERIOD_FILTER_OPTIONS` label list (6 entries), and the `getPeriodFilterStartDate` date-range function, per the spec's Technical Decisions.

**2. Rewire useCredits.ts** - Remove the local `FilterOption` type and `getFilterStartDate` function; import the shared equivalents; update the default filter value to the renamed `'last-12-months'`.

**3. Rewire CreditsTab.tsx** - Remove the local `FILTER_OPTIONS` constant; import and render `PERIOD_FILTER_OPTIONS` from the shared module.

---

### Stage 2: Web — Test Coverage

**4. periodFilter.test.ts** - Add unit tests covering all 6 options' date-range computation against fixed reference dates, including the new YTD option and its January edge case.

**5. Update useCredits.test.ts** - Update the existing assertions that reference the old `'last-year'` value to the renamed `'last-12-months'`.

**6. Update CreditsTab.test.tsx** - Update button-label assertions for the two relabelled options and add a new assertion for the YTD button.

---

### Stage 3: WPF — Shared Period Filter Helper and Wiring

**7. PeriodFilterHelper.cs** - Add the new shared helper in `Financial.App/Helpers/` with the `PeriodFilter` enum, the `Options` label list, and the `GetDateRange` date-range function, per the spec's Technical Decisions.

**8. Remove CreditsFilter.cs** - Delete the superseded `Financial.App/ViewModels/CreditsFilter.cs`, now replaced by `PeriodFilter`.

**9. Rewire CreditsFilterOptionViewModel.cs and CreditsViewState.cs** - Update the `Filter` property/field type on both from `CreditsFilter` to `PeriodFilter`.

**10. Rewire AssetDetailsViewModel.cs** - Retype `_selectedCreditsFilter` and related method signatures to `PeriodFilter`; rebuild `InitializeCreditsFilters()` to iterate `PeriodFilterHelper.Options`; delegate `FilterCredits`'s date-range computation to `PeriodFilterHelper.GetDateRange`.

---

### Stage 4: WPF — Test Coverage

**11. PeriodFilterHelperTests.cs** - Add unit tests covering all 6 filters' `GetDateRange` output against fixed reference dates, including the new YTD option and its January edge case, plus the option-list ordering.
