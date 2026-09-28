# Realized Gain/Loss — Shares Only

## 1. Executive Summary

Financial's Investment views (Portfolio Holdings and Asset Summary, in both Historic scope) show a single "Realized Gain/Loss" figure that blends two economically different things: capital gains/losses from selling shares, and credit-type distributions such as dividends, interest on equity (JCP), coupons, and securities-lending income. For the single user of this tool, who holds Brazilian FIIs (real estate investment funds) where dividend credits are tax-exempt while share capital gains are taxable, this blended number cannot be used directly for tax analysis — it has to be manually decomposed every time.

This feature adds a second, narrower figure — "Realized (Shares Only)" — next to the existing "Realized Gain/Loss" everywhere it already appears: the Historic Investments Portfolio Holdings grid (as a new column with its own footer total) and the Historic Investments Asset Summary panel. The new figure is the capital-gain/loss component alone, with all credits excluded; the existing "Realized Gain/Loss" total is left completely unchanged.

The underlying data already exists — both `PortfolioAssetSummaryItemDTO` and `AssetDetailsDTO` carry `RealizedGainLoss` (shares + credits) and `TotalCredits` (credits alone) fields today, and one existing calculation (`PortfolioHoldingsTab`'s historic profit-percent) already derives the shares-only capital gain as `RealizedGainLoss - TotalCredits` for an internal percentage. This feature surfaces that same derived value as its own labeled, visible amount, computed identically in Financial.Web and Financial.App (WPF) so both front ends stay at parity, with no backend, DTO, or API contract changes required.

## 2. Problem and Opportunity

**The Problem**
- **Blended tax-relevant figures**: "Realized Gain/Loss" mixes taxable share capital gains/losses with tax-exempt credit distributions (e.g., FII dividends in Brazil), so the number cannot be used as-is for tax reporting or planning.
- **Manual decomposition today**: The user must currently subtract credits from the total by hand (or eyeball the difference against the separate "Credits"/"Total Credits" figure) every time they need the shares-only component, for every asset and for the whole historic portfolio footer.
- **Inconsistent availability across front ends**: If added to only one client, the user would get accurate tax-relevant figures in one app (Web) but not the other (WPF), breaking the existing React/WPF parity the project maintains for every workflow.

**The Opportunity**
- Surface the already-computed shares-only component (`RealizedGainLoss - TotalCredits`) as its own labeled value in both the Portfolio Holdings grid and Asset Summary panel, eliminating manual subtraction — directly solves the blended-figure problem.
- Reuse the same fields (`RealizedGainLoss`, `TotalCredits`) both front ends already receive, so the new value can be computed identically in Web and WPF display code with no new backend or API work, guaranteeing parity by construction rather than by separate review.

## 3. Target Audience

### Primary Users

**Self-directed investor tracking Brazil + UK holdings**
- Uses Financial as a single-user, self-hosted tool to consolidate Brazilian and UK investment transactions, including Brazilian FIIs whose dividend credits are tax-exempt.
- Periodically reviews Historic Investments to understand realized performance and prepare tax-relevant figures for closed positions.
- Works from whichever front end is open at the time — Financial.Web (browser) or Financial.App (WPF desktop) — and expects the same figures and terminology in both.

## 4. Objectives

**Product Objectives**
- **Separate** taxable share capital gains/losses from tax-exempt credit distributions in every place "Realized Gain/Loss" is shown.
- **Preserve** the existing "Realized Gain/Loss" total exactly as-is, with zero behavior change to its value, sorting, or footer sum.
- **Match** the new metric's value and behavior between Financial.Web and Financial.App so neither front end is authoritative over the other.

**Success Metrics**
- 100% of surfaces currently showing "Realized Gain/Loss" (Portfolio Holdings grid + footer, Asset Summary panel) also show "Realized (Shares Only)", verified by the acceptance criteria below.
- 0 changes to existing "Realized Gain/Loss" test assertions (value, sort order, footer sum) across Web and WPF test suites after this feature ships.
- For any historic asset with `TotalCredits ≠ 0`, "Realized (Shares Only)" differs from "Realized Gain/Loss" by exactly `TotalCredits`, verified by unit tests in both Web and WPF.

## 5. User Stories

### F01. Realized (Shares Only) in Financial.Web
- As a user, I want to see a "Realized (Shares Only)" column next to "Realized Gain/Loss" in the Historic Investments Portfolio Holdings grid so that I can read the taxable share capital gain/loss for each asset without doing the subtraction myself
- As a user, I want the Portfolio Holdings footer to show the sum of "Realized (Shares Only)" across all listed assets so that I have a portfolio-level taxable-gain figure
- As a user, I want to sort the Portfolio Holdings grid by "Realized (Shares Only)" so that I can rank assets by taxable capital gain/loss independent of dividend size
- As a user, I want to see "Realized (Shares Only)" in the Historic Investments Asset Summary panel, next to the existing "Realized Gain/Loss" field, so that I get the same decomposition when reviewing a single asset in detail
- As a user, I want the new "Realized (Shares Only)" value to use the same positive/negative coloring convention as "Realized Gain/Loss" so that I can read gains vs. losses at a glance

### F02. Realized (Shares Only) parity in Financial.App (WPF)
- As a user, I want to see "Realized (Shares Only)" next to "Realized Gain/Loss" in the WPF Portfolio Holdings-equivalent grid so that I get the same taxable-gain figure whichever app I have open
- As a user, I want the WPF portfolio footer/summary to show the aggregate "Realized (Shares Only)" the same way the footer already aggregates "Realized Gain/Loss" so that totals match Financial.Web
- As a user, I want to see "Realized (Shares Only)" in the WPF asset-details view next to the existing "Realized Gain/Loss" field so that single-asset review is consistent with Financial.Web

## 6. Functionalities

### F01. Realized (Shares Only) in Financial.Web

**Capabilities:**
- New value computed client-side as `realizedGainLoss - totalCredits` per row/asset, using the existing `PortfolioAssetSummaryItemDto.realizedGainLoss`/`totalCredits` (Portfolio Holdings grid) and `AssetDetailsDto`-equivalent `asset.realizedGainLoss`/`asset.totalCredits` (Asset Summary panel) fields already present in the generated API types — no new DTO field, no OpenAPI snapshot change.
- Label: "Realized (Shares Only)", used verbatim as the grid column header, the `data-label` on the footer cell, and the Asset Summary field label.
- Displayed with the same numeric formatting (`formatN2`) and the same sign-based CSS coloring (`getProfitClass`/`signClass`) already used for "Realized Gain/Loss".
- Visible only where "Realized Gain/Loss" is currently visible: Portfolio Holdings grid and footer only render the Realized column set in Historic scope (unchanged existing condition — the new column follows the same condition), and the Asset Summary "Realized" section only renders for a fully-closed (zero-quantity) historic asset (unchanged existing condition).
- New sortable grid column "Realized (Shares Only)" positioned immediately after the existing "Realized Gain/Loss" column, using the same `SortableColumnHeader` pattern and numeric sort semantics (ascending/descending toggle) as the existing column.
- Footer sum: `Σ(realizedGainLoss - totalCredits)` across all currently-listed historic assets, rendered as a read-only input matching the existing footer cell pattern, with `data-label="Realized (Shares Only)"`.

**Experience:**
- Portfolio Holdings grid: a new column header "Realized (Shares Only)" appears to the right of "Realized Gain/Loss", numeric-aligned, clickable to sort (single-column sort, replacing any prior sort — consistent with existing grid sort behavior). Each row shows the per-asset value in green (positive), red (negative), or neutral (zero) using the same color classes as "Realized Gain/Loss". The footer row gains a matching total cell.
- Asset Summary panel: within the existing "Realized" section (which already shows the section title "Realized" and the "Realized Gain/Loss" field for closed historic assets), a new field "Realized (Shares Only)" appears immediately below "Realized Gain/Loss", styled identically (same label/value classes, same sign-based coloring).
- No new empty/loading/error states are introduced: the new column/field renders wherever its sibling "Realized Gain/Loss" renders, using data already loaded for that view, and is absent under the exact same conditions "Realized Gain/Loss" is absent (active scope, or a still-open position in Asset Summary).

### F02. Realized (Shares Only) parity in Financial.App (WPF)

**Capabilities:**
- Same derivation as F01: `RealizedGainLossSharesOnly => RealizedGainLoss - TotalCredits`, added as a computed property on `PortfolioAssetSummaryRowViewModel` (alongside its existing `RealizedGainLoss`/`TotalCredits` properties, which are already populated from the DTO) and on `AssetDetailsViewModel` (alongside its existing `RealizedGainLoss`/`TotalCredits` properties), plus a `FooterRealizedGainLossSharesOnly` aggregate on `AssetDetailsViewModel` computed the same way `FooterRealizedGainLoss` is (`assetItems.Sum(i => i.RealizedGainLossSharesOnly)`).
- Label: "Realized (Shares Only)", matching the Web label exactly (per the UI parity invariant — equivalent terminology across front ends).
- Displayed with the same numeric string format (`"N2"`) and the same positive/negative visual convention (`IsPositive`/`IsNegative`-style boolean flags, mirroring `RealizedGainLossIsPositive`/`RealizedGainLossIsNegative`) already used for `RealizedGainLoss`.
- Visible only where `RealizedGainLoss`/`FooterRealizedGainLoss` are currently visible in the WPF views bound to `PortfolioAssetSummaryRowViewModel` and `AssetDetailsViewModel` — no new visibility conditions introduced.

**Experience:**
- Wherever the WPF grid/view currently shows a "Realized Gain/Loss" column or field bound to `PortfolioAssetSummaryRowViewModel.DisplayRealizedGainLoss` / `AssetDetailsViewModel.RealizedGainLoss`, a sibling "Realized (Shares Only)" column/field appears immediately after it, bound to the new `DisplayRealizedGainLossSharesOnly` property, right-aligned (per this repo's numeric/currency grid convention) and using the same positive/negative coloring as its neighbor.
- The WPF footer/summary area that shows `FooterRealizedGainLoss` gains a matching `FooterRealizedGainLossSharesOnly` cell, reset to `0m` wherever `FooterRealizedGainLoss` is reset (e.g., on data reload/clear), consistent with existing reset logic.
- No new empty/loading/error states: the new column/field follows the exact visibility and reset lifecycle of its existing sibling property.

## 7. Out of Scope

- **Backend/API changes**: no new DTO field, no `Asset` domain property, no OpenAPI snapshot or generated-types regeneration — the value is derived entirely from `RealizedGainLoss` and `TotalCredits`, both already served today.
- **Net-of-withholding or net-of-fee variants**: the new metric uses the same gross `TotalCredits`/`RealizedGainLoss` basis as the existing total; no separate "net of withholding tax" realized figure is introduced.
- **Currency conversion or multi-currency aggregation changes**: the new metric inherits whatever currency/date logic already applies to `RealizedGainLoss` and `TotalCredits` — no new conversion logic is added.
- **New export, report, or tax-form generation**: this feature only adds a displayed figure; it does not feed a tax report, CSV export, or the P51 tax-reporting feature (a future feature could consume it, but that integration is not part of this PRD).
- **Historic Investments-only restriction**: not applicable — the metric follows "Realized Gain/Loss" wherever it already appears (both historic-scope Portfolio Holdings and Asset Summary), not a Historic-Investments-page-specific addition.
- **Changing the existing "Realized Gain/Loss" total's value, formula, sort key, or footer computation**: explicitly unchanged in both front ends.

## 8. Dependency Graph

| # | Feature | Priority | Dependencies |
|---|---------|----------|--------------|
| F01 | Realized (Shares Only) in Financial.Web | 1 | None |
| F02 | Realized (Shares Only) parity in Financial.App (WPF) | 1 | None |

### Execution Waves
Features within the same wave can be built in parallel. A wave starts only after every feature in earlier waves is complete.

- **Wave 1**: F01, F02

### Priority levels
- **1** = Essential — product does not work without it
- **2** = Important — significant value addition
- **3** = Desirable — incremental improvement

```mermaid
graph TD
  F01[Web Shares-Only]
  F02[WPF Shares-Only]
```

## 9. Acceptance Criteria

### F01. Realized (Shares Only) in Financial.Web
- [ ] Portfolio Holdings grid (Historic scope) shows a "Realized (Shares Only)" column immediately after "Realized Gain/Loss"
- [ ] Each row's "Realized (Shares Only)" value equals `realizedGainLoss - totalCredits` for that asset
- [ ] The "Realized (Shares Only)" column is sortable (ascending/descending) independently of the "Realized Gain/Loss" column
- [ ] The Portfolio Holdings footer shows a "Realized (Shares Only)" total equal to the sum of `(realizedGainLoss - totalCredits)` across all listed historic assets
- [ ] Positive "Realized (Shares Only)" values render in the same "positive" color class as positive "Realized Gain/Loss" values; negative values render in the same "negative" color class
- [ ] The "Realized (Shares Only)" column/footer cell does not render in Active scope, matching the existing "Realized Gain/Loss" column's visibility
- [ ] Asset Summary panel (historic, closed position) shows a "Realized (Shares Only)" field directly below "Realized Gain/Loss" within the existing "Realized" section, equal to `asset.realizedGainLoss - asset.totalCredits`
- [ ] Asset Summary panel does not show "Realized (Shares Only)" when the "Realized" section itself is hidden (open/active position)
- [ ] Existing "Realized Gain/Loss" value, sort behavior, and footer sum are unchanged by this feature (no regression in existing tests)

### F02. Realized (Shares Only) parity in Financial.App (WPF)
- [ ] `PortfolioAssetSummaryRowViewModel` exposes a `RealizedGainLossSharesOnly` value equal to `RealizedGainLoss - TotalCredits` and a formatted `DisplayRealizedGainLossSharesOnly` string
- [ ] `AssetDetailsViewModel` exposes a `RealizedGainLossSharesOnly` value equal to `RealizedGainLoss - TotalCredits` for the current asset
- [ ] `AssetDetailsViewModel` exposes a `FooterRealizedGainLossSharesOnly` aggregate equal to `assetItems.Sum(i => i.RealizedGainLossSharesOnly)`, reset to `0m` alongside `FooterRealizedGainLoss` on data reload/clear
- [ ] The WPF grid/view bound to `PortfolioAssetSummaryRowViewModel` shows "Realized (Shares Only)" immediately after "Realized Gain/Loss", right-aligned, with matching positive/negative coloring
- [ ] The WPF asset-details view shows "Realized (Shares Only)" next to the existing "Realized Gain/Loss" field
- [ ] The WPF footer/summary area shows the "Realized (Shares Only)" aggregate next to the existing "Realized Gain/Loss" footer value
- [ ] For the same underlying data, WPF's "Realized (Shares Only)" values (row, asset-details, and footer) numerically match Financial.Web's corresponding values
- [ ] Existing `RealizedGainLoss`/`FooterRealizedGainLoss` values and behavior are unchanged by this feature (no regression in existing WPF tests)

### Cross-Feature Integration
- [ ] (Not applicable — F01 and F02 have no Consumes relationship; each independently derives its value from the same source DTO fields already returned by the shared API for its own front end.)
