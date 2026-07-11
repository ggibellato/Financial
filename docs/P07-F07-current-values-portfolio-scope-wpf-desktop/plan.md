# Implementation Plan: Current Values Portfolio Scope — WPF Desktop

**Prerequisites:**
- .NET SDK targeting `net10.0-windows` (already installed)
- xUnit + FluentAssertions (already referenced by `Tests/Financial.Presentation.Tests`)
- No new packages; no production ViewModel/service code changes in this feature

### Stage 1: WPF Portfolio Scope Configuration

**1. AssetPriceFetch Config Update** - Add the Coinbase/Cryptocurrency portfolio to the WPF app's fixed price-fetch scope, alongside the existing XPI entries.

### Stage 2: AssetPriceFetchViewModel Test Coverage

**2. Coinbase/Bitcoin Request Shape Coverage** - Add a new test file for `AssetPriceFetchViewModel` proving a Coinbase-scoped, blank-exchange Bitcoin asset produces a price-fetch request carrying the correct asset classification and broker name, and that the pre-existing non-crypto scoped assets remain unaffected.
