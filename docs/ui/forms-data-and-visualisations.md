# Forms, Data, Trees, Charts, and Totals

## Forms

### Month/year selection

Any place the user picks a month and year together — a page's primary scope
(e.g. CashFlow Monthly's top-of-page period filter) **or** a form field
(e.g. a credit-card expense's Invoice Month) — uses **one control**, not two
separate fields the user operates independently. Web's native
`<input type="month">` is the reference: a single edit surface that holds
month and year together, with a built-in calendar affordance for picking the
value visually rather than typing/spinning it. WPF must reach the same
result with a genuine calendar picker restricted to month/year — not two
`ComboBox`es, even visually unified ones (that was tried first and rejected:
it reads as two fields, not one, and has no calendar affordance at all), and
not a bare `TextBox` showing "MM/yyyy" as raw text either (no picker
affordance at all, and looks broken/unstyled next to every other field on
the same form).

`Financial.App/Components/MonthYearPicker.xaml` is the one reference
implementation for both cases: a flat, bordered field showing the selected
period as `"MMMM yyyy"` (an outline-only `Border` around a `ui:Button` with
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
existing `SelectedYear`/`SelectedMonth` dependency-property API, so it drops
into any consumer unchanged — the page-level filter
(`MonthlyView.xaml`, `MensaisView.xaml`, `InvestmentSnapshotsView.xaml`) and
a form field (`ExpenseFormView.xaml`'s Invoice Month, replacing the old
`MonthYearTextBox`) alike.

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

### Add/Edit variant layout continuity

When an entity's Add/Create and Edit forms are separate views with different
field sets by design, any field that appears in both variants — plus the
trailing validation-error and Save/Cancel action rows — must occupy the same
absolute grid row in both views. Reserve empty, fixed-height rows in the
shorter variant for fields it doesn't show, rather than compacting its
shared field/action rows upward, so the value someone is editing and the
buttons they'll press stay in the same place whether they just opened Add or
Edit for the same entity. This does not require both variants to have the
same field count or add fields neither design calls for — it only fixes the
vertical position of what they already share.

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

**Required indicator mechanism:** Web uses Fluent's `Field` component's
first-class `required` prop (`<Field label="X" required>`), which renders a
visible asterisk after the label and wires `aria-required` on the control —
do not hand-roll a separate asterisk. WPF appends a themed `Run` after the
label text: `<Run Text=" *" Foreground="{DynamicResource
SystemFillColorCriticalBrush}"/>`, paired with
`AutomationProperties.HelpText="Required"` on the control. Any form using
`FieldLabelStyle` already follows this — see `ExpenseFormView.xaml` for the
reference.

**Contextual help mechanism:** for a field whose meaning isn't obvious from
its label alone, Web uses Fluent's `InfoLabel` in place of a plain string
label — `<Field label={{children: (_, props) => <InfoLabel {...props}
info="...">Label</InfoLabel>}}>` — which renders a trailing info button
opening a `Popover` with the explanation, and handles the `aria-owns`/focus
wiring itself. Use `InfoLabel` (not a plain tooltip) when the explanation is
more than a few words or needs any interaction — a plain tooltip is for
short, non-interactive text only. WPF: the shared `controls:HelpFlyoutButton`
control (`HelpText` property) — an info-icon `ui:Button` opening a
`ui:Flyout` with the text; do not hand-roll a new `SymbolIcon`+`Flyout` pair.
`BalanceAdjustmentForm.tsx`'s/`BalanceAdjustmentFormView.xaml`'s "Target
Balance" field is the reference on both platforms.

**Inline computed-value sentence bolding:** a prose sentence reporting a
computed value (e.g. "Current calculated balance for Barclays: £2,344.37",
"Adjustment of £4.20 recorded") bolds only the numeric portion, the same
"bold the value, not the label" principle as the Totals rule below — see
that rule's note on splitting a single bound/formatted string into separate
label/value elements, since you cannot bold part of one string.
`BalanceAdjustmentForm.tsx`/`BalanceAdjustmentFormView.xaml` are the
reference on both platforms.

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

**Save/Cancel/Confirm icons:** none. Fluent's own convention reserves
leading icons for actions where recognition value is high (Add, Delete,
Edit) and leaves primary form-submit actions as plain text — every existing
form on both platforms already follows this. Do not add an icon to Save/
Cancel/Confirm to "match" a grid's Add/Edit/Delete icons; those are a
different action category.

### Validation

- Validate client-side and server-side where appropriate.
- Keep field errors near the affected control.
- Use an error summary when several errors exist.
- Make messages specific and actionable.
- Example: “Enter a quantity greater than 0.”
- Do not use only “Invalid value” or “Required.”
- Focus the first actionable error after failed submission where appropriate.
- Do not use colour alone for invalid state.

## Action buttons (applies everywhere — pages, dialogs, panels, grids alike)

Any button that performs a real action — creates something, refreshes data,
moves or deletes an entity, runs a check, submits a form — gets the **same**
treatment everywhere on both platforms. This is not a grid-specific rule;
it covers a grid's own "New X" create trigger just as much as an Admin list
page's page-level "Create X" header button, a section's Refresh button, a
detail panel's contextual Move/Delete action row, and a form's Save button.

- **Style: primary-appearance, always.** `appearance="primary"` on Web,
  `Appearance="Primary"` on WPF, using the pinned brand blue from
  `decisions/ADR-005-brand-and-status-colors.md` — the same blue, same
  size, everywhere. Don't hand-draw a "+" as a text character (Web) or leave
  one instance as a plain unstyled `Button` while another uses the real
  primary/icon treatment (WPF) — both are the same drift this rule exists
  to prevent. Two peer action buttons sitting side by side (New Income
  Split/New Withdrawal; Move.../Delete Portfolio; New Transfer/New Balance
  Correction) both stay primary, left-aligned, in the same style — never
  demote one to a lesser style just because there are two, and never treat
  a Move/Delete pair as if it were a Save/Cancel pair.
- **Position: left**, directly above or beside the content it acts on —
  never right-aligned via `justify-content: space-between`/`flex-end` or
  `margin-left: auto` (Web), and never `HorizontalAlignment="Right"` (WPF) —
  even when the button sits in a page header next to a title, where
  `space-between` is the single most common way this rule gets silently
  violated. CashFlow Monthly's Bank tab is the reference for a grid's own
  create trigger; every other tab under it (Expense, Credit Card, Income)
  must match, not the other way around.
- **Size: standard.** No ad hoc `font-size`/`padding` overrides, and no
  `size="small"` on a full action button with a visible text label —
  `size="small"` is reserved for compact icon-only row actions inside a
  grid (see "Grid row actions" below), not a page- or panel-level action.

**The one sanctioned exception**: a confirmation dialog's own Cancel/dismiss
button — the button that backs out of the Save/Delete/Confirm decision the
dialog itself presents — stays `appearance="secondary"` (grey, not blue).
The dialog's whole action row (Confirm + Cancel together) still moves left
as a group, matching this app's own inline-form action-row convention
rather than Fluent's default right-aligned dialog-footer convention — only
Cancel's *color* is exempt, never the row's *position*. Cancel being grey
is the only sanctioned "lesser style" anywhere in this rule; nothing else
earns it — a Refresh button, a Move button, a Delete-as-a-page-action
button are not "cancel-shaped" just because they aren't the single most
important action on the page.

**Fix the whole file, not just the named button.** An audit or bug report
that names two broken buttons on a page does not mean the other three on
the same page are already correct — when a file is open for this reason,
check every action button in it, not only the ones named. A page with three
matching blue, left-aligned buttons and two left on the old grey/
right-aligned style looks *more* broken afterward, not less: the
inconsistency becomes the story instead of being fixed away. This is the
same "fix the whole chain together" principle as
"Trigger-to-form naming consistency" below, applied to styling instead of
wording.

## Data grids

### Grid create/new actions

A button that creates a new row for a grid (New Expense, New Income, New
Transfer, New Balance Correction, etc.) follows the "Action buttons" rule
above like every other action button. The one grid-specific detail: position
it **directly above the grid it creates rows for**, left-aligned — not
floating in an unrelated toolbar elsewhere on the page.

### Trigger-to-form naming consistency

The "New X" trigger button, the form it opens, and that form's own confirm
action must all name the same thing, so cause and effect are traceable
without re-reading anything: **trigger button → form title → confirm
button**. `ExpensesSection.tsx`/`ExpenseForm.tsx` is the reference: the
trigger reads "New Expense", the form's own title reads "New Expense" (and
"Edit Expense" once populated from an existing row), and the confirm button
reads "Add Expense" (and "Save" once editing) — never a generic
"Submit"/"Confirm"/"OK" that drops the entity name.

Do not let the form re-title itself into different wording once open (e.g.
a "New Transfer" trigger opening a form titled "Move Money") — the trigger's
noun carries through unchanged into both the form title and the confirm
button, on both platforms, in every mode (create and edit).

**Fix the whole chain together, not one link at a time.** An audit or PRD
item that names only one link (e.g. "the trigger drops the entity name")
does not mean the other two links are already correct — verify all three
before scoping a fix, and fix all three in the same change. A trigger-only
fix can make the chain look *more* broken than before: P38-F03 initially
fixed only the Investment Transaction/Credit/Price triggers (Web + WPF) to
match this rule, leaving each form's own title and confirm button
untouched — WPF's `TransactionDialogViewModel.Title`/`ConfirmLabel` still
read "Add Transaction"/"Add", so the merged trigger ("New Transaction") no
longer matched its own form, a mismatch that didn't visibly exist before
because the trigger was just a bare, unremarkable "New". A reviewer caught
it before merge; the fix was to close all three links in one change rather
than ship the partial one. When a later feature (e.g. an F07-style
"normalize casing/verbs" item) is expected to touch the same three-link
chain a current feature is only partially fixing, either fix the whole
chain now or explicitly hold the trigger fix until the form/confirm fix
ships alongside it.

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

### Grid row actions

Icon-only row actions (Edit, Delete, and similar per-row commands) go in the
**leftmost column(s)** of the grid, before the data columns — not trailing at
the right edge. `ExpensesSection.tsx`/`IncomeSection.tsx` are the reference:
each row's Edit and Delete `Button`s (`appearance="subtle"`, `size="small"`,
an explicit `aria-label`, no visible text) sit in the first two
`TableCell`s, ahead of Date/Description/Category/etc., using `EditRegular`/
`DeleteRegular` from `@fluentui/react-icons`. WPF matches with the same
leading column position, `ui:Button Appearance="Transparent"` and
`ui:SymbolIcon Symbol=Edit16`/`Symbol=Delete16`. This keeps the action affordance in a fixed, predictable
place regardless of how many data columns a given grid has, and matches this
rule's own action-button convention of never right-aligning actions (see
"Action buttons" above).

