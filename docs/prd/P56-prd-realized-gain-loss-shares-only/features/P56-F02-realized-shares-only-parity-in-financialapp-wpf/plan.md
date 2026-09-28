# Implementation Plan: F02. Realized (Shares Only) parity in Financial.App (WPF)

**Prerequisites:**
- .NET SDK matching `Financial.slnx` (existing solution toolchain)
- No new NuGet packages, config, or environment variables

### Stage 1: View-Model Computation

**1. Portfolio row shares-only property** - Add the shares-only realized figure and its display/coloring companions to `PortfolioAssetSummaryRowViewModel`, computed from the DTO fields already received in its constructor. Follow the exact pattern already used by its sibling `RealizedGainLoss`/`DisplayRealizedGainLoss`/`RealizedGainLossIsPositive`/`RealizedGainLossIsNegative` properties.

**2. Asset details shares-only property and footer aggregate** - Add the per-asset shares-only realized figure to `AssetDetailsViewModel`, set wherever `RealizedGainLoss` is set or reset, and add the corresponding footer aggregate computed the same way `FooterRealizedGainLoss` is, reset alongside it on data clear. Reference the spec's Decision D3 for the exact reset scope.

### Stage 2: WPF View Bindings

**3. Portfolio Holdings grid column and footer cell** - Add a new grid column and a new footer cell to `PortfolioHoldingsView.xaml`, positioned immediately after their "Realized Gain/Loss" counterparts, using the same right-alignment and positive/negative styling conventions already applied to those counterparts.

**4. Asset Summary panel field** - Add a new field to the "Realized" section of `PortfolioSummaryView.xaml`, positioned immediately below "Realized Gain/Loss", using the same visibility condition, alignment, and coloring convention as that existing field.

### Stage 3: Verification

**5. Manual parity check against Financial.Web** - Run the WPF app against sample historic data with non-zero credits and confirm the new value, coloring, and placement match the equivalent Financial.Web surfaces (or the F01 spec's expected values) for the same underlying data, per the PRD's cross-front-end parity acceptance criterion.
