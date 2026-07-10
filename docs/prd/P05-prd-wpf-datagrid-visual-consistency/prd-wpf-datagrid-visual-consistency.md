# WPF DataGrid Visual Consistency

## 1. Executive Summary

WPF DataGrid Visual Consistency is a presentation-only fix for the Financial application's WPF desktop app (Financial.App). Today, DataGrids across different screens do not share a consistent visual language: some grids have no zebra row striping at all, and one grid applies a monospace font to its numeric columns while the rest of the app uses the default font. Numeric columns are already right-aligned everywhere, which is the correct convention and stays unchanged — but the surrounding font and row-striping inconsistency still makes the desktop app feel visually inconsistent as the user navigates between the Portfolio Summary, Transactions, Credits, and Shares Dividend Check screens.

This feature establishes the existing "Assets Current Prices" grid on the Read Assets Current Values screen as the canonical visual standard and brings five other grids — Portfolio Summary, Assets Transactions, Assets Credits, and the Shares Dividend Check tab's Dividend History and By Year grids — into alignment with it: same font family and size, the same alternating-row zebra pattern, and the same right-aligned numeric column convention already used across the app (including the reference grid's own Price column, which stays right-aligned). The shared visual style is centralized as reusable named resources in `App.xaml` so all six grids reference the same style definitions instead of duplicating XAML, removing existing copy-paste duplication in the process.

No business logic, data, or layer boundaries change. This is purely a WPF Presentation-layer styling change.

---

## 2. Problem and Opportunity

### The Problem

**Inconsistent zebra striping across grids**
- The Portfolio Summary grid and the Assets Transactions and Assets Credits grids have no alternating-row background at all — their `RowStyle`/`CellStyle` only handle selection highlighting, not zebra striping
- The Dividend History, By Year, and Assets Current Prices grids do have zebra striping, but its definition is copy-pasted identically into three separate XAML files, risking future drift

**Inconsistent font usage**
- The Portfolio Summary grid's numeric columns use a monospace font (`Consolas`) while the reference grid and most other grids use the app's default font, so numbers visually stand out differently depending on which screen the user is viewing

**Style duplication increases maintenance risk**
- The row/column-header/cell style XAML block is duplicated verbatim across `NavigationView.xaml` and `DividendCheckView.xaml`, meaning any future visual tweak must be applied in multiple places or grids will silently drift out of sync again

### The Opportunity

- Inconsistent zebra striping → apply one shared zebra `RowStyle` resource to every grid, including the three that currently have none
- Inconsistent font usage → drop the `Consolas` override so every grid uses the same default font, matching the reference grid exactly
- Style duplication → extract the shared `RowStyle`, `ColumnHeaderStyle`, `CellStyle`, and a numeric-column base style (right-aligned, matching the existing convention) into named resources in `App.xaml`, referenced via `StaticResource` from every grid, so there is a single source of truth for the visual standard going forward

---

## 3. Target Audience

### Primary Users

**Personal Investor (WPF desktop user)**
- Uses the WPF desktop app to review portfolio data across multiple screens in the same session (Portfolio Summary, Transactions, Credits, Dividend Check, Read Assets Current Values)
- Notices visual inconsistency between screens as a polish/quality signal, even though it has no functional impact
- Expects the desktop app to feel like one coherent product rather than a set of independently styled screens

---

## 4. Objectives

**Establish one visual standard for all WPF DataGrids**
- Metric: all 6 grids in scope (Assets Current Prices, Portfolio Summary, Assets Transactions, Assets Credits, Dividend History, By Year) use the same `FontFamily`, `FontSize`, and `FontWeight` for their base text

**Apply consistent zebra row striping everywhere**
- Metric: all 6 grids render an alternating row background using the same shared `RowStyle` resource, verified by all six grids referencing the same `StaticResource` key

**Keep numeric columns consistently right-aligned**
- Metric: 100% of numeric/currency-formatted columns across all 6 grids use `TextAlignment="Right"` via one shared style, including the reference grid's own Price column

**Eliminate style duplication**
- Metric: the row/header/cell style XAML exists exactly once, in `App.xaml`, and is referenced by `StaticResource` from every grid definition — zero duplicated style blocks remain in `NavigationView.xaml`, `DividendCheckView.xaml`, or `AssetPriceView.xaml`

---

## 5. User Stories

### F01. WPF DataGrid Visual Standardization

- As a user, I want every DataGrid in the WPF app to use the same font, size, and style so that the app feels visually consistent as I move between screens
- As a user, I want every DataGrid to show the same alternating row striping so that long rows of data remain easy to scan on every screen, not just some
- As a user, I want numeric and currency values to stay right-aligned consistently across every grid, using one shared style instead of ad-hoc per-grid definitions, so decimal points line up and I don't have to re-adjust my scanning pattern from screen to screen
- As a user, I want the existing color-coding (profit green/red, transaction type colors, bold totals) to keep working exactly as it does today, since only the font and row-striping consistency is changing, not the information itself or its alignment

---

## 6. Functionalities

### F01. WPF DataGrid Visual Standardization

