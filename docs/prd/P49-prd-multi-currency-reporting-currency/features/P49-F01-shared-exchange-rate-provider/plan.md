# Implementation Plan: F01. Shared Exchange Rate Provider

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- No new external services, credentials, or configuration files — Frankfurter's base address (`https://api.frankfurter.app/`) is reused unchanged

### Stage 1: Shared Kernel Promotion

**1. Promote the Currency enum** - Create the widened `Currency` enum (`GBP`, `BRL`, `USD`) in `Financial.Shared.Abstractions`, in a new folder dedicated to currency/FX types.

**2. Promote the exchange-rate interface** - Create `IExchangeRateProvider` in the same shared location with its existing signature, unchanged.

### Stage 2: Vendor Project Relocation

**3. Create the Frankfurter vendor project** - Add a new `Integrations/Frankfurter` project following the existing vendor-isolation convention (`GoogleDrive`, `GoogleSheets`, `WebPageParser`), referencing only the shared kernel project.

**4. Move and enhance the Frankfurter provider** - Relocate `FrankfurterExchangeRateProvider` into the new project against the shared interface/enum, and add the nearest-earlier-date fallback behavior described in the spec.

**5. Register the new project in the solution** - Add the new project (and its test project, created in Stage 4) to `Financial.slnx`.

### Stage 3: Bounded-Context Rewiring

**6. Repoint CashFlow's Domain layer** - Retype `MaeLedgerEntry.SourceCurrency` to the shared `Currency`, add the necessary project reference, and delete the CashFlow-local `Currency` enum.

**7. Repoint CashFlow's Application layer** - Update `ControleMaeService` and `CurrencyParser` to reference the shared types instead of the deleted local interface/enum, with no behavioral change.

**8. Repoint CashFlow's Infrastructure DI** - Update `CashFlowInfrastructureServiceCollectionExtensions` to register the shared interface against the relocated Frankfurter implementation, and delete the old in-place implementation file.

**9. Register the provider in Investment's Infrastructure DI** - Add the same shared-interface registration to `InvestmentInfrastructureServiceCollectionExtensions` so later features (F02, F03) can resolve it without further DI changes.

### Stage 4: Tests and Verification

**10. Relocate and extend provider tests** - Move `FrankfurterExchangeRateProviderTests` into the new `Tests/Financial.Frankfurter.Tests` project and add coverage for the fallback behavior (exact-date hit, fallback hit, exhausted window).

**11. Update tests across repointed consumers** - Adjust `MaeLedgerEntryTests`, `ControleMaeServiceTests` and `CurrencyParserTests` to the shared namespace; confirm `ControleMaeServiceTests` passes with no assertion changes, per the feature's acceptance criterion.

**12. Add the vendor-isolation architecture test** - Add a rule test asserting the new `Integrations/Frankfurter` assembly has no dependency on either bounded context, matching the existing isolation rule tests for other vendor projects.

**13. Add an Investment DI resolution test** - Assert `IExchangeRateProvider` resolves from `Financial.Investment.Infrastructure`'s service collection.

**14. Full-suite verification** - Run the complete test suite and a full solution build to confirm no remaining reference to the deleted CashFlow-local `IExchangeRateProvider`/`Currency`/`FrankfurterExchangeRateProvider`, and that `ControleMãe`'s existing endpoint tests still pass unchanged.
