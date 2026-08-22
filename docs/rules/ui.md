# UI Implementation Rule

This file is mandatory for any change that affects a user-facing workflow in:

- `Financial.Web`
- `Financial.App`
- Shared presentation components
- User-facing validation, warnings, errors, confirmations, or notifications
- API contracts whose changes alter visible Web or WPF behavior
- User-facing financial calculations, totals, formatting, grids, charts, filters,
  tree views, navigation, or page layout

## React is the UX source of truth

`Financial.Web` is the UX source of truth.

For workflows available in both front ends:

1. Define and validate the intended user experience in React first.
2. Improve the React workflow first if it conflicts with product requirements,
   accessibility, financial clarity, or the UI standards.
3. Implement an equivalent WPF desktop experience.
4. Adapt controls and interaction mechanics only where WPF conventions,
   accessibility, keyboard efficiency, or window behavior require it.
5. Do not change workflow meaning, terminology, field order, action priority,
   validation meaning, data formatting, or expected outcomes without a documented
   decision.

React being the reference does not make existing React behavior automatically
correct. It must still comply with the rules in this repository.

## Required reading

Before producing a design, implementation plan, code, or review for UI work,
read:

1. `docs/ui/README.md`
2. `docs/ui/standards-hierarchy.md`
3. `docs/ui/ux-principles.md`
4. `docs/ui/design-tokens.md`
5. `docs/ui/forms-data-and-visualisations.md`
6. `docs/ui/accessibility.md`
7. Relevant platform guidance:
   - React: `docs/ui/react.md`
   - WPF: `docs/ui/wpf.md`
8. Relevant decision records in `docs/ui/decisions/`
9. `docs/ui/review-checklist.md` before declaring work complete

For significant UI work, also use the `fluent-ui` skill.

## Required process

Before implementation:

1. Inspect the existing React and WPF version of the affected workflow.
2. Inspect existing shared UI components, styling/theme infrastructure, WPF
   ResourceDictionaries, validation patterns, commands, API DTOs, and tests.
3. Identify the user task, financial/domain context, current context, primary
   action, and secondary actions.
4. Assess whether the React implementation meets the target standard.
5. Identify WPF parity gaps against the intended React experience.
6. Define all applicable states:
   - Initial
   - Loading
   - Empty
   - Validation error
   - Server error
   - Saving/progress
   - Success
   - Disabled
   - Unsaved changes
7. Define responsive/adaptive layout behavior.
8. Define keyboard, focus, accessible-name, zoom/text-scaling, theme, and
   high-contrast behavior.
9. Reuse existing components, tokens, resources, and patterns before creating
   new ones.
10. Identify API contract implications. If an API-visible DTO changes, follow
    the root `CLAUDE.md` OpenAPI snapshot and `types.ts` requirements.

## Required design output

Before coding a significant UI change, provide:

- User task
- Financial/domain context
- Current React behavior and target React behavior
- Current WPF behavior and parity gaps
- Information hierarchy
- Primary and secondary actions
- Field order and grouping
- Inline versus dialog versus drawer decision
- Required states
- Responsive/adaptive layout behavior
- Accessibility and focus behavior
- WPF-native adaptations and why they preserve the React-defined outcome
- Files to change
- Tests to add or execute
- Assumptions requiring confirmation

## Completion requirement

Do not call UI work complete until:

- Relevant items in `docs/ui/review-checklist.md` have been reviewed.
- React and/or WPF builds and relevant tests have run.
- React behavior has been checked against the UI standards.
- Equivalent WPF behavior has been verified, or an intentional difference is
  documented and justified.
- User-facing API contract changes are reflected in the OpenAPI snapshot,
  `Financial.Web/src/api/types.ts`, and tests where relevant.