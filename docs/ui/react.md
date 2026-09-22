# React UI Rules

## React UX authority

`Financial.Web` is the UX source of truth for cross-platform workflows.

React defines the intended:

- User task sequence
- Information hierarchy
- Form field order and grouping
- Terminology
- Action labels and priority
- Validation behavior and wording
- Loading, empty, saving, success, and error outcomes
- Financial formatting
- Totals behavior
- Responsive prioritization

Before changing a React workflow, verify it against product/domain rules,
accessibility requirements, Fluent 2 principles, and the UX principles in this
directory. Current React behavior is not automatically correct merely because it
is the reference.

## Component system

- Use the Fluent UI React version adopted in `ADR-004`.
- Reuse project components and wrappers before using raw Fluent components.
- Do not add a competing component or styling system without approval.
- Use semantic HTML and native browser semantics where practical.
- **Tab strips** (page-level content-switching tabs, e.g. an asset's Summary/
  Transactions/Credits/Price History, or Annual Summary's Category Totals/
  Investments/Historic Summary Average): use Fluent's `TabList`/`Tab`, not a
  hand-rolled `<button>` group — it ships the ARIA tablist/keyboard-nav
  pattern (arrow-key navigation, `role="tablist"/tab"`, `aria-selected`) for
  free. `MonthlyPage.tsx` is the reference. This is distinct from the chart
  filter/mode "chip" pattern documented in `forms-data-and-visualisations.md`
  (same underlying component, different content role).

## Grid row height

