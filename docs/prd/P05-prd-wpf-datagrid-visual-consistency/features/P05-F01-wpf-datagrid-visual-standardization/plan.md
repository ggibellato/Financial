# F01. WPF DataGrid Visual Standardization — Implementation Plan

## Prerequisites

- None (Wave 1, no feature dependencies).

## Phase 1: Centralize the shared DataGrid style in App.xaml

1. **DataGrid base style** - Update the existing unkeyed `Style TargetType="DataGrid"` in `Financial.App/App.xaml` to add `FontSize="13"`, keeping all its current setters unchanged.
2. **DataGridRow style** - Add a new unkeyed `Style TargetType="DataGridRow"` in `App.xaml` reproducing the reference grid's zebra pattern (`AlternationIndex` trigger to `#F5F5F5`, `White` default, `LightYellow` on selection including inactive-selection resource overrides).
3. **DataGridCell style** - Add a new unkeyed `Style TargetType="DataGridCell"` in `App.xaml` reproducing the reference grid's selection highlight behavior.
4. **DataGridColumnHeader style** - Add a new unkeyed `Style TargetType="DataGridColumnHeader"` in `App.xaml` reproducing the reference grid's header look (background, foreground, bold, padding, border).
5. **NumericColumnTextStyle** - Add a new keyed `Style x:Key="NumericColumnTextStyle" TargetType="TextBlock"` in `App.xaml` with `TextAlignment="Right"` (preserving the existing convention) and `Padding="8"`, for explicit use by numeric/currency columns.

## Phase 2: Apply the standard across all six grids

1. **Reference grid (AssetPriceView.xaml)** - Remove its local `ColumnHeaderStyle`/`RowStyle`/`CellStyle` blocks (now inherited from `App.xaml`); change the Price column's `ElementStyle` to `BasedOn="{StaticResource NumericColumnTextStyle}"`, keeping its `FontWeight="Bold"`/`Foreground="Black"` setters and dropping the now-redundant duplicate right-alignment/padding declaration (alignment itself stays right).
2. **Portfolio Summary grid (NavigationView.xaml)** - Switch its 4 numeric `DataGridTextColumn`s (Quantity, Total Invested, % Portfolio, Total Credits) from the inline `Right`+`Consolas` style to `BasedOn="{StaticResource NumericColumnTextStyle}"` (still right-aligned, Consolas dropped); update its 4 `DataGridTemplateColumn` numeric cells (Current Value, % Profit, % Profit w/ Credits, XIRR) to drop `FontFamily="Consolas"` while keeping `TextAlignment="Right"`, preserving all existing `DataTrigger` coloring logic untouched.
3. **Assets Transactions grid (NavigationView.xaml)** - Remove its local selection-only `RowStyle`/`CellStyle` (now inherited with zebra added); switch its 4 numeric columns (Quantity, Unit Price, Fees, Total) to `BasedOn="{StaticResource NumericColumnTextStyle}"`, keeping the Total column's extra `FontWeight="Bold"` setter and the Type column's existing color-converter binding untouched.
4. **Assets Credits grid (NavigationView.xaml)** - Remove its local selection-only `RowStyle`/`CellStyle`; switch its Value column to `BasedOn="{StaticResource NumericColumnTextStyle}"`, keeping its `FontWeight="Bold"`/`Foreground="Black"` setters.
5. **Dividend History and By Year grids (DividendCheckView.xaml)** - Remove their local `ColumnHeaderStyle`/`RowStyle`/`CellStyle`/`FontSize` (now inherited from `App.xaml`).
6. **Auto-generated column alignment (DividendCheckView.xaml.cs)** - No change needed; `ApplyValueColumnStyle` already right-aligns generated numeric columns, consistent with the shared convention.

## Phase 3: Verification

1. **Build and full test suite** - Run `dotnet build` and `dotnet test` across the solution to confirm no markup compile errors and no regression in the existing ViewModel/helper/builder test suite.
2. **Manual visual pass** - Launch the WPF app and visually confirm all 6 grids share the same font/size/zebra pattern, all numeric columns remain right-aligned (including the reference grid's Price column, with the Consolas override gone), and all existing conditional coloring, selection highlighting, sorting, and action buttons still work as before.
