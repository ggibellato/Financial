# Implementation Plan: F01. Realized (Shares Only) in Financial.Web

**Prerequisites:**
- Node/npm toolchain already set up for `Financial.Web` (`npm install` run)
- No environment variables, no configuration file changes
- No backend, DTO, OpenAPI snapshot, or generated-types regeneration needed — `realizedGainLoss`/`totalCredits` are already present on both consumed DTOs

### Stage 1: Portfolio Holdings Grid

**1. Realized (Shares Only) Column** - Add the derived-value helper for the shares-only capital gain and use it to render a new "Realized (Shares Only)" column immediately after the existing "Realized Gain/Loss" column in `PortfolioHoldingsTab.tsx`, visible only in Historic scope, with the same numeric formatting and sign-based coloring as its neighbor. Reference the spec's Component Overview and Technical Decisions for exact placement and helper naming.

**2. Sortable Header and Sort Accessor** - Wire the new column into the grid's existing sort mechanism: add a sort accessor keyed to the derived value and a matching `SortableColumnHeader` positioned right after "Realized Gain/Loss", following the same pattern already used by that column.

**3. Footer Total** - Extend the Portfolio Holdings footer to compute and display the sum of the derived value across all currently-listed historic assets, as a new footer cell positioned after the existing "Realized Gain/Loss" footer cell, following that cell's existing rendering pattern.

### Stage 2: Asset Summary Panel

**4. Realized (Shares Only) Field** - Add a new field to the Asset Summary "Realized" section in `AssetSummaryTab.tsx`, positioned directly below the existing "Realized Gain/Loss" field, computing and displaying the same derived value with matching label/value styling and sign-based coloring. Follow the section's existing visibility condition so the field appears and disappears exactly when "Realized Gain/Loss" does.

### Stage 3: Test Coverage

**5. Portfolio Holdings Grid Tests** - Add tests to `PortfolioHoldingsTab.test.tsx` covering the new column's presence and position in Historic scope, its absence in Active scope, per-row value correctness, independent ascending/descending sort behavior, the footer sum, and positive/negative coloring driven by the derived value rather than the sibling "Realized Gain/Loss" value. Confirm existing "Realized Gain/Loss" tests remain unchanged and passing.

**6. Asset Summary Panel Tests** - Add tests to `AssetSummaryTab.test.tsx` covering the new field's presence, position, and value for a historic closed asset, its absence when the "Realized" section is hidden, and positive/negative coloring driven by the derived value. Confirm existing "Realized Gain/Loss" field tests remain unchanged and passing.

**7. Full Verification** - Run the full `Financial.Web` test suite, lint, and `npm run build` (`tsc -b && vite build`) to confirm no regressions and no type errors, per the spec's Testing Strategy.
