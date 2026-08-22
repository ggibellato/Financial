# Implementation Plan: F04. Configuration Abstraction Extraction

**Prerequisites:**
- F01, F02, and F03 merged to `main`
- .NET 10 SDK, existing solution builds and all tests pass on `main` before starting

### Stage 1: Move the resolver and fix up both consumers

**1. New Configuration namespace** - Create a `Configuration/` folder under `Financial.Shared.Abstractions` and move `RepositoryProviderResolver` into it unchanged, then delete the now-empty `Financial.Shared.Infrastructure/Configuration/` folder.

**2. CashFlow and Investment Infrastructure** - Update the `using` statements in `CashFlowInfrastructureServiceCollectionExtensions` and `InvestmentInfrastructureServiceCollectionExtensions` so `BuildRepositoryOptions` keeps resolving the configured provider from the new namespace.

### Stage 2: Full verification

**3. Full verification** - Run a full solution build and the full test suite (with coverage settings) to confirm no project's behavior changed and the solution remains in a deployable state.
