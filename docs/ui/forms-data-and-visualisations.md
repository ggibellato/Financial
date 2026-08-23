# Forms, Data, Trees, Charts, and Totals

## Forms

### Month/year selection

Where a page's primary scope is one calendar month (e.g. CashFlow Monthly's
top-of-page period filter), month and year are **one control**, not two
separate fields the user operates independently. Web's native
`<input type="month">` is the reference: a single edit surface that holds
month and year together, with a built-in calendar affordance for picking the
value visually rather than typing/spinning it. WPF must reach the same
result with a genuine calendar picker restricted to month/year — not two
`ComboBox`es, even visually unified ones (that was tried first and rejected:
it reads as two fields, not one, and has no calendar affordance at all).

`Financial.App/Components/MonthYearPicker.xaml` is the reference
implementation: a flat, bordered field showing the selected period as
`"MMMM yyyy"` (an outline-only `Border` around a `ui:Button` with
`Appearance="Transparent"` — not a raised/filled button, matching the plain
look of Web's native `<input type="month">`), which opens a fixed-size
popup: a year header with prev/next arrows, and a 4x3 grid of month cells.
Month cells match the browser's own native month-picker popup exactly — flat
text at rest, a solid rounded accent highlight only on the selected month —
via a custom flat `ControlTemplate`, not `ui:Button` or the native `Calendar`
control's `Year` view (both were tried first and rejected: a bare
`Calendar`'s `Year` view has no bounded natural size inside an unconstrained
`Popup`, so it rendered full screen-width before a month was ever picked, and
`ui:Button`'s default chrome/border made the grid read as a "board" of
bordered buttons rather than flat text like the reference). This keeps the
existing `SelectedYear`/`SelectedMonth` dependency-property API so
`MonthlyView.xaml`, `MensaisView.xaml`, and `InvestmentSnapshotsView.xaml`
need no changes to consume it.

### Default field order

Unless an approved financial workflow requires another order:

1. Date and time
2. Related entities and classifications
3. Description and free-text detail
4. Quantity, price, amount, fees, and other financial values
5. Optional metadata
6. Actions

For a transaction form, the normal sequence is:

1. Date
2. Type
3. Account, broker, bank, card, category, or investment
4. Merchant and description
5. Quantity, unit price, amount, and fees
6. Notes
7. Save and Cancel

### Layout

Use a content-aware responsive form grid:

- Wide desktop: normally 4 columns
- Medium layout: normally 2 columns
- Narrow layout: normally 1 column

Typical field spans:

- Date, type, quantity, unit price, amount, and fees: 1 column
- Account, broker, category, and investment lookups: 1–2 columns
- Merchant and description: 2 or more columns
- Notes: full available group width

Do not create one ultra-wide row merely because the window is wide.
Do not force every field to equal width.

### Field rules

Every editable field requires:

- Visible label
- Appropriate input control
- Accessible name
- Required indicator where relevant
- Contextual help where relevant
- Clear validation behavior
- Appropriate width
- Logical tab order

Do not use placeholders as labels.

### Form actions and saving

- Use one primary form action.
- Use specific action labels.
- Keep related Save and Cancel actions together.
- Keep destructive actions visually and spatially distinct.
- Prevent duplicate submission while saving.
- Show progress on the initiating action.
- Preserve values after failed save.
- Confirm discarding only when unsaved changes exist.
- Update dependent grids, totals, and charts after successful changes.

### Validation

- Validate client-side and server-side where appropriate.
- Keep field errors near the affected control.
- Use an error summary when several errors exist.
- Make messages specific and actionable.
- Example: “Enter a quantity greater than 0.”
- Do not use only “Invalid value” or “Required.”
- Focus the first actionable error after failed submission where appropriate.
- Do not use colour alone for invalid state.

## Data grids

### Grid create/new actions

A button that creates a new row for a grid (New Expense, New Income, New
Transfer, New Balance Correction, etc.) is styled and placed the same way
everywhere, on both platforms:

- **Position: left**, directly above the grid it creates rows for — not
  right-aligned. CashFlow Monthly's Bank tab is the reference; every other
  tab under it (Expense, Credit Card, Income) must match, not the other way
  around.
- **Style:** primary-appearance button with a leading "add" icon and a
  concise label (`New Expense`, `New Income`, `New Transfer`) — the same
  size, color, and icon convention everywhere. Don't hand-draw a "+" as a
  text character (Web) or leave one tab's button as a plain unstyled
  `Button` while another uses the real primary/icon treatment (WPF) — both
  are the same drift this rule exists to prevent.