Each action column sizes to its icon button's own width plus comfortable
padding — never auto/flex-adjusted to share the grid's leftover space the way
the identifying/label column does above. A narrow, fixed action column next
to data columns that stretch to fill the container is the intended contrast,
not a bug to "fix" by letting it grow: a wide, loosely-clickable action column
reads as broken alignment, not intentional whitespace. The action column(s)
sit ahead of, and are excluded from, the "leftmost column takes the remaining
width" rule above — that rule applies to the leftmost *data* column, not an
icon action column preceding it.

Column-header click-to-sort is not a designed feature yet on either platform.
A native WPF `DataGrid` may expose default sorting via
`CanUserSortColumns="True"` while React's plain `<th>` headers have none —
that is not feature parity, just an accident of the native control. When
sorting is actually specified, implement equivalent, explicit sort behavior
on both platforms as part of that feature's own slice.

### Chart filter/mode toggle ("chip") pattern

A chart's period filter (This month/Last 3/6/12 months/YTD/All time),
display-mode toggle (Bar/Line), or grouping toggle (Stacked/Grouped) is a
single-select control among mutually-exclusive options — that's a tab
semantic, not an independent multi-toggle one. Web uses the shared
`FilterTabList` component (`components/FilterTabList.tsx`, wrapping Fluent's
`TabList`/`Tab`, `appearance="subtle" size="small"`), which also gets ARIA
tablist semantics and keyboard navigation for free — do not hand-roll a
`<button>`-per-option toggle for this again.

