# Implementation Plan: F01. Investments Projects Rename & Domain Folder Restructuring

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- No new NuGet packages
- No environment variable or configuration changes

### Stage 1: Rename Domain and Application Projects

**1. Rename Financial.Domain** - Rename the `Financial.Domain/` folder and `Financial.Domain.csproj` to `Financial.Investment.Domain/` and `Financial.Investment.Domain.csproj`, and update every type in that project to the `Financial.Investment.Domain` namespace tree.

**2. Rename Financial.Application** - Rename the `Financial.Application/` folder and `Financial.Application.csproj` to `Financial.Investment.Application/` and `Financial.Investment.Application.csproj`, and update every type to the `Financial.Investment.Application` namespace tree, except `IJsonStorage` (handled in Stage 2).

**3. Update direct consumers' usings** - Update every `using Financial.Domain`/`using Financial.Application` statement across the solution (`Financial.Infrastructure`, `Financial.Api`, `Financial.App`, `Integrations/*`, `Tests/*`) to the new namespaces, and repoint their `ProjectReference`s to the renamed `.csproj` files.

### Stage 2: Extract the Shared Storage Engine

**4. Create Financial.Shared.Infrastructure project** - Create a new `Financial.Shared.Infrastructure/` project at repo root with no dependency on any Investments or CashFlow type.

**5. Move the storage engine** - Move `IJsonStorage` (currently in `Financial.Application/Interfaces`), `LocalJsonStorage`, `GoogleDriveJsonStorage`, `IRemoteFileClient`, and `IRemoteFileClientFactory` (currently in `Financial.Infrastructure/Persistence`) into `Financial.Shared.Infrastructure/Persistence`, updating their namespace and removing them from their prior locations.

**6. Move the storage engine's tests** - Create `Tests/Financial.Shared.Infrastructure.Tests/`, referencing only `Financial.Shared.Infrastructure`, and move `LocalJsonStorageTests.cs` and `GoogleDriveJsonStorageTests.cs` into it with updated namespaces and no assertion changes.

### Stage 3: Rename Financial.Infrastructure

**7. Rename Financial.Infrastructure** - Rename the `Financial.Infrastructure/` folder and `Financial.Infrastructure.csproj` to `Financial.Investment.Infrastructure/` and `Financial.Investment.Infrastructure.csproj`, update all remaining types (`JSONRepository`, `RepositoryFactory`, `RepositoryProvider`, `RepositorySelectionOptions`, `InvestmentsLoader`, `InvestmentsSerializerAdapter`, `InvestmentsTypeInfoResolver`, `IInvestmentsSerializer`, `IAssetPriceFetcher`, `IFinanceService`, pricing/finance services) to the `Financial.Investment.Infrastructure` namespace tree, and add a `ProjectReference` to `Financial.Shared.Infrastructure`.

**8. Rename Financial.Infrastructure.Tests** - Rename `Tests/Financial.Infrastructure.Tests/` to `Tests/Financial.Investment.Infrastructure.Tests/`, update its namespaces and `ProjectReference`s, keeping only the Investments-specific test files (`JsonRepositoryTests.cs`, `InvestmentsJsonSerializerTests.cs`, etc.) after Stage 2 moved the storage-engine tests out.

### Stage 4: Update Remaining Consumers and Solution Structure

**9. Update Financial.Api and Financial.App** - Update every `using` statement and `ProjectReference` in `Financial.Api` and `Financial.App` to the 3 renamed projects; add a `ProjectReference` to `Financial.Shared.Infrastructure` wherever code directly resolves `IJsonStorage`-family types via dependency injection.

**10. Update Integrations projects** - Update `GoogleFinancialSupport`, `ImportGoogleSpreadSheets`, and `WebPageParser` to reference the renamed projects (plus `Financial.Shared.Infrastructure` where `IRemoteFileClient`/`IRemoteFileClientFactory` are implemented or consumed), and rename their `RootNamespace`/`AssemblyName` from the `Financial.Infrastructure.Integrations.*` prefix to `Financial.Investment.Infrastructure.Integrations.*`.

**11. Restructure Financial.slnx** - Replace the existing `/DDD/` solution folder with `/Investment.DDD/` (containing the 3 renamed projects), an empty `/CashFlow.DDD/`, and `/Shared.DDD/` (containing `Financial.Shared.Infrastructure`), leaving the `/Integrations/`, `/Tests/`, and `/Presentation/` folders in place with their updated project paths.

**12. Update the Dockerfile** - Repoint every `COPY` instruction referencing the old project paths to the renamed projects and the new `Financial.Shared.Infrastructure` project.

### Stage 5: Verification

**13. Full solution build** - Build the entire solution and resolve any remaining compile error from a missed namespace, using statement, or project reference.

**14. Full test suite run** - Run every test project and confirm the same pass/fail outcome as the pre-rename baseline, with particular attention to the moved `LocalJsonStorageTests`/`GoogleDriveJsonStorageTests` and the Investments-specific tests left behind in `Financial.Investment.Infrastructure.Tests`.

**15. Manual smoke check** - Start `Financial.Api` (serving `Financial.Web`) and `Financial.App` locally and confirm existing Investments tabs still load and display data identically to before the rename.
