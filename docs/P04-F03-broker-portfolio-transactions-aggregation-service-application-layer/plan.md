# Implementation Plan: F03 — Broker & Portfolio Transactions Aggregation Service — Application Layer

**Prerequisites:**
- No new libraries or environment configuration required
- No dependency on F01 or F02 — this feature reuses only the pre-existing `IRepository.GetAssetsByBroker`/`GetAssetsByBrokerPortfolio` and applies no exclusion rules

---

### Stage 1: DTO and Service Contract

**1. TransactionSummaryItemDTO** - Add the new cross-asset transaction DTO in the Application layer's DTOs folder, following the existing DTO style (`CreditDTO`, `TransactionDTO`).

**2. Service interface** - Define the new query service contract with two methods: one returning a broker's combined transaction list, one returning a single portfolio's.

---

### Stage 2: Service Implementation and Wiring

**3. TransactionQueryService** - Implement combined transaction retrieval per the spec's data source and sort rules, with no Encerradas or active-asset filtering.

**4. NavigationMapper mapping helper** - Add the internal mapping method from an `(Asset, Transaction)` pair to `TransactionSummaryItemDTO`.

**5. Controller endpoints and DI registration** - Add the two new GET endpoints to the existing `TransactionsController`, with route-parameter validation returning 400, and register the new service in the Application layer's dependency injection setup.

---

### Stage 3: Test Coverage

**6. Unit tests** - Cover combined retrieval across assets and portfolios, the no-exclusion behaviour (Encerradas and inactive assets included), the date-ascending/asset-name-tiebreak sort order, and empty/unknown-scope edge cases.

**7. Integration tests** - Extend the existing transactions endpoint test file to verify both new endpoints' success and validation-error responses over live HTTP.