WPF keeps its existing `Button`+`Command`-per-option pattern (bound to each
ViewModel's `Select*Command`, with an `IsSelected` property per option
driving the trigger below) rather than switching to `RadioButton`+
`GroupName` — the ViewModel-driven selection model already works and does
not need re-plumbing to fix the actual gap, which was hardcoded, non-theme-
aware colors. Use the shared `FilterToggleTextStyle`/`FilterToggleLabelStyle`
resources (declared in `App.xaml`) on the option `TextBlock`s and the
"View:"/"Group:" label `TextBlock`s respectively, instead of a new inline
`Style` per view — `TransactionsView.xaml`, `CreditsFilterBar.xaml`, and
`PriceHistoryView.xaml` are the reference.

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

**Exception — Admin lookup-entity CRUD:** Bank, Broker, Category, CreditCard,
IncomeSource, InvestmentAccount, Portfolio, RecurringBill, ReserveBucket, and
Asset create/edit forms are exempt from this rule and may use a modal
`Dialog`/`Window` instead of an inline panel — see
`decisions/ADR-006-admin-crud-modal-dialogs.md` for the rationale (no
associated chart/running total, rarely edited, genuinely short forms). Do not
extend this exception to any other entity without a documented reason.

### Transaction workspace default

When users benefit from seeing a graph, entering a transaction, and immediately
reviewing the transaction grid and totals, keep the transaction form inline
between the graph and grid unless the form’s complexity makes that impractical.