- A tab with more than one create action (Bank: New Transfer / New Balance
  Correction) places both buttons together, left-aligned, in the same style
  — do not demote one to a lesser style just because there are two.

Grids must provide applicable:

- Clear column headers
- Sort state
- Filter state
- Selection state
- Keyboard navigation
- Loading state
- Empty state
- Error state
- Stable row height and column layout
- Pagination or virtualization for larger data sets

Let the identifying/label column (leftmost, textual — e.g. "Bank", "Description")
take the remaining width so the grid fills its available space the way a
plain HTML table does by default; keep numeric/status columns compact. Do not
leave fixed-width columns that add up to less than the container width — the
gap reads as an unfinished/broken grid, not intentional whitespace.

Column-header click-to-sort is not a designed feature yet on either platform.
A native WPF `DataGrid` may expose default sorting via
`CanUserSortColumns="True"` while React's plain `<th>` headers have none —
that is not feature parity, just an accident of the native control. When
sorting is actually specified, implement equivalent, explicit sort behavior
on both platforms as part of that feature's own slice.

### Alignment and financial values

- Text normally aligns left.
- Numeric and financial values normally align right.
- Dates align consistently.
- Do not center data by default.
- Use consistent currency, decimal precision, sign convention, date format, and
  locale behavior.
- Clearly distinguish percentage, currency, quantity, price, and rate.

### Totals

Totals must:

- Have a clear label.
- Be visually distinct from ordinary rows — in practice, bold the value
  (React: `<strong>`; WPF: `FontWeight="Bold"`) on both platforms, not just
  one. If a total's text is otherwise one bound/formatted string (e.g. a WPF
  `MultiBinding` producing "Bank Balance: 45.00 · Round-Up: 0.00"), split it
  into separate label/value elements instead so only the values are bold —
  you cannot make part of a single bound string bold. On WPF, use separate
  `TextBlock.Text` bindings for this, not `<Run Text="{Binding ...}">`:
  `Run.Text` defaults to `TwoWay` and crashes on a read-only bound property,
  unlike `TextBlock.Text`, which defaults to `OneWay`.
- State whether they are filtered subtotal, period total, or grand total.
- Use consistent calculation and formatting rules.
- Update after relevant changes.
- Avoid appearing editable unless they genuinely are editable.

## Tree views

Trees must support:

- Clear hierarchy
- Expand/collapse affordances
- Visible selection
- Keyboard navigation
- Loading for asynchronous children
- Empty branch behavior
- Accessible expanded/collapsed state

Do not rely on indentation alone when the relationship could be unclear.

## Charts and graphs

Every chart must provide:

- Clear title
- Units and labels
- Date range or period
- Legend where multiple series exist
- Meaningful tooltips
- Loading state
- Empty state
- Error state
- Accessible alternative data or summary
- Non-colour distinction between series where needed

Charts must answer a user question. Do not use them as decoration.

### Series color

Single-series bars and lines are **blue**, not grey/neutral — the same blue
already established by the Investment Credits and Price History charts on
both platforms (Web: `#4682b4`, a light-to-dark blue gradient
`rgb(173,216,230)`→`rgb(8,81,156)` for multi-type bars; WPF:
`OxyColors.SteelBlue`, the equivalent RGB gradient in
`*ChartBuilder.BuildBluePalette`). A chart is not exempt from this because it
started out grey for an unrelated reason (e.g. reusing a neutral/disabled
color) — match the existing chart blue, don't invent a new one.

### Value labels

