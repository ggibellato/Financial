# Implementation Plan: F09. GoogleFinancialSupport Integration Reference Realignment

**Prerequisites:**
- F01 and F03 merged to `main` (F01 moved `IRemoteFileClient`/`IRemoteFileClientFactory`, F03 moved `TransientStorageException` — both already consumed correctly by `GoogleFinancialSupport` via a transitive reference this feature makes explicit)
- .NET 10 SDK, existing solution builds and all tests pass on `main` before starting

### Stage 1: Declare the dependency explicitly

**1. Add the ProjectReference** - Add an explicit `ProjectReference` to `Financial.Shared.Abstractions` in `GoogleFinancialSupport.csproj`, replacing the implicit transitive dependency the project has relied on since F01/F03 landed.

### Stage 2: Full verification

**2. Full verification** - Run a full solution build and the full test suite (with coverage settings), confirming `Integrations/GoogleFinancialSupport` still builds standalone and no test's behavior changed.
