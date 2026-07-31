# Implementation Plan: F02. CashFlow Domain Model & Storage

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- No new NuGet packages beyond what `Financial.Investment.*` already uses

### Stage 1: Domain Layer

**1. Create Financial.CashFlow.Domain project** - Add the new project under `DDD/CashFlow`, matching `Financial.Investment.Domain`'s target framework and package set.

**2. Root aggregate and placeholder entities** - Add the `CashFlowData` root aggregate with 7 private collections and their `AddXxx` methods, and the 7 placeholder entity types (`Expense`, `ReserveMovement`, `CardStatement`, `RecurringBillTemplate`, `RecurringBillInstance`, `MaeLedgerEntry`, `InvestmentSnapshot`), each carrying only an identifier for now.

**3. Enums** - Add the `Category` (14 members), `PaymentSource` (3 members), and `CreditCard` (5 members) enums.

**4. Domain unit tests** - Add `Financial.CashFlow.Domain.Tests`, covering `CashFlowData`'s empty-by-default state and each `AddXxx` method's isolation from the other collections.

### Stage 2: Application Layer

**5. Create Financial.CashFlow.Application project** - Add the new project referencing `Financial.CashFlow.Domain`, matching `Financial.Investment.Application`'s package set.

**6. Repository abstraction** - Add `ICashFlowRepository`, exposing a read accessor and an `AddXxx` method for each of the 7 collections plus one `SaveChangesAsync`.

### Stage 3: Infrastructure Layer — Serialization

**7. Create Financial.CashFlow.Infrastructure project** - Add the new project referencing `Financial.CashFlow.Application`, `Financial.CashFlow.Domain`, and `Financial.Shared.Infrastructure`.

**8. Serializer and type resolver** - Add `ICashFlowSerializer`, `CashFlowSerializerAdapter`, and `CashFlowTypeInfoResolver`, mirroring the Investments serialization pattern for `CashFlowData` and its 7 entity types.

**9. First-run-safe loader** - Add `CashFlowLoader`, which returns an empty `CashFlowData` when the underlying file doesn't exist yet, and lets any other load failure (e.g. malformed JSON) propagate unchanged.

**10. Serialization tests** - Add tests covering round-tripping a populated `CashFlowData` through the serializer, and the loader's first-run/malformed-file/valid-file behavior.

### Stage 4: Infrastructure Layer — Repository, Configuration, and DI

**11. Repository implementation** - Add `CashFlowJsonRepository`, holding the in-memory `CashFlowData` and persisting the whole object back through `IJsonStorage` on save.

**12. Repository factory and provider selection** - Add `CashFlowRepositoryProvider`, `CashFlowRepositorySelectionOptions`, and `CashFlowRepositoryFactory`, mirroring `RepositoryFactory`'s Local/GoogleDrive provider construction using `Financial.Shared.Infrastructure`'s storage engine.

**13. Configuration keys and settings** - Add `CashFlowRepositoryConfigurationKeys` and `CashFlowRepositorySettingsOptions`, using a `CashFlow`-scoped configuration section distinct from Investments' existing keys.

**14. Dependency injection wiring** - Add `CashFlowInfrastructureServiceCollectionExtensions.AddFinancialCashFlowInfrastructure`, registering the repository and its dependencies for consumption by later CashFlow features.

**15. Repository and DI tests** - Add tests covering the repository's save behavior, the factory's provider-selection logic, and that the DI extension resolves a working `ICashFlowRepository`.

### Stage 5: Presentation Wiring and Solution Structure

**16. Wire into Financial.Api** - Call the new DI extension from `Program.cs` alongside the existing Investments registration, and add the corresponding `CashFlow` configuration section to `appsettings.json`.

**17. Update Financial.slnx** - Add the 3 new projects to the `DDD/CashFlow` solution folder and the 3 new test projects to `/Tests/`.

**18. Full-suite verification** - Build the entire solution, run every test project, and confirm `data-cashflow.json` is created empty on first run and round-trips correctly when populated, without any change to Investments' existing behavior or data file.
