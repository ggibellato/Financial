# Implementation Plan: F02. USD-Based Rate Resolution and Yesterday-or-Earlier Rule

**Prerequisites:**
- .NET 10 SDK, `Financial.Shared.Abstractions` (F01's `IFxRateStore`/`FxRateRecord` and F03's `IUsdRateFetcher`/`UsdRateFetchResult` already merged)
- No new NuGet packages

### Stage 1: Resolution Provider

**1. USD-Based Cross-Rate Provider** - Implement `UsdBasedExchangeRateProvider` as a new `IExchangeRateProvider`: the `from == to` short-circuit, the host-local today-vs-historical date classification, store-first/fetch-and-persist-second resolution for historical dates, always-live/never-persisted resolution for today and future dates, and the shared USD-anchored cross-rate formula with its zero-rate guard.

### Stage 2: Tests

**2. Test Coverage** - Cover same-currency short-circuit, store-hit and store-miss-then-persist paths for historical dates, the never-persist rule for today, all six currency-pair combinations for mathematical consistency, full and partial fetch failure handling, and the zero-stored-rate guard — including the cross-feature integration cases tying F01's store and F03's fetcher into F02's resolution.
