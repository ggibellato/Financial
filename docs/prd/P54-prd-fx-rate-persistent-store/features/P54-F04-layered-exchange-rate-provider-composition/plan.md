# Implementation Plan: F04. Layered Exchange Rate Provider Composition

**Prerequisites:**
- .NET 10 SDK; F01's `IFxRateStore`, F02's `UsdBasedExchangeRateProvider`, and F03's batched `FrankfurterExchangeRateProvider` already merged to `main`
- No new NuGet packages, no configuration changes

### Stage 1: DI Composition Swap

**1. Investment and CashFlow Provider Registration** - Replace the inner factory passed to `InMemoryCachedExchangeRateProvider` in both `InvestmentInfrastructureServiceCollectionExtensions.AddFinancialInfrastructure` and `CashFlowInfrastructureServiceCollectionExtensions.AddFinancialCashFlowInfrastructure`, so it constructs the new USD-based resolver over the persistent store and batched fetcher instead of decorating the Frankfurter provider directly. Keep both files' `TryAddSingleton` guard and surrounding registrations exactly as they are today.

### Stage 2: Tests

**2. Composition Verification** - Extend both DI extension test files to confirm the resolved `IExchangeRateProvider` is still the shared `InMemoryCachedExchangeRateProvider` singleton, now backed by the new resolver, and add the cross-feature integration test proving a rate resolves end-to-end through the full chain to the same public interface every existing caller already uses. Re-run the full solution test suite (including `FrankfurterIsolationRuleTests` and every existing FX-consuming service's test suite) to confirm nothing else needed to change.