Every table in the app — every sortable/interactive grid, and every static
table (a post-submit result summary, a chart's accessible-data-table legend)
alike — uses Fluent's `<Table>`/`<TableHeader>`/`<TableBody>`/`<TableRow>`/
`<TableCell>` components, consistently across the whole app since 2026-09
(the handful of grids that still used a plain `<table>` — `TotalsGrid`,
`CardsGrid`, `ControleMaePage`, `ReservaPage`, `MensaisPage`,
`CurrentValuesPage`, `DividendCheckPage`, `RecurringBillsPage`,
`AnnualSummaryPage`, `PortfolioSummaryTab`, `UpcomingIncomePanel`,
`IncomeSplitForm`, `AllocationPieChart` — were migrated to Fluent's `Table`
for consistency). No exceptions: a raw `<table>` is never correct here,
even for a static, one-time or non-interactive table — always reach for
Fluent's `Table` components instead. Every `<Table>` carries the shared
`.data-table` class (`Financial.Web/src/styles/data-table.css`) on its
root. That class is what gives every table a uniform ~32px row height
(13px font, 8px/10px cell padding). **Never omit `.data-table`** from a
`<Table>`, and never pass a `size` prop to override it: Fluent's
`TableCell` hardcodes `height: 44px` for its default `"medium"` size (34px
for `"small"`, 24px for `"extra-small"`) — none of which match the app's
row height — and `.data-table td`'s `height: auto` rule is what
neutralizes that and restores natural, content-driven sizing (see that
rule's own comment for why it reliably wins regardless of style-injection
order).

## Long/variable-length text in grid cells

Never let a table/grid cell wrap (`white-space: normal`, or CSS missing
`white-space: nowrap` while relying on `text-overflow: ellipsis`, which is a
no-op without it) — an occasional long value then silently grows just that
row to double height while every other row stays single-line, an
inconsistent, jarring result rather than a deliberate design (fixed across
`ExpensesSection`, `ControleMaePage`, `ReservaPage`, `MensaisPage`, and
`TaxRulesPage` in 2026-09). Instead, use `TruncatedText`
(`Financial.Web/src/components/TruncatedText.tsx`) for any free-text
description/note/label cell: it truncates to one line with an ellipsis via
the shared `.truncated-text` class (`Financial.Web/src/styles/data-table.css`)
and reveals the full value through a Fluent `Tooltip` on hover or keyboard
focus (`relationship="inaccessible"` — the trigger's own text content already
carries the full string for screen readers, so no `aria-describedby`
duplication is needed; Fluent otherwise force-renders that content into the
DOM permanently for `"label"`/`"description"`, which breaks `getByText` in
tests). Grid rows must stay a uniform single-line height across the whole
grid; do not opt one column into wrapping to "fit more."

## Layout

- Use CSS Grid for page and form structure.
- Use Flexbox for simple linear groups.
- Use the established project token/styling system.
- Preserve logical DOM reading order when layouts reflow.
- Keep responsive behavior near the component or within the established styling
  layer.
- A component's own hardcoded className must not bake in a sizing assumption
  (a stretch/`flex`, a `max-height`, an equal-height rule) that only holds for
  one of its usage contexts. If a component is reused standalone (its natural
  content height) and inside a side-by-side row (stretched to match
  siblings), scope the stretching rule to the ancestor selector for that row
  (e.g. `.grids-row .section--grid { max-height: ... }`), not the component's
  bare class — otherwise the standalone usage inherits a fixed height meant
  for the row and shows a large, unintentional gap below its content. This is
  exactly what happened to `BanksGrid` when reused on the CashFlow Monthly
  page's Expense tab outside the Summary tab's grids-row (fixed 2026-08-22).

### Mobile scrolling

`Financial.Web` pages are designed against the WPF desktop experience first,
so on a small screen — including landscape phone widths — not everything
fits without scrolling. That is expected, not a bug to design away:

- Do not constrain page height to the viewport on mobile (no `height: 100vh`
  / `100%` chain forcing the page itself to fit one screen). Let the page
  grow to its natural content height and scroll.
- Do not use `overflow: hidden` on a mobile breakpoint in a way that clips
  content instead of scrolling it — reserve `overflow: hidden` for cases with
  no informational loss (e.g. a clipped decorative background).
- Media queries should reflow/stack content and adjust spacing/type for small
  screens, not attempt to cram the desktop layout into one unscrolled
  viewport.
- Wide tables/grids that can't reasonably reflow may scroll horizontally in
  their own `overflow-x: auto` container; the page itself still scrolls
  vertically around them.
- No content or functionality available on desktop is dropped on mobile —
  users reach it by scrolling instead.

## Forms

- Migrating one form to the Fluent `Field`/`Input`/`Select`/`Button`
  components (per `ADR-004`) does not migrate its sibling forms, even ones
  that render on the same page and look done at a glance. `ExpenseForm.tsx`
  moved to Fluent components first; `IncomeForm.tsx`, `TransferForm.tsx`,
  and `BalanceAdjustmentForm.tsx` kept a legacy hand-rolled
  `monthly-page__form-*`/`monthly-page__submit-btn` CSS-class button (small
  font, a different blue, a different border-radius, no Fluent focus/hover
  treatment) for a full round after the pilot, because they still *looked*
  like part of a finished page. Grep for the component library actually
  imported (`@fluentui/react-components`) rather than assuming a page-level
  pilot covered every form rendered on that page.
- Use visible labels.
- Use semantic `form` elements where submission applies.
- Use suitable HTML input type, input mode, autocomplete, and descriptive
  semantics.
- Connect help and validation messages to inputs.
- Do not use `aria-label` as a replacement for a visible label.

## Accessibility

- Use headings and landmarks appropriately.
- **Breadcrumb:** wrap in `<nav aria-label="Breadcrumb">` containing an
  `<ol>`, one `<li>` per segment, with `aria-current="page"` on the last
  (current-page) segment. A category segment with no route of its own
  (this app's nav tree has none — categories only group leaf pages) stays
  plain text, not a link; do not invent a fake link target just to make a
  segment "clickable." `Breadcrumb.tsx` is the reference.
- Prefer native semantics over custom ARIA roles.
- Manage focus after dialogs, drawers, asynchronous updates, and validation
  failure.
- Keep focus outlines or supply an equivalent compliant focus treatment.
- Use accessible live status behavior where necessary.

## Required state model

Async UI must define:

- Initial
- Loading
- Loaded
- Empty
- Error
- Retry where appropriate

Editable UI must define:

- Clean
- Dirty
- Valid
- Invalid
- Saving
- Save succeeded
- Save failed

## Verification

For meaningful UI changes, update applicable:

- Vitest component or unit tests
- Contract-related tests where API-visible data changes
- Playwright smoke coverage where a critical workflow changes
- Accessibility-oriented assertions where the test stack supports them
- `npm run lint`
- `npm test`
- `npm run build`