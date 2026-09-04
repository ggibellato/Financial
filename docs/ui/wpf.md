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
- A custom `UserControl`'s `DependencyProperty` that does arithmetic or
  constructs a value type (e.g. `MonthYearPicker.SelectedYear`/
  `SelectedMonth` building a `DateTime`) must tolerate its *bound* value
  before the ViewModel has ever set it deliberately — not just the DP's own
  declared default. A plain `int`/`decimal` ViewModel field defaults to `0`
  until some command (e.g. "New Expense") first assigns it; the moment a
  `TwoWay` binding activates (control `Loaded`, or the control simply
  existing inside a `Visibility="Collapsed"` sibling that's already in the
  visual tree), WPF pushes that `0` into the DP, overwriting its
  `PropertyMetadata` default — a control that assumes "the DP default is
  always valid" and does e.g. `new DateTime(SelectedYear, SelectedMonth, 1)`
  unguarded will crash the whole app on startup. Clamp/fall back (to
  `DateTime.Today`, or whatever's sensible) inside the control itself; don't
  rely on the consumer never binding to an unset value.
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

**Breadcrumb:** give the breadcrumb `TextBlock`/container
`AutomationProperties.Name="Breadcrumb"` so a screen reader can identify it.
WPF has no settable landmark-type XAML property (`AutomationLandmarkType`
requires a custom `AutomationPeer`) — the accessible name is the practical
equivalent to Web's `<nav aria-label="Breadcrumb">` here. Segments stay
plain, non-interactive text, matching Web (this app's nav tree has no route
for a category to link to). `MainWindow.xaml` is the reference.

## Forms

- Bind fields with appropriate `UpdateSourceTrigger`.
- Use the established WPF validation mechanism consistently.
- Bind actions to commands.
- Disable or prevent duplicate saves while the save command executes.
- Preserve values after failed saves.
- Use automation properties where control purpose is not obvious.
- Never set `DataContext` and another `Binding`-valued property (most often
  `Visibility`) on the *same* element. Once `DataContext` is set locally on
  an element, every other `Binding` on that same element resolves against
  the new local value, not the inherited one — so
  `Visibility="{Binding IsXFormOpen}"` next to
  `DataContext="{Binding XFormViewModel}"` silently looks for `IsXFormOpen`
  on `XFormViewModel` (which doesn't have it), fails to resolve, and
  `Visibility` just sits at its CLR default (`Visible`) forever — the form
  never collapses, on either open or close. This is exactly what happened
  embedding `TransactionFormView`/`CreditFormView`/`PriceFormView` (see
  "Dialogs and contextual UI" below): wrap the child in a plain `Grid` (or
  any container), put `Visibility` on that wrapper — bound at the *parent's*
  DataContext, where `IsXFormOpen` actually lives — and set `DataContext`
  only on the inner form element:
  ```xml
  <Grid Visibility="{Binding AssetDetails.IsXFormOpen, Converter={StaticResource BoolToVisibilityConverter}}">
      <local:XFormView DataContext="{Binding AssetDetails.XFormViewModel}"/>
  </Grid>
  ```

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
- If an OxyPlot chart thins its value-label density based on the `PlotView`'s
  measured width (`SizeChanged`), don't gate label rendering on that width
  being known yet (`if (plotWidth <= 0) return;`). The first build can run
  before the view's first `SizeChanged` fires, so that guard means no labels
  ever appear until an actual resize happens — silently, since nothing
  errors. Default to showing every label (no thinning) when the width isn't
  known yet, and re-thin once a real measurement arrives. This is what made
  `TransactionsChartBuilder`'s labels never show up (`CreditsChartBuilder`
  had the identical bug, just harder to notice in casual testing).
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
  efficient — see `docs/ui/forms-data-and-visualisations.md`'s "'New X'
  create actions are inline forms, not popup dialogs" rule for the create/
  edit case specifically.
- Converting an existing `Window`-based create/edit dialog to an inline form
  keeps the same `*DialogViewModel` (state, validation, `ConfirmCommand`/
  `CancelCommand`/`CloseRequested`) — it's still the right shape for an
  inline form's state, "dialog" in the name is just historical. Only the
  *hosting* changes: instead of `new XDialog(vm){Owner=...}.ShowDialog()`
  blocking synchronously, wrap the show/close cycle in a
  `TaskCompletionSource` — set an `IsXFormOpen` bool and an `XFormViewModel`
  property, subscribe to `CloseRequested`, and complete the `TaskCompletionSource`
  from that handler. This lets a `Func<Task<TData?>> showForm` slot into an
  existing `Actions`-class method (`TransactionActions.Add`, etc.) with only
  an `await` added — the request/validation/service-call logic underneath
  doesn't change at all. Delete the `Window`/`.xaml.cs` only for the modes
  you actually convert; a `Delete` confirmation can keep using the same
  `Window` in read-only mode, since confirmations are exempt from the
  inline-form rule.
- `ui:Flyout` content (and `Wpf.Ui.Controls.SplitButton.Flyout`, which hosts
  the same kind of content) is a `Popup`-rooted visual tree that does **not**
  inherit the outer `DataContext` — a plain `{Binding Foo}` inside it silently
  resolves against nothing. `HelpFlyoutButton.xaml` and
  `Controls/StatusSplitButton.xaml` both work around this by naming the
  hosting `UserControl` (`x:Name="root"`) and binding everything inside the
  Flyout back to it explicitly (`{Binding Foo, ElementName=root}`), the same
  way `BillTableView.xaml` already escapes `DataGridTemplateColumn` cell
  scoping. Use this pattern for any new Flyout content instead of assuming
  ambient `DataContext` reaches inside it.
- To show a small "is this the currently selected one" checked/disabled state
  in a repeated item template (a Flyout's status list, a sidebar's nav item),
  reuse the existing `EqualityToBoolConverter` in a `MultiBinding` bound to
  the item's `Tag`, then drive `IsEnabled`/icon visibility off that `Tag` with
  a `DataTrigger` — see `Sidebar.xaml`'s `NavChildButtonTemplate` (the
  original) and `StatusSplitButton.xaml` (the same pattern reused for a
  Flyout item). This needs no new converter for the common case.
- Every existing `*DialogViewModel` before `UkExpensePromptDialogViewModel`
  was Confirm/Cancel only, matching `DialogCloser`'s and `Window.DialogResult`'s
  native `bool?`. For a dialog with a third outcome (e.g. Confirm/Skip/Cancel),
  don't change that signature — add a `Decision` enum property the caller reads
  after `ShowDialog()` returns `true`, and have both non-cancelling actions
  raise `CloseRequested(true)` (only Cancel raises `false`). This reuses
  `DialogCloser`/`IDialogService`/`StubDialogService` unchanged for any future
  3+-outcome dialog instead of introducing a second dialog-closing mechanism.