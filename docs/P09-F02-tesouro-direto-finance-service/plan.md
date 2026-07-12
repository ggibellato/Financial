# Implementation Plan: Tesouro Direto Finance Service

**Prerequisites:**
- No new tools or libraries — uses the existing HtmlAgilityPack + xUnit + FluentAssertions stack
- Before finalizing the scraper's parsing logic, manually inspect the live tesourodireto.com.br redemption page from a machine with normal internet access (the sandboxed environment used to write this spec could not reach the site — see spec's Technical Decisions), and confirm or correct the assumed table/column structure

### Stage 1: Tesouro Direto Scraper

**1. TesouroDireto Scraper** - Add the static scraper that fetches the Tesouro Direto redemption-price table with a browser-like request identity, locates the relevant table by its column headers rather than a hardcoded position, and matches a bond by title. Reference the spec for the exact fetch, column-resolution, and matching approach.

**2. Manual Verification Test Class** - Add a skipped-by-default test class that exercises the scraper against the real site with known bond titles, mirroring the existing Google Finance verification tests, so the live structure can be confirmed by running it manually before this feature is relied on in production.

### Stage 2: Finance Service and Not-Found Signal

**3. Shared Not-Found Exception** - Add the shared exception type that signals "this source doesn't have the requested value" in a way distinguishable from any other failure, since the existing finance-service contract only supports throw-on-failure. Reference the spec for its placement and intended catch-site.

**4. TesouroDiretoFinanceService Implementation** - Add the finance-service implementation that validates its request, delegates to the scraper, and translates a no-match result into the shared not-found exception. Reference the spec for the exact validation and translation behavior.

### Stage 3: Wiring and Test Coverage

**5. Dependency Injection Registration** - Register the new finance service in the Infrastructure composition root by its own concrete type, not as the shared finance-service interface, to avoid creating an ambiguous registration for the existing Google Finance consumers. Reference the spec's Technical Decisions for why.

**6. Unit Test Coverage** - Add test coverage for the scraper's row-matching logic against in-memory HTML and for the finance service's validation and not-found translation, following the existing sibling test files' structure. Reference the spec's Testing Strategy for the full list of test functions and the seam used to avoid a live network call.

**7. Regression Check** - Confirm every existing test in the solution still passes unchanged, since this feature only adds new, not-yet-wired types. Run the full test suite to verify no existing behavior changed.
