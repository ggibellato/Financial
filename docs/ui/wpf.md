# WPF UI Rules

## React UX source of truth

`Financial.Web` is the UX source of truth.

Before implementing or changing a WPF workflow:

1. Inspect the corresponding React feature.
2. Read the relevant `docs/ui/` rules.
3. Preserve the React-defined task sequence, terminology, information hierarchy,
   field order, validation wording, action priority, status meaning, financial
   formatting, totals behavior, and user-visible outcomes.
4. Adapt presentation and mechanics only where a high-quality WPF experience
   needs desktop-specific keyboard efficiency, window sizing, DataGrid behavior,
   focus behavior, or native controls.
5. Document intentional differences and explain why they preserve the same user
   outcome or improve accessibility/usability.

Do not use an old WPF pattern as the reason to diverge from the React target.

## Architecture

- Follow MVVM strictly.
- Keep business logic, persistence, service calls, and domain calculations out
  of code-behind.
- Use bindings, commands, shared styles, ResourceDictionaries, and the
  established validation mechanism.
- Use code-behind only for narrowly scoped view concerns that cannot reasonably
  be expressed elsewhere; explain such use in the change.

## Component and theme system

- Use the Fluent-themed WPF controls adopted in `ADR-004`.
- Reuse shared styles and ResourceDictionaries.
- Use theme-aware resources, normally `DynamicResource` where appropriate.
- Do not add another WPF UI framework without explicit approval and an ADR.
- When scoping WPF-UI to a specific `UserControl`/`Window` (merging
  `ui:ThemesDictionary`/`ui:ControlsDictionary` in its own `.Resources`
  instead of `App.xaml`, to keep the migration to one view at a time — see
  ADR-004's consequences), remember that an implicit style found in that
  local scope **replaces** the nearest ancestor's implicit style for the same
  `TargetType`; it does not merge with it. A `DataGrid` inside that scope
  loses whatever `App.xaml`'s global `DataGrid` style set
  (`AutoGenerateColumns`, `IsReadOnly`, `GridLinesVisibility`, etc.) unless
  you set those properties again, explicitly, on the element itself.
  Confirmed the hard way during the CashFlow Monthly Expense pilot
  (2026-08-22): the grid silently auto-generated a raw column per DTO
  property alongside the intended ones.
- To pin an exact brand/status hex so it matches Web pixel-for-pixel, define
  literal `SolidColorBrush` resources under the specific keys the target
  control template binds (for `Wpf.Ui.Controls.Button`'s `Primary`
  appearance: `AccentButtonBackground` / `AccentButtonBackgroundPointerOver`
  / `AccentButtonBackgroundPressed`), in the same scoped `.Resources` block,
  after the `ui:ControlsDictionary` merge. Do not use
  `ApplicationAccentColorManager.Apply(...)` for this — see ADR-005.

## Layout

- Use `Grid` for structured page and form layouts.
- Use `StackPanel` for simple linear groups.
- Use shared spacing and style resources.
- Adapt to narrower windows rather than allowing fields to become unusably
  narrow.
- Preserve logical focus and tab order after layout changes.
- Form fields follow the same responsive grid `docs/ui/decisions/ADR-002-responsive-form-layout.md`
  defines for React — a 4-column `Grid` (equal-width `ColumnDefinition`s) on
  wide layouts, with each field as label-above-control (a small `StackPanel`
  of a label `TextBlock` then the input), not the older single-column,
  one-field-per-row, label-left layout. Assign fields to fixed
  `Grid.Row`/`Grid.Column` positions; where two fields are mutually
  exclusive by state (e.g. "Card" vs "Payment Source" depending on payment
  mode), place both in the same cell and toggle `Visibility` — WPF's `Grid`
  does not auto-reflow around a collapsed sibling the way CSS Grid does, so
  don't rely on that; place fields explicitly for the layout you intend.

## Forms

- Bind fields with appropriate `UpdateSourceTrigger`.
- Use the established WPF validation mechanism consistently.
- Bind actions to commands.
- Disable or prevent duplicate saves while the save command executes.
- Preserve values after failed saves.
- Use automation properties where control purpose is not obvious.

## Data, trees, and charts

- A page combining one grid with one chart (Investment Transactions/Credits/
  Price History) follows `docs/ui/forms-data-and-visualisations.md`'s
  "Grid-and-chart pages" rule: filters at the top always, then side-by-side
  (grid in a resizable left column, chart filling the right) or stacked
  (chart on top, full-width grid below) depending on how many columns the
  grid needs — not platform habit. Build the side-by-side case as a `Grid`
  with two `ColumnDefinition`s and a vertical `GridSplitter`
  (`ResizeDirection="Columns"`) between them, the same way the stacked case
  already uses a horizontal `GridSplitter` between rows
  (`ResizeDirection="Rows"`).
- Reuse approved `DataGrid`, `TreeView`, and chart patterns.
- Preserve keyboard navigation and selection behavior.
- Use virtualization appropriately.
- Keep totals distinct: use `FontWeight="Bold"` on the numeric values, to
  match React's `<strong>` (see `docs/ui/forms-data-and-visualisations.md`).
  When a total's text is otherwise data-bound (e.g. built from a
  `MultiBinding`), split it into separate `TextBlock.Text` bindings instead —
  one plain, one bold — rather than one bound string covering the whole
  line, since you cannot make part of a single bound string bold. Do not use
  `<Run Text="{Binding ...}">` for this: `Run.Text` defaults to `TwoWay` and
  crashes on a read-only bound property, unlike `TextBlock.Text`, which
  defaults to `OneWay`.
- Give the identifying/label column (leftmost, textual) `Width="*"` so the
  grid fills the available width the way an HTML table naturally does;
  keep numeric columns compact/fixed-width. A `DataGrid` with only
  fixed-width columns leaves a dead blank area in the header/body — this is
  what `BanksGridView.xaml` did before the CashFlow Monthly Expense pilot
  fixed it (2026-08-22).
- Keep sort/filter state visible.
- Provide accessible alternatives for important chart data.
- Column-header sorting is not a designed feature on either platform yet —
  don't treat a native `DataGrid`'s default `CanUserSortColumns="True"`
  click-to-sort as parity with React's plain `<th>` headers (which have no
  sort behavior at all). When sorting is actually speced, implement
  equivalent explicit sort behavior on both platforms in that feature's own
  slice, rather than leaving WPF with an accidental head start.

## Dialogs and contextual UI

- Use the approved dialog/window pattern.
- Manage focus when opening and closing.
- Use descriptive titles.
- Keep destructive actions distinct.
- Do not use a modal window when inline or contextual interaction is more
  efficient.