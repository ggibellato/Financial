# Implementation Plan: F07. Controle Mae Ledger

**Prerequisites:**
- Existing `Financial.CashFlow.Domain`/`.Application`/`.Infrastructure` projects and `data-cashflow.json` repository pattern from F02
- No new NuGet packages (Frankfurter is called via the built-in `HttpClient`/`System.Net.Http.Json`)
- No repository interface changes needed — F02's existing `GetMaeLedgerEntries`/`AddMaeLedgerEntry` are sufficient

### Stage 1: Domain Layer

**1. Currency enum** - Add the 2-member `Currency` enum (BRL, GBP).

**2. Flesh out MaeLedgerEntry** - Replace the placeholder with its real fields (date, description, note, source currency, nullable BRL/GBP values) and a `Create` factory, plus an `UpdateValues` method for the manual-override edit.

**3. Domain tests** - Add tests for the entity's factory and its update method.

### Stage 2: Application Layer

**4. Mae ledger DTOs** - Add the entry read model, the create request, and the values-only update request.

**5. Currency parser** - Add a parser for `Currency` string values, following the existing enum-parsing convention.

**6. Exchange-rate abstraction** - Add `IExchangeRateProvider` with a single historical-rate lookup method that returns a nullable rate.

**7. Controle mae service** - Add `IControleMaeService`/`ControleMaeService` covering entry creation (future-date rejection, field validation, rate lookup with soft-degrade to a null converted value on failure), listing entries by month, and the manual currency-values override.

**8. Register the new service** - Add `IControleMaeService` to the existing `CashFlowApplicationServiceCollectionExtensions`.

**9. Application-layer tests** - Add service tests covering successful and failed rate lookups, future-date rejection, field validation, month filtering, the manual override leaving other fields untouched, and the currency parser.

### Stage 3: Infrastructure Layer

**10. Frankfurter exchange-rate provider** - Add the `IExchangeRateProvider` implementation that calls the Frankfurter API for a given date and currency pair, catching any HTTP or parsing failure and returning a null rate instead of throwing.

**11. Register the HTTP client** - Register `FrankfurterExchangeRateProvider` as a typed `HttpClient` in the existing `CashFlowInfrastructureServiceCollectionExtensions`.

**12. Infrastructure tests** - Add tests for the provider covering a successful parse and each failure mode (non-2xx, malformed body, thrown exception).

### Stage 4: Presentation Layer

**13. Controle mae controller** - Add HTTP endpoints for creating an entry, listing a month's entries, and updating an entry's currency values, translating validation and not-found failures to 400/404.

**14. API integration tests** - Add endpoint tests covering the full create-entry → get-month → update-values round trip over HTTP (with a stubbed exchange-rate provider), including the invalid-currency, future-date, and not-found error responses.

### Stage 5: Verification

**15. Full-suite validation** - Build the entire solution and run every test project, confirming no regression to Investments or the existing CashFlow features from F01-F06.

**16. Manual verification** - Run `Financial.Api` locally and exercise the new endpoints directly (create an entry with a real past date and confirm the live Frankfurter lookup populates both currencies, create an entry with a future date and confirm the 400, manually override one entry's values and confirm the other fields are untouched) to confirm the behavior matches the acceptance criteria end-to-end.
