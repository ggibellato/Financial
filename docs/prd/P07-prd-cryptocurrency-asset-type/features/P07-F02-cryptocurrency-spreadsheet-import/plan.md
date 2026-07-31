# Implementation Plan: Cryptocurrency Spreadsheet Import

**Prerequisites:**
- .NET SDK targeting `net10.0` (already installed)
- xUnit + FluentAssertions (already referenced by `Tests/Financial.Infrastructure.Tests` and `Tests/Financial.Domain.Tests`)
- No new packages, configuration, or environment variables required; no production code changes in this feature

### Stage 1: Infrastructure — Import Metadata Resolution Tests

**1. AssetMetadataResolver Test Coverage** - Add a new test file covering `AssetMetadataResolver.ResolveBrokerCurrency` and `ResolvePortfolioName` for the Coinbase broker, confirming currency resolves to GBP, an unmapped broker throws the expected error, and the default portfolio name resolves to "Cryptocurrency". Follow the spec for exact test names and how to construct the resolver without a real Google service dependency.

**2. CountryCodeResolver Test Coverage** - Add a new test file covering `CountryCodeResolver.FromCurrency`, confirming GBP resolves to the UK country code used by Coinbase-held assets.

### Stage 2: Domain — Cryptocurrency Asset Creation Tests

**3. Asset Creation Test Coverage** - Extend the existing `AssetTests.cs` with a test proving the asset-creation path used by the import pipeline produces the correct entity shape for Bitcoin's resolved data (blank ISIN/Exchange, ticker "BTC", UK country, Cryptocurrency class), and a second test documenting the current blank-ticker tolerance behavior per the spec's explicit decision not to add new validation.