When a chart shows a value directly on/at a bar or point, position the label
so it reads clearly regardless of the mark's own fill color — above/outside
the mark in a fixed, explicit dark color, not inside a colored fill where
contrast depends on which color in a gradient palette that particular bar
happens to be. WPF's `TextAnnotation`-above-the-bar convention (`OxyColors
.Black`, anchored above positive values / below negative ones) is the
reference; Web must produce the same readable result even though Recharts'
`LabelList` needs a different prop to do it (`position="top"` with an
explicit `fill`, not `position="inside"` with no fill set).

Every bar chart shows its value on top of the bar whenever there is room for
it — this is the standard, not an enhancement one platform happens to have.
A chart that currently shows no value labels at all (not just badly
positioned ones) is missing this, the same as one with unreadable labels.

### Month-axis labels

Every chart bucketed by month uses the same axis label format,
**`MM/yyyy`** (e.g. `09/2025`) — never a short month name like `Sep` or
`Sept`. This applies identically on both platforms and to every such chart
(Transactions, Credits, and any future one), not just the ones that already
happened to agree. On Web, build the label through one shared formatter
(`formatMonthKey` in `utils/formatters.ts`) rather than each chart hook
formatting its own month string — that divergence is exactly how Transactions
ended up different from Credits.

## Grid-and-chart pages

A page pairing one grid with one chart over the same data (e.g. Investment
Transactions/Credits/Price History) follows one layout rule, decided by how
much width the grid's columns actually need — not by platform convention or
habit:

- **Filters/period controls (e.g. "This Month", "Last 3 Months") always go at
  the top of the page**, above both the grid and the chart — never below
  either.
- **If the grid's columns fit comfortably in a fixed side panel** (roughly
  260–450px — few columns, or values that don't need much horizontal room,
  as in Credits and Price History), lay the page out **side by side**: grid
  in a resizable left panel, chart filling the remaining width on the right.
  Both stay visible without scrolling.
- **If the grid has enough columns/values that a side panel would cramp it**
  (as in Transactions — action icons, date, type, quantity, unit price, fees,
  and total), **stack instead**: chart full-width on top, grid full-width
  below. Do not force a wide grid into a narrow side panel just to keep a
  side-by-side layout consistent across pages — the grid's actual column
  count decides the layout, the page doesn't get to override it.
- Whichever layout applies, the New/action toolbar for the grid sits directly
  above that grid (inside its panel when side by side), not detached from it.

This is drawn directly from `Financial.Web`'s existing Transactions/Credits/
Price History tabs (React is the UX source of truth — see
`docs/ui/decisions/ADR-001-ui-standards-stack.md`): `TransactionsTab.tsx`
stacks (its grid has 8 columns including actions); `CreditsTab.tsx` and
`PriceHistoryTab.tsx` split side by side (5 columns each, via the shared
`SplitPanel` component). WPF must reach the same layout outcome using a
`GridSplitter`-based row split (stacked) or column split (side by side) —
identical controls are not required, the resulting reading order and use of
width are.

## Inline form, dialog, drawer, or page

Use an inline form when the task is short, repeated, and benefits from nearby
grid, chart, and total context.

Use a dialog for focused, self-contained confirmation or short blocking work.

Use a drawer for contextual details, filters, supporting information, or
secondary editing that should preserve the page context.

Use a dedicated page for complex, long, or multi-stage workflows.

### "New X" create actions are inline forms, not popup dialogs

A "New X" / "+ New X" button (New Expense, New Income, New Transfer, New
Transaction, New Credit, New Price, etc.) creating one row for the grid
immediately below it **always expands an inline form in place on the same
tab/page** — it never opens a separate modal `Window`/dialog. Web's pattern
is the reference: `ExpensesSection.tsx`/`IncomeSection.tsx`/
`BankOperationsSection.tsx` toggle a boolean (`isFormVisible` /
`IsXFormOpen`-style) that shows/hides a form component positioned between the
action button and the grid it feeds, so the user keeps the grid, chart, and
totals in view while filling it in. `Financial.App`'s CashFlow tabs
(`ExpenseSectionView.xaml`, `IncomeSectionView.xaml`,
`CreditCardExpensesView.xaml`, `BankSectionView.xaml`) already follow this:
a `<local:XFormView Visibility="{Binding IsXFormOpen, Converter=
{StaticResource BoolToVisibilityConverter}}"/>` embedded directly in the
tab's own `Grid`, toggled by the same command that shows the "New X" button.

This is the standard for **both** platforms — do not use a `Window`/
`ShowDialog()` popup for this class of action even where desktop convention
might otherwise reach for one. A WPF `Window`-per-action (as
`Views/Investment/TransactionDialog.xaml`, `CreditDialog.xaml`, and
`PriceDialog.xaml` did) breaks this: it blocks the rest of the window,
disconnects the form from the grid/chart it's populating, and gives the two
platforms genuinely different task flows for the same action — not just a
different control. Convert any such dialog into an inline form embedded in
the view it creates rows for, following the existing CashFlow
`IsXFormOpen`/`XFormView` pattern: a boolean VM property toggled by the
"New X" command, a `UserControl` form bound to it, `Visibility` driven by
`BoolToVisibilityConverter`, and the same form reused for Edit (populate its
fields from the selected row instead of defaults) so Add and Edit don't need
two separate implementations.

A genuine confirmation ("Delete this transaction?") or another short,
self-contained, blocking interaction unrelated to populating a specific grid
row may still use a real dialog — this rule is specifically about the
create/edit-a-row action, not dialogs in general.

### Transaction workspace default

When users benefit from seeing a graph, entering a transaction, and immediately
reviewing the transaction grid and totals, keep the transaction form inline
between the graph and grid unless the form’s complexity makes that impractical.