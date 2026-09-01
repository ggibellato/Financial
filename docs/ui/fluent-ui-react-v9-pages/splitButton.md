# Status Tag with Inline Menu (MenuButton-as-tag)

A compact control for changing a short-lived status value directly inside a grid
cell, without opening a form or drawer. Visually it looks like a colored status
tag with a trailing chevron; interacting with it (anywhere on the control, not
just the chevron) opens a menu listing every possible status.

Reference implementation: `Financial.Web/src/components/StatusMenuButton.tsx`
(first adopted by the Mensais grid's status column).

## When to use

- The value is a short, closed list of states (2-5 options) that the user
  changes often, in place, from a grid row.
- Opening a full edit form or drawer just to flip one field would be
  disproportionate to the size of the change.
- A visually distinct, at-a-glance color per state is useful for scanning a
  grid (e.g. paid/unpaid, open/closed).

Don't use this pattern for a field that needs its own validation, additional
context, or more than ~5 possible values — use a full edit form/drawer instead.

## Composition

Built from three existing Fluent v9 primitives, not a new component from the
library:

- **`MenuButton`** (inside `Menu`/`MenuTrigger`) is the trigger — its entire
  clickable surface (label and chevron) opens the menu. A true `SplitButton`
  is not used here: `SplitButton` gives its primary segment an independent
  default action distinct from the menu, which doesn't apply to a pure
  status-select control where there is no separate "do the default thing"
  action.
- **`Badge`** (`appearance="filled"`) renders inside the `MenuButton` as the
  colored tag itself. Color follows the project's status semantics
  (`docs/ui/design-tokens.md`'s `color.status.*` tokens, expressed through
  Badge's own `color` prop): a neutral/no-action state uses `subtle`, an
  informational/in-progress state uses `informative`, and a completed/success
  state uses `success`. Reserve `warning`/`danger` for states that genuinely
  need the user's attention — an expected, healthy intermediate state (like
  "Scheduled") is not a warning.
- **`Menu`/`MenuPopover`/`MenuList`/`MenuItem`** render the dropdown. Every
  possible value is always listed. The item matching the current value is
  `disabled` and shows a checkmark icon (`hasCheckmarks` on `MenuList`) —
  it is visible but not clickable, since selecting the current value again is
  a no-op. The other values are plain clickable `MenuItem`s that call back
  with the newly chosen value.

## Behavior

- Selecting a different value should call the backing update immediately —
  no separate "confirm" step for a same-page, easily-reversible change.
- While the update is in flight, disable the `MenuButton` (its built-in
  `disabled` state is sufficient) to prevent a duplicate concurrent request
  from the same control.
- On failure, revert to the previous value and surface the error using the
  page's existing error-message convention — don't invent a new error
  presentation just for this control.
- This pattern changes only the one field it represents. It is not a
  replacement for a full edit form that also validates or updates other
  fields on the same record — both can coexist as separate paths to the same
  underlying update.

## Accessibility

- Give the trigger an accessible name that includes both the field and its
  current value (e.g. `aria-label="Status: Paid. Change status"`), since the
  visual chevron alone doesn't convey that this is a status-change control to
  a screen reader.
- Keyboard operable via `Menu`'s built-in behavior: Tab to focus the trigger,
  Enter/Space to open, arrow keys to navigate items, Enter to select, Escape
  to dismiss without changing anything.
- Never rely on the Badge color alone to convey the status — the text label
  inside the Badge is mandatory, not decorative.

## WPF equivalent

See `.claude/skills/fluent-ui/references/cross-platform-mapping.md` — the
mapped WPF-UI control is `SplitButton`, since WPF-UI does not ship a bare
`MenuButton`-equivalent; the same current-value checked/disabled treatment and
2-click change must hold there too.
