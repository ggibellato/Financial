# ADR-005: Brand and Status Accent Colors

## Status

Accepted

## Context

Neither front end has one canonical brand color today.

`Financial.Web/src/index.css` declares an `--accent: #aa3bff` (light) /
`#c084fc` (dark) token, but it is barely used in practice. The color actually
painted on nearly every primary button, active tab, tree/list selection
highlight, and link across both `Financial.Web` and `Financial.App` is an
un-tokenized blue, **`#007ACC`** (hover `#005FA3`), which recurs identically
across dozens of component stylesheets and several WPF views/converters (see
`docs/ui/current-state-audit.md`). It is already the documented, deliberate
selection-highlight color in multiple merged features (e.g. `docs/prd/P01`,
`docs/prd/P03`, `docs/prd/P23`). This blue is close to Fluent 2's own default
"communication blue" brand ramp (~`#0078D4`).

Status colors (success/warning/danger/info) have two to three competing hex
values per status on Web (see audit), and `Financial.App` uses named brushes
(`Brushes.Green` / `Brushes.Red`) rather than any shared token — there is no
single canonical value for any status color on either platform today.

## Decision

- Adopt **Fluent 2's default brand ramp** as the canonical
  `color.action.primary` / brand token on both platforms, rather than the
  declared-but-unused purple or a bespoke ramp built from scratch. This keeps
  the accent color users already associate with the app (`#007ACC` is close
  to Fluent's default `#0078D4`) while gaining a real, Fluent-authored,
  accessibility-tested ramp instead of an ad hoc hex value repeated
  file-by-file.
  - `Financial.Web`'s `FluentProvider` (from ADR-004) uses
    `@fluentui/react-components`'s default `webLightTheme` / `webDarkTheme`
    brand ramp as-is (light: `colorBrandBackground #0F6CBD`, hover
    `colorBrandBackgroundHover #115EA3`, pressed
    `colorBrandBackgroundPressed #0C3B5E`). Verified during the CashFlow
    Monthly Expense pilot (2026-08-22) to read close enough to the legacy
    `#007ACC` that no custom ramp is needed.
  - `Financial.App`'s WPF-UI theme must render the **exact same** hex values,
    pinned as literal `SolidColorBrush` resources under the specific keys
    `Wpf.Ui.Controls.Button`'s `Primary` appearance actually binds —
    `AccentButtonBackground` (`#0F6CBD`), `AccentButtonBackgroundPointerOver`
    (`#115EA3`), `AccentButtonBackgroundPressed` (`#0C3B5E`) — defined in the
    same scoped `UserControl.Resources` that merges
    `ui:ThemesDictionary`/`ui:ControlsDictionary` (see `docs/ui/wpf.md`).
    **Do not** use `Wpf.Ui.Appearance.ApplicationAccentColorManager.Apply(...)`
    to do this: it generates its own shade ramp from the seed color rather
    than using it directly (confirmed both by testing — feeding it
    `#0F6CBD` produced `#559CE4` on the button, a visibly different, lighter
    blue — and by [lepoco/wpfui#1481](https://github.com/lepoco/wpfui/issues/1481)),
    so it cannot be trusted to reproduce a specific hex.
- Status colors (`color.status.success` / `warning` / `danger` / `info`)
  adopt **Fluent 2's default status palette** on both platforms rather than
  any of the currently competing hex values, since Fluent's status ramp is
  already contrast-tested for WCAG 2.2 AA in both light and dark themes.
- The `--accent: #aa3bff` / `#c084fc` tokens in `Financial.Web/src/index.css`
  are retired in favor of the Fluent brand token during token implementation.

## Consequences

- This is the lowest-friction accent choice available: it preserves the
  visual identity already in front of the user on both platforms while
  replacing an untokenized, 15+-file-repeated hex literal with a real,
  themeable Fluent token.
- Every hardcoded `#007acc` / `#005fa3` (Web) and `#007ACC` (WPF) occurrence
  cataloged in `docs/ui/current-state-audit.md` is replaced by the shared
  brand token as each page is migrated — not by a single global find/replace
  done outside a page's own refactor slice.
- `Financial.App`'s named-brush status converters
  (`PositionTypeToColorConverter`, `TransactionTypeToColorConverter`,
  `SignedValueToBrushConverter`) are updated to reference the new shared
  status tokens instead of `Brushes.Green` / `Brushes.Red` so status colors
  match Web's semantic values, not just their sign (positive/negative).
- Dark-mode status/brand colors are defined from day one instead of
  inheriting Web's current partial dark-mode coverage (see audit).
- Status colors (success/warning/danger/info) are not yet pinned to literal
  hex values on WPF — do the same exact-value verification (screenshot both
  platforms, sample the rendered pixel) before considering a status color
  migrated, not just visual comparison by eye.
