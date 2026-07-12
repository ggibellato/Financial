# Implementation Plan: Status Invest Finance Service

**Prerequisites:**
- No new tools or libraries — uses the existing `HtmlAgilityPack` + xUnit + FluentAssertions stack
- The slug-derivation rule and the page's sell/buy price ordering were already confirmed against three real bond titles during spec research; no further live-site inspection is required before implementing, though the manual verification test should still be run once after implementation as a final check

### Stage 1: Status Invest Scraper

**1. StatusInvest Scraper** - Add the static scraper that derives a URL slug from a bond title, fetches the corresponding Status Invest page, and extracts the sell-price value from the confirmed VALOR DE VENDA section ahead of the VALOR DE COMPRA section. Reference the spec for the exact slug rule and extraction approach.

**2. Manual Verification Test Class** - Add a skipped-by-default test class that exercises the scraper against the real site with the bond titles already confirmed during research, mirroring the existing Google Finance verification tests, so the live structure can be reconfirmed by running it manually if needed later.

### Stage 2: Finance Service

**3. StatusInvestFinanceService Implementation** - Add the finance-service implementation that validates its request and delegates to the scraper, returning its result directly. Reference the spec for the exact validation behavior.

### Stage 3: Wiring and Test Coverage

**4. Dependency Injection Registration** - Register the new finance service in the Infrastructure composition root by its own concrete type, not as the shared finance-service interface, to avoid creating an ambiguous registration for the existing Google Finance consumers. Reference the spec's Technical Decisions for why.

**5. Unit Test Coverage** - Add test coverage for the scraper's slug-derivation and price-extraction logic against in-memory text, and for the finance service's validation, following the existing sibling test files' structure. Reference the spec's Testing Strategy for the full list of test functions.

**6. Regression Check** - Confirm every existing test in the solution still passes unchanged, since this feature only adds new, not-yet-wired types. Run the full test suite to verify no existing behavior changed.
