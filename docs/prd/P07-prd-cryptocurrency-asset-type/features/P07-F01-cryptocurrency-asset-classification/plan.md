# Implementation Plan: Cryptocurrency Asset Classification

**Prerequisites:**
- .NET SDK targeting `net10.0` (already installed, matches `Integrations/GoogleFinancialSupport/GoogleFinancialSupport.csproj`)
- xUnit + FluentAssertions (already referenced by `Tests/Financial.Infrastructure.Tests`)
- No new packages, configuration, or environment variables required

### Stage 1: Domain and Classification Data

**1. GlobalAssetClass Enum Extension** - Add `Cryptocurrency` as a new value to the `GlobalAssetClass` enum in `Financial.Domain/Entities/AssetClassification.cs`, appended after the existing values so no current member is renumbered. Leave `GlobalAssetClassMapping` untouched.

**2. Bitcoin Classification Data Update** - Update the existing `"Bitcoin"` entry in the embedded `AssetClassifications.json` resource so its classification reflects the new `Cryptocurrency` value, leaving every other field on that entry and every other entry in the file unchanged.

### Stage 2: Testing

**3. Classification Lookup Test Coverage** - Add a new test file covering `AssetClassificationLookup.TryGet` for the Bitcoin entry, confirming it now resolves to the `Cryptocurrency` class and that existing case-insensitive/trimmed lookup behavior and not-found handling are unaffected. Follow the spec for exact test function names and assertions.
