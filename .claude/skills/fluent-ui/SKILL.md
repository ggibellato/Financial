---
name: fluent-ui
description: Use when designing, implementing, reviewing, or refactoring user-facing workflows in Financial.Web, including forms, transaction entry, grids, totals, tree views, charts, headers, toolbars, dialogs, drawers, validation, warnings, errors, notifications, responsive/adaptive layouts, and accessibility.
---

# Fluent UI Skill

## Purpose

Implement a consistent, accessible, efficient financial-management experience
in `Financial.Web` (React + TypeScript), the only front end.

Do not reproduce an existing React implementation blindly. If it conflicts with
product requirements, accessibility, financial clarity, or repository standards,
correct it.

## Mandatory reading

Before significant UI work, read:

1. `docs/rules/ui.md`
2. `docs/ui/README.md`
3. `docs/ui/standards-hierarchy.md`
4. `docs/ui/ux-principles.md`
5. `docs/ui/design-tokens.md`
6. `docs/ui/forms-data-and-visualisations.md`
7. `docs/ui/accessibility.md`
8. Platform guidance: `docs/ui/react.md`
9. Relevant ADRs under `docs/ui/decisions/`
10. `docs/ui/review-checklist.md` before completion

Also inspect:

- Existing React implementation of the workflow
- Existing shared components and theme/resources
- Existing validation, loading, error, and command patterns
- Relevant API DTOs and `Financial.Web/src/api/types.ts` when visible data changes
- Existing tests and smoke coverage

## Required process

### 1. Assess the workflow

Before coding, describe:

- User task
- Relevant financial/domain context
- Current React behavior and whether it meets the target standard
- Information hierarchy
- Primary and secondary actions
- Field order and grouping
- Inline versus dialog versus drawer decision
- Required states
- Responsive/adaptive behavior
- Accessibility and focus requirements
- API contract implications
- Files and tests affected

Ask a focused question when a material product/domain rule cannot be inferred
safely.

### 2. Implement

1. Inspect the existing React experience and shared API/data model.
2. Define the intended UX according to `docs/ui/`.
3. Implement or correct the React experience.
4. Verify React behavior, state handling, responsiveness, accessibility, and tests.

### 3. Preserve shared semantics

Keep consistent across views:

- Terminology
- Field order
- Labels and required indicators
- Action labels and priority
- Validation wording and meaning
- Status severity
- Currency/date/quantity formatting
- Totals and calculation scope
- Loading, empty, success, and error outcomes
- Keyboard flow
- Selection, sorting, and filtering meaning

### 4. Apply UI rules

- Use Fluent 2 visual patterns and semantic tokens/resources.
- Follow a 4px spacing rhythm.
- Prefer 4/2/1 columns for wide/medium/narrow forms, adapting to content.
- Do not use a single excessively wide form row.
- Use one primary action per region.
- Keep labels visible.
- Never use colour alone for meaning.
- Design applicable initial, loading, empty, validation, server-error, saving,
  success, disabled, and unsaved-changes states.
- Do not implement only the happy path.

### 5. Verify before completion

Use `docs/ui/review-checklist.md` against every touched view or component in
full — not only the items tied to whatever rule or bug prompted the change
(`docs/rules/ui.md`'s "Scope of compliance"). Then have
`.claude/agents/ui-reviewer.md` review the change.

Run applicable project commands:

- Web: `npm run lint`, `npm test`, `npm run build`
- API/.NET changes: relevant `dotnet test` and `dotnet build --configuration Release`
- API contract work: update snapshot, `types.ts`, and contract tests in the same
  change where intended
- Critical workflow: relevant Playwright smoke test when practical

Build and tests passing is not the same as the UI being correct — actually
run the affected view in a browser (dev server) and look at it before calling
the work done. When checking a color, prefer sampling the actual rendered
pixel color over eyeballing a screenshot (a generated accent ramp can look
"close enough" and still be a different hex — see
`docs/ui/decisions/ADR-005-brand-and-status-colors.md`).

For any inline form, "looked at it" means opened it *and* closed it — a
static screenshot after navigating there once won't catch a broken toggle.

Report:

- UX decisions
- Accessibility behavior
- State behavior
- Tests executed and results