# Implementation Plan: F02 — Broker Portfolio & Asset Breakdown Service — Application Layer

**Prerequisites:**
- No new libraries or environment configuration required
- F01 (Aggregated Totals Enhancement) merged — this feature reuses `NavigationMapper.IsEncerradas` (widened to `internal` by F01)

---

### Stage 1: DTOs and Service Contract

**1. Breakdown DTOs** - Add the two new DTOs representing a portfolio-level breakdown entry and its nested asset-level entries, following the existing DTO style in the Application layer.

**2. Service interface** - Define the new query service contract with a single method returning the broker's eligible portfolio/asset breakdown.

---

### Stage 2: Service Implementation and Wiring

**3. BrokerBreakdownQueryService** - Implement the breakdown computation per the spec's exclusion and slice rules, reusing the broker/portfolio/asset data source and Encerradas check already established by F01.

**4. Controller endpoint and DI registration** - Add the new GET endpoint to the existing summary controller and register the new service in the Application layer's dependency injection setup.

---

### Stage 3: Test Coverage

**5. Unit tests** - Cover the exclusion rules (Encerradas, inactive assets, non-positive slices), portfolio-level omission, and alphabetical sorting for both portfolios and assets.

**6. Integration tests** - Extend the existing summary endpoint test file to verify the new endpoint's success and validation-error responses over live HTTP.
