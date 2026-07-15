# Implementation Plan: F03. AssetClassification Historic Portfolio Metadata

**Prerequisites:**
- No new tools, libraries, or environment variables required
- Builds on `Integrations/GoogleFinancialSupport` as it stands on `main`

### Stage 1: AssetClassification Data Model

**1. HistoricPortfolio Field** - Add an optional `HistoricPortfolio` member to `AssetClassificationEntry` (defaulting to null, so existing construction call sites keep compiling unmodified) and a matching optional property to the internal `AssetClassificationJson` deserialization model.

**2. Resolution Method** - Add an `UncategorizedHistoricPortfolioName` constant and a `ResolveHistoricPortfolio` method to `AssetClassificationLookup` that returns the classified value when present, or the fallback name otherwise.

### Stage 2: Tests

**3. Resolution and Fallback Tests** - Extend `AssetClassificationLookupTests` to cover a classified value resolving correctly, an unclassified-but-known asset falling back to the default name, and an unknown asset name falling back to the same default.

**4. Backward-Compatibility Test** - Add a test confirming that an existing classification entry without the new field still deserializes successfully.
