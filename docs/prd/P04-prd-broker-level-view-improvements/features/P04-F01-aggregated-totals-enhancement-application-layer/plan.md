# Implementation Plan: F01 — Aggregated Totals Enhancement — Application Layer

**Prerequisites:**
- No new libraries or environment configuration required
- Existing `Financial.Application`, `Financial.Application.Tests`, and `Financial.Api.Tests` projects build and pass before starting

---

### Stage 1: Data Shape and Shared Helper

**1. AggregatedSummaryDTO** - Add the new `TotalInvested` property to the DTO, following the existing properties' style, so both existing endpoints can return it once the service populates it.

**2. NavigationMapper visibility** - Widen `IsEncerradas` from `private` to `internal` so it can be reused outside `NavigationMapper` without duplicating the comparison logic.

---

### Stage 2: Service Logic

**3. GetBrokerSummary rewrite** - Change the broker-level aggregation to source data from the broker/portfolio/asset tree instead of the flat asset list, excluding the Encerradas portfolio before applying the existing active-asset filter, per the spec's chosen data-access approach.

**4. GetPortfolioSummary and Aggregate update** - Leave the portfolio-level data source unchanged; update the shared aggregation helper so both methods populate `TotalInvested` on the returned DTO.

---

### Stage 3: Test Coverage

**5. Unit test fixture rewrite** - Update the `SummaryQueryService` test double to support building a broker/portfolio/asset graph, and adapt all existing broker-level tests to the new fixture shape without changing their original assertions.

**6. New unit tests** - Add coverage for Encerradas exclusion (including case-insensitivity), an unknown broker name, and `TotalInvested` computation (including the negative case) at both broker and portfolio scope.

**7. Integration test updates** - Extend the existing HTTP-level summary endpoint tests to assert the new `totalInvested` field is present and correctly computed in the live response.
