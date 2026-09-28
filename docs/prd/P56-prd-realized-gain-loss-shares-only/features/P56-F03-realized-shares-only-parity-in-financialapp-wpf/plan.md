# Plan: F03. Realized (Shares Only) parity in Financial.App (WPF)

## Prerequisites

- F01 (Realized (Shares Only) domain calculation and API exposure) is implemented and merged into this branch — `RealizedGainLossSharesOnly` is already present on `PortfolioAssetSummaryItemDTO` and `AssetDetailsDTO`.

## Phase 1: View-model changes

1. **Row view model** - In `PortfolioAssetSummaryRowViewModel.cs`, add `RealizedGainLossSharesOnly` (set from `dto.RealizedGainLossSharesOnly` in the constructor), `DisplayRealizedGainLossSharesOnly`, and `RealizedGainLossSharesOnlyIsPositive`/`IsNegative`, immediately after their `RealizedGainLoss` counterparts.
2. **Asset details view model** - In `AssetDetailsViewModel.cs`, add a `RealizedGainLossSharesOnly` property mapped from `details.RealizedGainLossSharesOnly` in the asset-load method, and a `FooterRealizedGainLossSharesOnly` property computed as `assetItems.Sum(i => i.RealizedGainLossSharesOnly)` in the portfolio-summary load method, alongside their `RealizedGainLoss`/`FooterRealizedGainLoss` counterparts.
3. **Reset paths** - Reset the two new properties to `0m` in `Clear()` (alongside `FooterRealizedGainLoss`) and in `ClearAssetContext()` (alongside `RealizedGainLoss`).

## Phase 2: View changes

4. **Portfolio Holdings grid** - In `PortfolioHoldingsView.xaml`'s `PortfolioHoldingsTemplateHistoric`, add a "Realized (Shares Only)" `DataGridTemplateColumn` immediately after "Realized Gain/Loss", using the same `DataTrigger`-based positive/negative coloring pattern.
5. **Portfolio Holdings footer** - Add a "Realized (Shares Only):" stat to the footer `WrapPanel`, immediately after the existing "Realized Gain/Loss:" stat, bound to `AssetDetails.FooterRealizedGainLossSharesOnly` via `SignedValueToBrushConverter`.
6. **Asset Summary panel** - In `PortfolioSummaryView.xaml`'s `AssetSummaryTemplate`, insert a new grid row immediately after the existing "Realized Gain/Loss" / "Portfolio Weight" row (row 8) for "Realized (Shares Only):", add one `RowDefinition`, and renumber every subsequent `Grid.Row` attribute (old 9-16 → new 10-17).

## Phase 3: Test coverage

7. **Row view-model tests** - Add tests in `PortfolioAssetSummaryRowViewModelTests.cs` per spec.md's Testing Strategy: value set from DTO (not derived), display formatting, positive/negative flags.
8. **Asset details view-model tests** - Add tests in the file covering `AssetDetailsViewModel`'s portfolio-summary loading/footer/reset behavior, per spec.md's Testing Strategy: per-asset value, footer sum, reset on `Clear()`/`ClearAssetContext()`.

## Phase 4: Manual verification

9. **Visual check** - Per this repo's testing conventions, manually run the WPF app and confirm the new column, footer stat, and Asset Summary field render correctly, right-aligned, with correct coloring, and that the row insertion in `PortfolioSummaryView.xaml` didn't misalign any of the renumbered rows below it.
