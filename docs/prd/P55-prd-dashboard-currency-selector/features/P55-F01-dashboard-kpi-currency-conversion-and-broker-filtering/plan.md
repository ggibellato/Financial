# Implementation Plan: Dashboard KPI Currency Conversion and Broker Filtering

**Prerequisites:**
- Existing multi-currency infrastructure from P49 (`Currency` enum, `IExchangeRateProvider`,
  `CurrencyConversionContext`, `PortfolioDashboardConvertedBuilder`, `IReportingCurrencyProvider`)
  already in place — no new packages or configuration.
- `UPDATE_OPENAPI_SNAPSHOT` environment variable for the snapshot regeneration step.

### Stage 1: Service Contract and Broker Filtering

**1. Service interface** - Extend `IPortfolioDashboardService.GetDashboardAsync` with two optional
`Currency?` parameters (display currency and broker-currency filter), both defaulting to `null` so
every existing caller keeps compiling and behaving unchanged.

**2. Broker-currency filtering** - Update `PortfolioDashboardService`'s holding-collection step so
a supplied broker-currency filter excludes non-matching brokers, active and historic alike, before
any valuation or conversion work runs. Confirm a filter matching zero brokers flows through to a
valid, empty-totals result rather than an error.

### Stage 2: Always-On Conversion Override

**3. Resolved currency and override logic** - Update `PortfolioDashboardService`'s dashboard-build
step so a supplied display currency is always converted into, regardless of the global Reporting
Currency setting's enabled/disabled state, while an omitted display currency preserves today's
exact gated behavior. Ensure the response's resolved-currency and conversion-enabled indicators
reflect the requested-or-global currency and whether conversion actually ran.

**4. Partial/unavailable behavior verification** - Confirm the existing conversion engine's
partial/unavailable semantics apply unchanged under the new override and filtering paths, with no
new failure-handling logic needed.

### Stage 3: API Surface and Contract Sync

**5. Controller query parameters** - Add optional `displayCurrency`/`brokerCurrency` query
parameters to the dashboard endpoint, validating each against the supported currency values before
any calculation begins and rejecting an invalid value up front.

**6. OpenAPI and generated-types regeneration** - Regenerate the OpenAPI snapshot to reflect the
new query parameters, review the diff, then regenerate and commit the Financial.Web generated API
types from that snapshot.
