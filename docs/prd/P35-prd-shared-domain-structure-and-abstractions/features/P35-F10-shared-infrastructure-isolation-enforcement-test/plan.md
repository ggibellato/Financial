# Implementation Plan: F10. Shared Infrastructure Isolation Enforcement Test

**Prerequisites:**
- F06, F07, and F09 merged to `main` (the isolation this test enforces must already hold, or every theory case fails immediately)

### Stage 1: Add the enforcement test

**1. New isolation rule test** - Add `SharedInfrastructureIsolationRuleTests.cs` to `Tests/Financial.Architecture.Tests`, mirroring `ObservabilityIsolationRuleTests`'s `[Theory]`/`ProjectAssembly` shape, asserting `Financial.CashFlow.Infrastructure`, `Financial.Investment.Infrastructure`, `Integrations/GoogleFinancialSupport`, and `Integrations/WebPageParser` never reference `Financial.Shared.Infrastructure`.

### Stage 2: Prove the regression guard actually guards

**2. Manual revert check** - Temporarily reintroduce a `Financial.Shared.Infrastructure` reference in one of the four projects, confirm the new test fails naming that project, then revert.

### Stage 3: Full verification

**3. Full verification** - Run a full solution build and the full test suite (with coverage settings), confirming the new theory passes for all four projects and no other project's behavior changed.
