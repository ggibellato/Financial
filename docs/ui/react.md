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