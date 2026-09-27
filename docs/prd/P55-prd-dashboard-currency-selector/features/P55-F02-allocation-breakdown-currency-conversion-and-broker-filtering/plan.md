# Implementation Plan: Allocation Breakdown Currency Conversion and Broker Filtering

**Prerequisites:**
- Existing multi-currency infrastructure from P49: `Currency` enum, `IExchangeRateProvider`,
  `CurrencyConversionContext`, already registered in DI.
- `PortfolioDashboardConvertedBuilder` as the reference conversion pattern (read, do not modify).
- `UPDATE_OPENAPI_SNAPSHOT=1` environment variable support for the snapshot regeneration step.
- `Financial.Web` toolchain (`npm run generate-api-types`) for the generated-types regeneration
  step, run after the backend snapshot changes even though no other Web file changes in this
  feature.

### Stage 1: Contract and DTO

**1. Service contract** - Change `IAllocationBreakdownService.GetAllocationBreakdown` to an
async method accepting the two optional display-currency and broker-currency-filter parameters,
matching the shape already agreed for the Dashboard KPI equivalent.

**2. Allocation Breakdown DTO** - Add the three new top-level fields carrying the resolved
display currency and the panel-level partial/unavailable indicators to the response contract,
alongside the four existing dimension lists.

### Stage 2: Broker Filtering and Conversion

**3. Broker-currency filtering** - Apply the broker-currency filter to the active brokers this
service already reads, before any holding is valued, so a non-matching broker never contributes
to any dimension.

**4. Conversion pre-pass** - Extend the allocation builder with a conversion step that runs before
today's existing grouping/percentage logic, converting each priced holding's market value into
the requested display currency by its native currency, using the same conversion mechanism and
partial/unavailable rules already established for the Dashboard's converted KPI totals.

**5. Backward-compatible native path** - Preserve today's exact unconverted behavior when no
display currency is requested, including the "by currency" dimension's existing label/value
shape now made explicit as native label plus converted value.

### Stage 3: API Surface

**6. Controller query parameters** - Accept the two optional currency parameters as query strings,
validating each before invoking the service and rejecting an unparseable value up front.

**7. OpenAPI and generated types** - Regenerate the OpenAPI snapshot to capture the new query
parameters and response fields, then regenerate and commit the frontend's generated API types
from that snapshot.

### Stage 4: Verification

**8. Application-layer tests** - Cover broker filtering, conversion correctness across all four
dimensions, the native-currency label/converted-value split, the no-parameter compatibility path,
and the partial/unavailable determination including a fully-failed conversion.

**9. Acceptance and contract tests** - Add one acceptance test per §9 F02 criterion tracing to the
endpoint's observable behavior, and confirm the regenerated OpenAPI snapshot and generated
TypeScript types stay in sync with the implementation.
