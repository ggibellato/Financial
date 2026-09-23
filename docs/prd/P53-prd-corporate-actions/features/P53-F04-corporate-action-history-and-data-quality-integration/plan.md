# Implementation Plan: F04. Corporate Action History and Data-Quality Integration

**Prerequisites:**
- No new tools/libraries — uses the existing .NET 8 / System.Text.Json / FluentAssertions / xUnit stack already in `Financial.Investment.*`
- No configuration or environment variables
- Builds entirely on F01/F02/F03's already-shipped `CorporateAction`/`TaxClassification`/`Asset.RecordCorporateAction` family — no Domain change of any kind, only new Application-layer projections and one new controller route

**PR slicing note:** F04 is structurally much lighter than F01-F03 — it adds no Domain mutation logic and no new persisted field (see spec.md §1, §6). Counting every non-test production file across the whole feature: `CorporateActionDTO.cs` (new), `AssetDetailsDTO.cs` (modified), `NavigationMapper.cs` (modified), `NavigationService.cs` (modified), `DataQualityReportDTOs.cs` (modified), `DataQualityReportService.cs` (modified), `CorporateActionSummaryItemDTO.cs` (new), `ICorporateActionQueryService.cs` (new), `CorporateActionService.cs` (modified), `InvestmentApplicationServiceCollectionExtensions.cs` (modified), `CorporateActionsController.cs` (modified) — 11 files total, which does not fit in one PR under CLAUDE.md's 8-non-test-file guideline. It splits cleanly into two, along a seam that also happens to match the feature's own acceptance criteria:
- PR1 = Phase 1 (6 non-test files): everything that **extends an already-existing endpoint** — `AssetDetailsDTO`'s per-asset history and `DataQualityReportDTO`'s new warning category. This alone satisfies all five PRD §9 F04 acceptance criteria and both F04 rows of Cross-Feature Integration — a complete, independently-shippable vertical slice.
- PR2 = Phase 2 (5 non-test files): the **one genuinely new endpoint** — the portfolio-wide corporate-action list. This completes the PRD §6 F04 "Provides" bullet ("Per-asset and portfolio-wide... history/audit list") for F05/F06's future use, but is not itself gated by any PRD §9 F04 acceptance criterion, making it a natural, lower-risk second slice.

### Phase 1: Per-Asset History and Data-Quality Warning Integration

**1. Per-Asset Corporate Action History** - Add `CorporateActionDTO` (full flat shape mirroring `CorporateAction`'s own fields, plus a computed `CalculationStatus?`) per spec.md §4. Add `CorporateActions` to `AssetDetailsDTO`. Add `NavigationMapper.MapCorporateAction(CorporateAction, Asset)`, resolving the linked `TaxClassification`'s status by filtering `asset.TaxClassifications` for `SourceType.CorporateAction` + matching `SourceId` + `Status == Active`. Wire it into `NavigationService.GetAssetDetails`, ordered by `EffectiveDate` ascending.

**2. Data-Quality Warning Integration** - Add `CorporateActionAwaitingTaxReviewFinding` and `DataQualityReportDTO.CorporateActionsAwaitingTaxReview` per spec.md §4. Extend `DataQualityReportService.GenerateReport` with a query over `allHoldings`'s `Asset.CorporateActions.Where(ca => ca.IsReceivingRole)`, matched to their active linked `TaxClassification` (same filter as step 1), kept only when `CalculationStatus == RequiresReview`, ordered by `BrokerName`/`PortfolioName`/`AssetName` matching every other finding in the method.

**3. Regenerate Contracts** - Regenerate the OpenAPI snapshot (`UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests`) for the enriched `AssetDetailsDTO`/`DataQualityReportDTO` shapes, then regenerate and commit `Financial.Web`'s generated API types (`npm run generate-api-types`).

### Phase 2: Portfolio-Wide Corporate Action List

**4. Query Contract and Summary DTO** - Add `CorporateActionSummaryItemDTO` (mirrors `TransactionSummaryItemDTO`'s lighter, asset-name-qualified shape) and `ICorporateActionQueryService.GetCorporateActionsByPortfolio(brokerName, portfolioName, scope)` per spec.md §4.

**5. Service Implementation** - Extend `CorporateActionService` to also implement `ICorporateActionQueryService`: blank broker/portfolio name returns an empty list (matching `TransactionService.GetTransactionsByPortfolio`'s existing early-return shape); otherwise flatten `_repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName, scope)`'s assets' `CorporateActions`, map via a new private `MapSummaryItem` (same `TaxClassification` lookup as `NavigationMapper.MapCorporateAction`), ordered by `EffectiveDate`.

**6. DI and API Route** - Update `InvestmentApplicationServiceCollectionExtensions` to register `CorporateActionService` as a singleton and resolve both `ICorporateActionService` and the new `ICorporateActionQueryService` from it, the same two-interfaces-one-singleton pattern already used for `CreditService`/`TransactionService`. Add `GET corporate-actions/portfolio/{brokerName}/{portfolioName}` to `CorporateActionsController` (`[FromQuery] string? scope`, 400 if either name is blank), mirroring `TransactionsController.GetTransactionsByPortfolio` exactly.

**7. Regenerate Contracts** - Regenerate the OpenAPI snapshot and `Financial.Web`'s generated API types for the new route and `CorporateActionSummaryItemDTO`.
