# Implementation Plan: F03. Resilience Abstraction Extraction

**Prerequisites:**
- F01 and F02 merged to `main`
- .NET 10 SDK, existing solution builds and all tests pass on `main` before starting

### Stage 1: Move the exception and fix up every consumer

**1. New Resilience namespace** - Create a `Resilience/` folder under `Financial.Shared.Abstractions` and move `TransientStorageException` into it unchanged.

**2. Financial.Shared.Infrastructure's own consumer** - Update `TransientRetryPolicy`'s `using` statement so `IsRetryable` keeps catching the relocated exception type.

**3. GoogleFinancialSupport and tests** - Update the `using` statements in `GoogleTransientErrorTranslator` and every test file/test double that references `TransientStorageException`, with no assertion changes.

### Stage 2: Full verification

**4. Full verification** - Run a full solution build and the full test suite (with coverage settings) to confirm no project's behavior changed and the solution remains in a deployable state.
