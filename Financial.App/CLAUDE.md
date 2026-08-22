# Financial.App UI Instructions

- `Financial.Web` is the UX source of truth for cross-platform workflows.
- Follow `docs/rules/ui.md` and `docs/ui/wpf.md`.
- Inspect the equivalent React workflow before changing WPF UI.
- Follow MVVM strictly.
- Keep domain logic, service calls, and persistence out of code-behind.
- Reuse the Fluent-themed controls (WPF-UI, per `docs/ui/decisions/ADR-004-fluent-component-library-adoption.md`), shared styles, and ResourceDictionaries.
- Use theme-aware resources and AutomationProperties where needed.
- Preserve React-defined task flow, terminology, field order, action priority,
  validation meaning, financial formatting, status behavior, and outcomes.
- WPF may adapt controls and mechanics only when desktop convention,
  accessibility, or better usability requires it; document meaningful
  differences.
- Preserve keyboard navigation, focus visibility, high-DPI behavior, and
  high-contrast support.
- For significant UI work, use the `fluent-ui` skill.