# Implementation Plan: F04. Spreadsheet Import — Closed Position Routing to Historic

**Prerequisites:**
- No new tools, libraries, or environment variables required
- Builds on `Integrations/GoogleFinancialSupport` as it stands on `main` (including F02's `ActiveBrokers`/`HistoricBrokers` split and F03's `ResolveHistoricPortfolio`)

### Stage 1: Closed-Position Detection and Historic Naming

**1. Resolver Additions** - Add a closed-marker check and a historic-portfolio-name resolution method to `AssetMetadataResolver`, keeping the existing tab-colour-based portfolio name resolution completely unchanged.

**2. Resolver Unit Tests** - Extend `AssetMetadataResolverTests` to cover the closed-marker check for both a closed and a normal portfolio name, and the historic-name resolution's delegation to the existing classification fallback.

### Stage 2: Routing in the Import Orchestrator

**3. Lazy Active/Historic Broker Creation** - Restructure `GoogleGenerator.ProcessBrokerAsync` to track an Active and a Historic broker per file, both created on first use with the same resolved currency, and registered into the correct collection when created.

**4. Sheet Routing** - Route each sheet to the Active or Historic broker based on the closed-marker check, using the historic-portfolio-name resolution for closed sheets and the normal resolved name otherwise; update `ProcessSheetAsync` to take the already-decided broker and portfolio name.

### Stage 3: Verification

**5. Manual Verification of Routing** - Since `GoogleGenerator` has no existing unit test harness, manually verify the end-to-end routing behavior (a closed sheet ends up in the correct broker's `HistoricBrokers` entry under the correct portfolio name; a non-closed sheet is unaffected; two different brokers each get their own `"Uncategorized"` portfolio) by tracing the code path, since this is the class's pre-existing testing boundary.
