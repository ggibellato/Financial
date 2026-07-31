# Implementation Plan: Current Values Portfolio Scope — Web Frontend

**Prerequisites:**
- .NET SDK targeting `net10.0` and Node/npm toolchain for `Financial.Web` (both already installed)
- No new packages; no new environment variables

### Stage 1: Backend Portfolio Scope Configuration

**1. AssetPriceFetch Config Update** - Add the Coinbase/Cryptocurrency portfolio to the fixed scope list served by `/asset-price-fetch`, alongside the existing XPI entries.

### Stage 2: Web API Client Extension

**2. getCurrentPrice Parameter Extension** - Extend the typed API client's current-price function to accept optional asset-classification and broker-name parameters, appended to the request only when provided so existing non-crypto calls are unaffected.

**3. API Client Test Coverage** - Add test coverage for the new crypto-shaped request URL, and confirm the existing non-crypto request URL is unchanged.

### Stage 3: Current Values Page Wiring

**4. Cryptocurrency Asset Support** - Fix the page's asset-scope filter so Cryptocurrency-class assets with no exchange are no longer silently excluded, and pass each asset's classification and owning broker name into the price-fetch call.

**5. Current Values Page Test Coverage** - Add test coverage proving a Coinbase/Bitcoin-scoped asset is included and fetched with the correct classification and broker name, following the spec's test functions.
