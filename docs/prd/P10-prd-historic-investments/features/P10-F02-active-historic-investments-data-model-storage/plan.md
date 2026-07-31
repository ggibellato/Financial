# Implementation Plan: F02. Active/Historic Investments Data Model & Storage

**Prerequisites:**
- No new tools, libraries, or environment variables required
- Builds on `Financial.Domain`, `Financial.Application`, `Financial.Infrastructure`, and `Integrations/GoogleFinancialSupport` as they stand on `main` (including F01's `PositionType` work)

### Stage 1: Domain Layer

**1. Investments Active/Historic Split** - Replace `Investments`'s single `Brokers` collection with two independent collections, `ActiveBrokers` and `HistoricBrokers`, each backed the same way `Brokers` is today. Replace the single `AddBroker` method with `AddActiveBroker` and `AddHistoricBroker`. `Broker`/`Portfolio`/`Asset` entities are untouched.

**2. Domain Unit Tests** - Update `InvestmentsTests.cs` to cover `AddActiveBroker` and `AddHistoricBroker`, confirming the two collections populate independently of each other.

### Stage 2: Repository Abstraction and Infrastructure

**3. InvestmentScope Enum and IRepository Signature** - Add a new `InvestmentScope` enum (`Active`/`Historic`) and give `IRepository`'s four query methods an optional scope parameter defaulting to `Active`, so every existing caller keeps compiling and keeps today's behavior unchanged.

**4. JSONRepository Scope Resolution** - Update `JSONRepository` to resolve each query against `ActiveBrokers` or `HistoricBrokers` based on the scope argument. `SaveChangesAsync` continues to serialize the whole `Investments` instance unchanged.

**5. Import Tool Compile Fix** - Update `GoogleGenerator`'s one call to the old `AddBroker` to call `AddActiveBroker` instead, purely to keep the import tool compiling until F04 implements real routing logic there.

**6. Test-Stub Signature Updates** - Update the 8 test files that implement `IRepository` directly with an in-file stub, adding the new scope parameter to each stub's method signatures.

**7. Test Data Fixture Migration** - Update both copies of `data.test.json` (Infrastructure and Api test projects) from the old `{ "Brokers": [...] }` shape to the new `{ "ActiveBrokers": [...], "HistoricBrokers": [...] }` shape, preserving the existing broker/asset content under `ActiveBrokers`.

**8. Infrastructure Unit Tests** - Extend `InvestmentsJsonSerializerTests` to cover round-tripping both collections and the missing-key-defaults-to-empty behavior. Extend `JsonRepositoryTests` to cover scope-parameterized queries returning only the requested collection's data.

### Stage 3: Application Layer Cleanup

**9. Remove Sort-Last Special-Casing** - Remove `NavigationMapper.OrderByNameWithEncerradasLast` and replace its three call sites (portfolios within a broker, assets within a portfolio, brokers in `NavigationService.GetBrokers`) with plain alphabetical ordering. Leave `NavigationMapper.IsEncerradas` itself in place, since other services still depend on it.

**10. Application Test Updates** - Rewrite the three existing "sort Encerradas last" tests in `NavigationServiceTests` to assert plain alphabetical ordering instead, confirming the special-casing no longer exists.
