# UI Implementation Rule

This file is mandatory for any change that affects a user-facing workflow in:

- `Financial.Web`
- Shared presentation components
- User-facing validation, warnings, errors, confirmations, or notifications
- API contracts whose changes alter visible Web behavior
- User-facing financial calculations, totals, formatting, grids, charts, filters,
  tree views, navigation, or page layout

## React is the only front end

`Financial.Web` (React) is the only front end. Define, validate, and improve
user workflows there, and improve an existing workflow when it conflicts with
product requirements, accessibility, financial clarity, or the UI standards.

Existing React behavior is not automatically correct. It must still comply with
the rules in this repository.

## Required reading

Before producing a design, implementation plan, code, or review for UI work,
read:

1. `docs/ui/README.md`
2. `docs/ui/standards-hierarchy.md`
3. `docs/ui/ux-principles.md`
4. `docs/ui/design-tokens.md`
5. `docs/ui/forms-data-and-visualisations.md`
6. `docs/ui/accessibility.md`
7. React guidance: `docs/ui/react.md`
8. Relevant decision records in `docs/ui/decisions/`
9. `docs/ui/review-checklist.md` before declaring work complete

For significant UI work, also use the `fluent-ui` skill.

## Scope of compliance

A request to make a UI element "comply with the standards," "follow the UI
rules," or similar — or a request scoped to one specific rule (e.g. a newly
documented convention) — requires checking that element against **every**
applicable rule in `docs/ui/*.md` and this file, not only the rule named in
the request. A narrowly-worded trigger (e.g. "fix the row-action position")
does not narrow the compliance scope of the element being touched; it only
identifies which element to look at. Only skip rules the user has explicitly
excluded (e.g. "just reposition the buttons, don't touch anything else about
this grid").

This applies retroactively to elements a change touches: if fixing one rule
surfaces the element already violates another documented rule, fix or flag
that too rather than leaving it for a future request to rediscover.

`.claude/agents/ui-reviewer.md` enforces this — see "Completion requirement"
below.

## Required process

Before implementation:

1. Inspect the existing React version of the affected workflow.
2. Inspect existing shared UI components, styling/theme infrastructure,
   validation patterns, API DTOs, and tests.
3. Identify the user task, financial/domain context, current context, primary
   action, and secondary actions.
4. Assess whether the React implementation meets the target standard.
5. Define all applicable states:
   - Initial
   - Loading
   - Empty
   - Validation error
   - Server error
   - Saving/progress
   - Success
   - Disabled
   - Unsaved changes
6. Define responsive/adaptive layout behavior.
7. Define keyboard, focus, accessible-name, zoom/text-scaling, theme, and
   high-contrast behavior.
8. Reuse existing components, tokens, resources, and patterns before creating
   new ones.
9. Identify API contract implications. If an API-visible DTO changes, follow
   the root `CLAUDE.md` OpenAPI snapshot and `types.ts` requirements.

## Required design output

Before coding a significant UI change, provide:

- User task
- Financial/domain context
- Current React behavior and target React behavior
- Information hierarchy
- Primary and secondary actions
- Field order and grouping
- Inline versus dialog versus drawer decision
- Required states
- Responsive/adaptive layout behavior
- Accessibility and focus behavior
- Files to change
- Tests to add or execute
- Assumptions requiring confirmation

## Completion requirement

Do not call UI work complete until:

- Every item in `docs/ui/review-checklist.md` has been checked against each
  touched view or component — not only the items related to the rule that
  motivated the change (see "Scope of compliance" above).
- `.claude/agents/ui-reviewer.md` has reviewed the change.
- React builds and relevant tests have run.
- The affected view has actually been run and looked at in a browser (dev
  server) — a clean build and green tests do not guarantee the UI renders correctly; see
  `docs/ui/current-state-audit.md`'s pilot notes for a concrete case where
  they didn't.
- React behavior has been checked against the UI standards.
- User-facing API contract changes are reflected in the OpenAPI snapshot,
  `Financial.Web/src/api/types.ts`, and tests where relevant.