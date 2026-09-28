# Plan: F02. Realized (Shares Only) in Financial.Web

## Prerequisites

- F01 (Realized (Shares Only) domain calculation and API exposure) is implemented and merged into this branch — `realizedGainLossSharesOnly` is already present on `PortfolioAssetSummaryItemDto` and `AssetDetailsDto` in `Financial.Web/src/api/generated/openapi.ts`.

## Phase 1: Portfolio Holdings grid

1. **Column and cell** - In `PortfolioHoldingsTab.tsx`, add a "Realized (Shares Only)" `SortableColumnHeader` and `DataTableCell` immediately after the existing "Realized Gain/Loss" ones, inside the `isHistoric` block, reading `item.realizedGainLossSharesOnly` directly.
2. **Sort accessor** - Add a `realizedGainLossSharesOnly` entry to the `sortAccessors` map, reading `r.item.realizedGainLossSharesOnly`.
3. **Footer sum** - Extend the footer IIFE with a `realizedGainLossSharesOnly` reduce and render its footer cell immediately after the "Realized Gain/Loss" footer item.

## Phase 2: Asset Summary panel

4. **Realized section field** - In `AssetSummaryTab.tsx`, add a "Realized (Shares Only)" field immediately after "Realized Gain/Loss" within the existing historic "Realized" section, reading `asset.realizedGainLossSharesOnly` and applying the same `signClass` styling.

## Phase 3: Test coverage

5. **Grid tests** - Add/update tests in `PortfolioHoldingsTab.test.tsx` per spec.md's Testing Strategy: header position, active-scope absence, value-from-field (not derived), sort, footer sum, coloring; confirm existing "Realized Gain/Loss" tests are unaffected.
6. **Panel tests** - Add/update tests in `AssetSummaryTab.test.tsx` per spec.md's Testing Strategy: field position/value, visibility, coloring; confirm existing "Realized Gain/Loss" tests are unaffected.
