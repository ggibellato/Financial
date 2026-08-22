# Financial.Web UI Instructions

- `Financial.Web` is the UX source of truth for cross-platform workflows.
- Follow `docs/rules/ui.md` and `docs/ui/react.md`.
- Use the Fluent UI React packages adopted in `docs/ui/decisions/ADR-004-fluent-component-library-adoption.md` (`@fluentui/react-components`, `@fluentui/react-icons`) and project components.
- Do not add another styling system or component library without approval.
- Prefer semantic HTML before ARIA.
- Use CSS Grid for structured page/form layout and Flexbox for smaller linear
  groups.
- Preserve logical DOM reading order during responsive reflow.
- If a workflow exists in WPF, ensure the React implementation clearly defines
  the expected task sequence, terminology, fields, actions, states, and
  formatting that WPF must match.
- For significant UI work, use the `fluent-ui` skill.