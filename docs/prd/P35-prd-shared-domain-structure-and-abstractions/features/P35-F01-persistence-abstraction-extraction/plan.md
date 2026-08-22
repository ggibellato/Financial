# Implementation Plan: F01. Persistence Abstraction Extraction

**Prerequisites:**
- .NET 10 SDK, existing solution builds and all tests pass on `main` before starting
- No new tools, libraries, or configuration required

### Stage 1: Extract the Persistence contracts into Financial.Shared.Abstractions

**1. New Persistence namespace** - Create a `Persistence/` folder under `Financial.Shared.Abstractions` and move `IJsonStorage`, `IRemoteFileClient`, `IRemoteFileClientFactory`, and `ReflectionJsonTypeInfoHelpers` into it unchanged, deleting them from `Financial.Shared.Infrastructure.Persistence`.

**2. IJsonStorageFactory contract** - Add a new `IJsonStorageFactory` interface to the same folder, exposing local and Google Drive storage construction with the reduced signatures described in the spec.

### Stage 2: Convert the concrete factory and fix up its own project

**3. Instance-based JsonStorageFactory** - Rewrite `Financial.Shared.Infrastructure.Persistence.JsonStorageFactory` from a static class into an instance class implementing `IJsonStorageFactory`, taking its cross-call dependencies through the constructor instead of as method parameters, and producing the same storage graph as today.

**4. Fix up Financial.Shared.Infrastructure's own consumers** - Update the `using` statements in `DebouncedJsonStorage`, `GoogleDriveJsonStorage`, `GoogleDriveStorageFactory`, `LocalJsonStorage`, and `JsonStorageSyncExtensions` so they resolve the relocated types from `Financial.Shared.Abstractions.Persistence`.

### Stage 3: Fix up every downstream consumer so the build stays green

**5. CashFlow Infrastructure** - Update `using` statements in `CashFlowLoader`, `CashFlowTypeInfoResolver`, `CashFlowJsonRepository`, and `CashFlowInfrastructureServiceCollectionExtensions`; change `CashFlowRepositoryFactory.CreateStorage` to construct a `JsonStorageFactory` instance and call its instance methods in place of the removed static ones, with no change to `CashFlowRepositoryFactory`'s own public constructor or DI registration.

**6. Investment Infrastructure** - Apply the identical set of changes to `InvestmentsLoader`, `InvestmentsTypeInfoResolver`, `InvestmentJsonRepository`, `InvestmentInfrastructureServiceCollectionExtensions`, and `InvestmentRepositoryFactory`.

**7. GoogleFinancialSupport integration** - Update the `using` statements in `GoogleFileClientFactory`, `GoogleFinancialSupportServiceCollectionExtensions`, `GoogleGenerator`, and `GoogleService` so they resolve `IRemoteFileClient`/`IRemoteFileClientFactory`/`IJsonStorage` from the new namespace, relying on the existing transitive project reference (no `.csproj` change in this feature).

**8. Test doubles and existing tests** - Update the `using` statements in `FakeSyncStatusStorage`, `ControllableJsonStorage`, and every other test file that references the relocated types, with no assertion changes.

### Stage 4: Add coverage for the new instance-based factory

**9. JsonStorageFactory unit tests** - Add a new test file covering the now-instantiable `JsonStorageFactory`: constructing a local storage instance, constructing a Google Drive storage instance (asserting it is debounce-wrapped), and the missing-`IRemoteFileClientFactory` error path.

**10. Full verification** - Run a full solution build and the full test suite (with coverage settings) to confirm no other project's behavior changed and the solution remains in a deployable state.
