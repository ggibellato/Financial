---
name: fluent-ui
description: Use when designing, implementing, reviewing, or refactoring user-facing workflows in Financial.Web or Financial.App, including forms, transaction entry, grids, totals, tree views, charts, headers, toolbars, dialogs, drawers, validation, warnings, errors, notifications, responsive/adaptive layouts, accessibility, and React-to-WPF parity.
---

# Fluent UI Skill

## Purpose

Implement a consistent, accessible, efficient financial-management experience
across:

- `Financial.Web` — React + TypeScript
- `Financial.App` — WPF

`Financial.Web` is the UX source of truth.

For features that exist in both front ends, define and validate the intended
experience in React first, then implement an equivalent WPF desktop experience
using appropriate native conventions.

Do not reproduce a React implementation blindly. If React conflicts with product
requirements, accessibility, financial clarity, or repository standards, correct
React first and then map the corrected workflow to WPF.

## Mandatory reading

Before significant UI work, read:

1. `docs/rules/ui.md`
2. `docs/ui/README.md`
3. `docs/ui/standards-hierarchy.md`
4. `docs/ui/ux-principles.md`
5. `docs/ui/design-tokens.md`
6. `docs/ui/forms-data-and-visualisations.md`
7. `docs/ui/accessibility.md`
8. Platform guidance:
   - `docs/ui/react.md`
   - `docs/ui/wpf.md`
9. Relevant ADRs under `docs/ui/decisions/`
10. `docs/ui/review-checklist.md` before completion

Also inspect:

- Existing React and WPF implementation of the workflow
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
- Current WPF behavior and parity gaps against the React target
- Information hierarchy
- Primary and secondary actions
- Field order and grouping
- Inline versus dialog versus drawer decision
- Required states
- Responsive/adaptive behavior
- Accessibility and focus requirements
- Necessary WPF-native adaptations and why they preserve user outcomes
- API contract implications
- Files and tests affected

Ask a focused question when a material product/domain rule cannot be inferred
safely.

### 2. Implement React first

For cross-platform UI work:

1. Inspect the existing React experience and shared API/data model.
2. Define the intended UX according to `docs/ui/`.
3. Implement or correct the React experience first.
4. Verify React behavior, state handling, responsiveness, accessibility, and tests.
5. Implement the equivalent WPF experience using MVVM and established
   Fluent-themed WPF controls.
6. Compare labels, fields, actions, formatting, state handling, keyboard flow,
   and outcomes.
7. Document intentional platform-specific differences.

### 3. Preserve shared semantics

Preserve across React and WPF:

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

Equivalent user experience is required. Identical controls are not.

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

Use `docs/ui/review-checklist.md`.

Run applicable project commands:

- Web: `npm run lint`, `npm test`, `npm run build`
- WPF/.NET: relevant `dotnet test` and `dotnet build --configuration Release`
- API contract work: update snapshot, `types.ts`, and contract tests in the same
  change where intended
- Critical workflow: relevant Playwright smoke test when practical

Report:

- UX decisions
- React-to-WPF equivalence result
- Accessibility behavior
- State behavior
- Tests executed and results
- Intentional platform differences and their justification