**Capabilities:**
- A shared `DataGrid` base style, `RowStyle` (zebra striping via `AlternationIndex` + selection highlighting), `ColumnHeaderStyle`, `CellStyle`, and a numeric-column `TextBlock` element style are defined once as keyed resources in `Financial.App/App.xaml`, replacing the existing weak, incomplete global `DataGrid` style already present there
- All values in the shared resources match the current "Assets Current Prices" grid's visual definition (`FontSize="13"`, `AlternatingRowBackground="#F5F5F5"`, header background `#F0F0F0` with bold `#333333` text and `8`px padding, `LightYellow` selection highlight, numeric columns right-aligned) — this feature changes no alignment anywhere, only consolidates the already-consistent right-alignment convention into one shared resource
- The default `FontFamily` used across all shared resources is the WPF default (no explicit override), replacing the `Consolas` monospace override currently applied to the Portfolio Summary grid's numeric columns
- Every numeric/currency-formatted column across the 6 grids in scope — Price (Assets Current Prices), Quantity/Total Invested/% Portfolio/Total Credits/Current Value/% Profit/% Profit w/ Credits/XIRR (Portfolio Summary), Quantity/Unit Price/Fees/Total (Assets Transactions), Value (Assets Credits), and any numeric columns auto-generated in Dividend History and By Year — references the shared numeric-column style (right-aligned) via `ElementStyle`/`BasedOn`, or an equivalent per-column override where the column is defined via `DataGridTemplateColumn` (which cannot directly reference the shared `ElementStyle` resource)
- The Dividend History and By Year grids (`DividendCheckView.xaml`) use `AutoGenerateColumns="True"` with a code-behind `AutoGeneratingColumn` handler; that handler keeps generating numeric columns as right-aligned, consistent with the shared convention
- All existing conditional formatting is preserved unchanged: profit/loss green-red coloring on % Profit, % Profit w/ Credits, and XIRR in the Portfolio Summary grid; transaction-type coloring and bold Total column in the Assets Transactions grid; Total Invested's `SignedValueToBrushConverter`-based coloring; and any credit-type coloring in the Assets Credits grid
- Non-numeric columns (names, dates, types, action buttons) are unaffected and keep their current left alignment / existing behavior
- `Financial.App/Views/AssetPriceView.xaml` (the reference grid) is updated to consume the new shared resources instead of its own locally-defined styles; its Price column stays right-aligned, so the reference grid itself is 100% consistent with the new standard

**Experience:**
1. User opens any of the 6 screens in scope (Read Assets Current Values, Portfolio Summary, Asset Transactions tab, Asset Credits tab, Shares Dividend Check tab)
2. Every grid renders with the same font, same zebra row striping, and the same right-aligned numeric columns as before
3. Existing interactions (row selection highlight, sorting, update/delete action buttons, color-coded profit/loss and transaction-type text) behave exactly as before — only font and zebra striping consistency change; numeric alignment is unchanged
4. No new user-facing controls, no new data, no change to what information is shown — purely visual consistency

---

## 7. Out of Scope

**New grid features**
- No new columns, filters, sorting behavior, export, or pagination are introduced by this change

**Non-DataGrid controls**
- Buttons, ComboBoxes, charts (OxyPlot), and other non-DataGrid controls elsewhere in the app are not restyled by this feature

**React web frontend**
- This feature is WPF-only; the web app's tables/grids are not in scope

**Dark mode or theming system**
- No configurable theme or dark mode is introduced; the shared style resources use the same fixed color palette the reference grid already uses today

**Domain/Application/Infrastructure changes**
- No business logic, DTOs, services, or repository code changes; this is a Presentation-layer-only change

---

## 8. Dependency Graph

| # | Feature | Priority | Dependencies |
|---|---------|----------|--------------|
| F01 | WPF DataGrid Visual Standardization | 3 | None |

### Execution Waves
Features within the same wave can be built in parallel. A wave starts only after every feature in earlier waves is complete.

- **Wave 1**: F01

### Priority levels
- **1** = Essential — product does not work without it
- **2** = Important — significant value addition
- **3** = Desirable — incremental improvement

```mermaid
graph TD
  F01[DataGrid Standardization]
```

---

## 9. Acceptance Criteria

### F01. WPF DataGrid Visual Standardization
- [ ] Assets Current Prices, Portfolio Summary, Assets Transactions, Assets Credits, Dividend History, and By Year grids all use the same `FontFamily`, `FontSize`, and `FontWeight` for base row text
- [ ] All 6 grids render alternating row background striping using the same shared `RowStyle` resource (verified by identical `StaticResource` key usage)
- [ ] All 6 grids use the same `ColumnHeaderStyle` (background, foreground, font weight, padding, border)
- [ ] Every numeric/currency column across all 6 grids is right-aligned (`TextAlignment="Right"`) via the shared numeric-column style, including the Assets Current Prices grid's own Price column
- [ ] The `Consolas` font override on the Portfolio Summary grid's numeric columns is removed; those columns use the same default font as the rest of the app
- [ ] Profit/loss green-red coloring (% Profit, % Profit w/ Credits, XIRR, Total Invested) in the Portfolio Summary grid renders identically to before this change, with only font/zebra differing
- [ ] Transaction-type coloring and the bold Total column in the Assets Transactions grid render identically to before this change
- [ ] Selection highlighting (`LightYellow` background) behaves identically across all 6 grids
- [ ] The shared row/header/cell/numeric styles exist exactly once, as keyed resources in `Financial.App/App.xaml`; no duplicated style blocks remain in `NavigationView.xaml`, `DividendCheckView.xaml`, or `AssetPriceView.xaml`
- [ ] Existing sorting, row selection, and action-button (update/delete) behavior in Assets Transactions and Assets Credits grids is unaffected (regression check)

### Cross-Feature Integration
- [ ] N/A — this PRD has a single feature with no functional data dependencies on other features
