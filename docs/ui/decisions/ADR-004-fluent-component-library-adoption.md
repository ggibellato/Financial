# ADR-004: Concrete Fluent Component Libraries

## Status

Accepted

## Context

Neither front end has a Fluent component library installed today.

- `Financial.Web` has no `@fluentui/*` package, or any other component
  library, in `package.json`. Every form, table, tree, and dialog is hand-built
  HTML/CSS/React (see `docs/ui/current-state-audit.md`).
- `Financial.App` has no Fluent WPF package. Every control is native
  `System.Windows.Controls`, styled globally in `App.xaml`. The only existing
  "Fluent-adjacent" element is incidental use of the Segoe MDL2 Assets icon
  glyph font in a handful of views.

`ADR-001` established Fluent 2 as the standards stack, but referred to "the
installed Fluent UI React stack" and "the established Fluent-themed WPF
control/resource system" as though a concrete library already existed on each
platform. It does not. `docs/ui/wpf.md`, `docs/rules/ui.md`, and
`docs/ui/standards-hierarchy.md` all require explicit approval and an ADR
before introducing a WPF UI framework, since none is present to reuse. This
ADR is that approval and names the concrete library for each platform.

## Decision

- `Financial.Web` adopts **Fluent UI React v9**: npm packages
  `@fluentui/react-components` (components, theming, `FluentProvider`) and
  `@fluentui/react-icons` (iconography). This is the component library
  referenced everywhere `docs/ui/react.md` and the `fluent-ui` skill say "the
  Fluent UI React version" or "the installed Fluent UI React stack."
- `Financial.App` adopts **WPF-UI** (NuGet package `WPF-UI`,
  `lepoco/WPF-UI` on GitHub) as the Fluent-themed WPF control/resource system
  referenced everywhere `docs/ui/wpf.md` and `Financial.App/CLAUDE.md` say
  "the established Fluent-themed WPF controls."
- Selection rationale for WPF-UI over the alternatives (ModernWpfUI,
  MahApps.Metro, HandyControl, FluentWPF): WPF-UI is the most actively
  maintained library that implements the actual Fluent **2** design language
  (Mica/Acrylic materials, `NavigationView`, Fluent 2 control anatomy).
  ModernWpfUI and FluentWPF track the older Fluent 1 / UWP-era visual
  language, not Fluent 2.
- No other design system, and no second WPF UI framework, may be introduced on
  either platform without a new ADR, per `docs/ui/standards-hierarchy.md`
  ("Prohibited design-system mixing").

## Consequences

- These are new dependencies. The implementation phase adds
  `@fluentui/react-components` / `@fluentui/react-icons` to
  `Financial.Web/package.json` and the `WPF-UI` package reference to
  `Financial.App/Financial.App.csproj` before any page is migrated.
- Existing hand-built React components (`Financial.Web/src/components/*.tsx` +
  their `.css` files) and native WPF views/`App.xaml` styles are migrated
  incrementally, page by page, against this decision — not rewritten in one
  pass. This matches the repository's PR-size and vertical-slice conventions
  in the root `CLAUDE.md`.
- Every place in `docs/ui/react.md`, `docs/ui/wpf.md`,
  `Financial.Web/CLAUDE.md`, and `Financial.App/CLAUDE.md` that refers to the
  Fluent library as "already installed" or "already present" now resolves to
  these two specific packages.
- `ADR-005` builds on this ADR to fix the brand/status color values used
  when configuring each library's theme.
