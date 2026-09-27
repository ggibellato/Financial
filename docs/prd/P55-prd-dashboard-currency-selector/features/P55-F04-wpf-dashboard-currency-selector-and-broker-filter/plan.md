# Implementation Plan: F04. WPF Dashboard Currency Selector and Broker Filter

**Prerequisites:**
- F01 and F02 merged — this feature calls `IPortfolioDashboardService.GetDashboardAsync(Currency?,
  Currency?)` and `IAllocationBreakdownService.GetAllocationBreakdownAsync(Currency?, Currency?)`,
  neither of which exists in code until those two features ship.
- No new external services, packages, or configuration.

### Stage 1: Page-Local Filter State on the Dashboard

**1. Add the seeded filter properties to `DashboardViewModel`** - Introduce the display-currency and
broker-currency-filter bindable properties, seeded from the global Reporting Currency setting and
"All currencies" respectively before the dashboard's existing initial load runs, with the added
dependency this seeding requires.

**2. Re-trigger only the affected panels on change** - Wire both properties so that changing either
one reloads the KPI tiles and Allocation Breakdown sub-view models with the new values, leaving the
Data Quality Warnings and Upcoming Income panels untouched.

### Stage 2: Threading the Filters Through the KPI and Allocation Sub-View Models

**3. Extend the KPI tiles sub-view model's load method** - Accept the two filter values as
parameters and pass them to the Dashboard's Application-layer service call, replacing the current
no-argument call.

**4. Extend the Allocation Breakdown sub-view model's load method** - Accept the same two filter
values, switch its service call to the awaited, filter-aware overload, and map the response's
resolved display currency and partial/unavailable indicators onto new bindable properties.

### Stage 3: WPF Presentation

**5. Add the currency-controls header row** - Present the currency selector and broker-currency
filter together above the KPI tiles, using WPF-appropriate controls that mirror the web app's field
order and terminology.

**6. Hide the native KPI row and surface the Allocation Breakdown's provenance/partial/unavailable
states** - Stop showing the unconverted KPI tile row now that a display currency always applies, and
add the Allocation Breakdown panel's currency label, partial notice, unavailable state, and
empty-filter message, reusing the existing FX provenance converter pattern.
