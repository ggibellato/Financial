# F01. WPF DataGrid Visual Standardization — Technical Specification

## 1. Scope

**Included:**
- Centralizing the DataGrid visual standard (font, zebra row striping, column header style, cell selection style) as implicit, unkeyed WPF styles in `Financial.App/App.xaml`, so every `DataGrid`, `DataGridRow`, `DataGridCell`, and `DataGridColumnHeader` in the app inherits it automatically without per-view duplication
- Adding one keyed `TextBlock` style (`NumericColumnTextStyle`) in `App.xaml` for right-aligned numeric/currency columns (preserving the existing right-alignment convention already used across the app), referenced explicitly by every numeric column across the 6 grids in scope
- Applying the standardized style to: Assets Current Prices grid (reference), Portfolio Summary grid, Assets Transactions grid, Assets Credits grid, Dividend History grid, By Year grid
- Removing the now-redundant locally-duplicated `RowStyle`/`CellStyle`/`ColumnHeaderStyle`/`FontSize` XAML blocks from `AssetPriceView.xaml`, `DividendCheckView.xaml` (both grids), and `NavigationView.xaml` (Transactions and Credits grids), since they duplicate what the new implicit App.xaml styles already provide
- `DividendCheckView.xaml.cs`'s `ApplyValueColumnStyle` method (used by both grids' `AutoGeneratingColumn` handlers) keeps right-aligning generated numeric columns, consistent with the shared convention
- Preserving all existing conditional formatting: profit/loss green/red coloring (Portfolio Summary's % Profit, % Profit w/ Credits, XIRR columns), `SignedValueToBrushConverter` coloring on Total Invested, transaction-type coloring and bold Total column (Assets Transactions), bold black Value column (Assets Credits)

**Out of scope (per PRD Section 7):**
- New columns, filters, sorting, export, or pagination behavior
- Non-DataGrid controls (buttons, ComboBoxes, OxyPlot charts)
- The React web frontend
- Dark mode / theming system
- Any Domain, Application, or Infrastructure layer change
- Any change to numeric column alignment (already right-aligned everywhere; this feature keeps it that way, just via one shared style instead of per-grid duplication)

## 2. Component Overview

| File | Role |
|---|---|
| `Financial.App/App.xaml` | Replaces the existing weak, unkeyed `DataGrid` style with a complete one (adds `FontSize="13"`); adds new unkeyed styles for `DataGridRow` (zebra `AlternationIndex` trigger + selection highlight), `DataGridCell` (selection highlight), and `DataGridColumnHeader` (header look). Adds one keyed style, `NumericColumnTextStyle` (`TargetType="TextBlock"`, `TextAlignment="Right"`, `Padding="8"`), for numeric column cell content. |
| `Financial.App/Views/AssetPriceView.xaml` | Reference grid. Removes its local `ColumnHeaderStyle`/`RowStyle`/`CellStyle` (now inherited from `App.xaml`). Price column's `ElementStyle` changes to `BasedOn="{StaticResource NumericColumnTextStyle}"`, dropping its own duplicate right-alignment/padding declaration (alignment itself is unchanged). |
| `Financial.App/Components/NavigationView.xaml` | Portfolio Summary grid: no local row/cell/header styles exist today (nothing to remove) — it starts inheriting zebra/font/header automatically once `App.xaml` changes land. Its 4 `DataGridTextColumn` numeric columns switch from an inline `Right`+`Consolas` style to `BasedOn="{StaticResource NumericColumnTextStyle}"` (still right-aligned, Consolas dropped); its 4 `DataGridTemplateColumn` numeric cells (Current Value, % Profit, % Profit w/ Credits, XIRR) drop `FontFamily="Consolas"` from their inline `TextBlock` while keeping `TextAlignment="Right"` and their existing `DataTrigger`-based coloring untouched. Assets Transactions grid: removes its local selection-only `RowStyle`/`CellStyle` (now inherited with zebra added); its 4 numeric `DataGridTextColumn`s switch to `BasedOn="{StaticResource NumericColumnTextStyle}"` (Total column keeps its extra `FontWeight="Bold"` setter layered on top). Assets Credits grid: removes its local selection-only `RowStyle`/`CellStyle`; its Value column switches to `BasedOn="{StaticResource NumericColumnTextStyle}"`, keeping its `FontWeight="Bold"`/`Foreground="Black"` setters layered on top. |
| `Financial.App/Views/DividendCheckView.xaml` | Both grids (Dividend History, By Year) remove their local `ColumnHeaderStyle`/`RowStyle`/`CellStyle`/`FontSize` (now inherited from `App.xaml`). |
| `Financial.App/Views/DividendCheckView.xaml.cs` | `ApplyValueColumnStyle` is unchanged in behavior — its generated `Style` keeps `TextBlock.TextAlignmentProperty` set to `TextAlignment.Right`, consistent with the shared convention. |

No Domain, Application, or Infrastructure files are touched.

## 3. Requirements / Business Rules

- The `App.xaml` `DataGrid` style keeps every existing setter (`AutoGenerateColumns="False"`, `IsReadOnly="True"`, `AlternatingRowBackground="#F5F5F5"`, `GridLinesVisibility="Horizontal"`, `HeadersVisibility="Column"`, `SelectionMode="Single"`, `CanUserResizeRows="False"`, `CanUserSortColumns="True"`) and adds `FontSize="13"` — matching the reference grid's current value.
- The `DataGridRow` style zebra pattern uses the same rule already used by the reference grid: `Background="White"` by default, `Background="#F5F5F5"` when `ItemsControl.AlternationIndex=1`, and `Background="LightYellow"` on selection (both active and inactive selection states), including the `InactiveSelectionHighlightBrushKey`/`InactiveSelectionHighlightTextBrushKey` resource overrides already used by the reference grid.
- The `DataGridColumnHeader` style matches the reference grid exactly: `Background="#F0F0F0"`, `Foreground="#333333"`, `FontWeight="Bold"`, `Padding="8"`, `BorderBrush="#CCCCCC"`, `BorderThickness="0,0,1,1"`.
- The `DataGridCell` style matches the reference grid's selection behavior: `Background="LightYellow"`, `Foreground="Black"` on selection (active and inactive).
- Because these four styles are unkeyed (`TargetType` only, no `x:Key`), WPF's implicit style resolution applies them automatically to every `DataGrid`, `DataGridRow`, `DataGridCell`, and `DataGridColumnHeader` in the app that does not set its own local `Style`/`RowStyle`/`CellStyle`. No grid needs to reference them explicitly — the fix is to stop overriding them locally, not to add new references.
- `NumericColumnTextStyle` is deliberately keyed (not implicit) because an app-wide implicit `TextBlock` style would affect every label, button caption, and header text in the app, not just grid numeric cells. Every numeric/currency column must reference it explicitly via `ElementStyle`. It sets `TextAlignment="Right"`, preserving the alignment convention every grid already uses today — this feature does not change numeric alignment, only consolidates its definition into one shared resource and removes the stray `Consolas` font override.
- Numeric columns that need additional formatting beyond alignment (bold totals, conditional profit/loss colors, `SignedValueToBrushConverter`) apply `BasedOn="{StaticResource NumericColumnTextStyle}"` and layer their own `Setter`/`Trigger`/`DataTrigger` elements on top, exactly as they do today for their existing formatting — only the font source changes (alignment stays right, as before).
- `DataGridTemplateColumn`-based numeric cells (Portfolio Summary's Current Value, % Profit, % Profit w/ Credits, XIRR) cannot use `ElementStyle`/`BasedOn` the same way since their `TextBlock` is defined inline inside a `DataTemplate` with its own `Style`; for these, `FontFamily="Consolas"` is removed while `TextAlignment="Right"` stays on the `TextBlock` unchanged, and the inline `Style`'s `Text` binding and `DataTrigger`s (loading indicator, green/red coloring) are left unchanged.
- `DividendCheckView.xaml.cs`'s dynamically-built `Style` (used because those two grids have `AutoGenerateColumns="True"`) is unchanged: `FontWeight="Bold"`, `Foreground="Black"`, and `TextAlignment.Right` all stay as they are today.
- No column changes width, header text, sort behavior, alignment, or data binding — only font source and row background pattern change.

## 4. UX Flow

1. User opens the Read Assets Current Values screen, Portfolio Summary tab, Asset Transactions tab, Asset Credits tab, or Shares Dividend Check tab.
2. Every `DataGrid` renders with `FontSize="13"` in the app's default font (no monospace override), a `#F0F0F0` bold column header, and alternating `White`/`#F5F5F5` row backgrounds.
3. Every numeric/currency column's text remains right-aligned, exactly as before, including the reference grid's own Price column — only the font and zebra pattern become consistent.
4. Existing behaviors are visually unchanged aside from the above: row selection still highlights `LightYellow`, sorting still works by clicking headers, update/delete action buttons in Transactions/Credits grids still function, and all conditional coloring (profit/loss green/red, transaction type colors, bold totals) still renders exactly as before.
5. No new interaction is introduced; this is a pure re-render of existing data with a consistent visual style.

## 5. Error Handling

Not applicable — this is a read-only, presentation-only styling change with no data mutation, network call, or state transition that can fail. Per the PRD's Error Handling inclusion criteria (auth, payments, data loss risk, security, long-running/irreversible operations), none apply here.

## 6. Testing Strategy

The existing test suite (`Tests/Financial.Presentation.Tests`) covers ViewModels, chart builders, and helpers — it contains no XAML/view-rendering tests, and this change introduces no new C# business logic. Consistent with that pattern, this feature's testing strategy is:

- **Build verification:** `dotnet build` on `Financial.App` (and the full solution) must succeed after the `App.xaml` resource changes and every touched `.xaml`/`.xaml.cs` file, catching any XAML resource-lookup or markup compile error (e.g., a missing `StaticResource` key, an invalid `BasedOn` target).
- **Full existing test suite regression:** `dotnet test` across the solution must remain green — this change touches no ViewModel or business logic, so no existing test should be affected; a failure would indicate an unintended side effect.
- **Manual visual verification (acceptance test surrogate):** since there is no automated visual/UI test harness in this codebase, run the WPF app and visually confirm, for each of the 6 grids, against the PRD's Section 9 acceptance criteria:
  - Same font/size/zebra pattern across all 6 grids
  - Every numeric column still right-aligned, including the reference grid's Price column, with the `Consolas` override gone from Portfolio Summary
  - Profit/loss coloring, transaction-type coloring, and bold totals still render correctly
  - Row selection highlighting and sorting still work
  - Update/delete action buttons in Transactions and Credits grids still work

## 7. Assumptions / Decisions

(Auto-accepted per the Batch Mode policy at PRD-interview time; documented here for traceability.)

- **Font family:** drop the `Consolas` monospace override entirely; all grids use the WPF default font, matching the reference grid. *(Locked in during the PRD-stage clarification interview.)*
- **Numeric column alignment:** stays right-aligned everywhere, matching the convention every grid already used before this feature. *(An earlier draft of this spec incorrectly proposed switching to left-alignment; corrected after user review — right-alignment is the standard, consistent with decimal-point alignment conventions for financial figures.)*
- **Style reuse mechanism:** centralize as unkeyed/implicit WPF styles in `App.xaml` for `DataGrid`, `DataGridRow`, `DataGridCell`, `DataGridColumnHeader` (auto-applied everywhere, no per-grid reference needed), plus one keyed style for the numeric-column text style (must stay keyed to avoid affecting all `TextBlock`s app-wide). This is a refinement of the PRD-stage decision "centralize as named resources in App.xaml" — using implicit (unkeyed) styles for the row/cell/header/grid-level styling is the simplest mechanism that achieves zero per-grid duplication, consistent with "avoid overengineering."
- **Conditional formatting preserved exactly:** confirmed during PRD-stage interview — profit/loss coloring, `SignedValueToBrushConverter`, transaction-type coloring, and bold totals are untouched; only font/zebra change.
- **`DataGridTemplateColumn` cells:** since these can't consume `ElementStyle`/`BasedOn` the same way as `DataGridTextColumn`, the font-family fix is applied directly on their inline `TextBlock` declarations rather than forcing them onto the shared resource — documented here since the PRD didn't specify this mechanical detail.
- **`DividendCheckView.xaml.cs` code-behind:** left unchanged — it already right-aligns generated numeric columns, consistent with the shared convention.
