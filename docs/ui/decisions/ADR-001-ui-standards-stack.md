# ADR-001: UI Standards Stack

## Status

Accepted

## Context

Financial has two front ends:

- `Financial.Web` — React + TypeScript
- `Financial.App` — WPF

The application contains high-density financial workflows involving forms, grids,
trees, charts, totals, validations, errors, and recurring data entry.

The project needs a common visual language, accessibility baseline, usability
review framework, and unambiguous cross-platform UX authority.

## Decision

Financial uses:

- Microsoft Fluent 2 as the primary visual and component design system.
- The Fluent UI React stack for `Financial.Web`, per `ADR-004`.
- The Fluent-themed WPF control/resource system for `Financial.App`, per
  `ADR-004`.
- WCAG 2.2 AA as the accessibility baseline where applicable.
- Nielsen Norman Group usability heuristics for workflow review.
- `docs/ui/` as the authority for Financial-specific UX decisions.

`Financial.Web` is the UX source of truth.

React defines the intended workflow, terminology, hierarchy, field ordering,
action priority, validation wording, status behavior, financial formatting,
totals, and user-visible states.

`Financial.App` implements an equivalent desktop experience. WPF may use
desktop-appropriate controls and interaction mechanics, but it must not change
the workflow meaning, sequence, or expected outcome without a documented
exception.

## Consequences

- Do not introduce competing design systems without approval and an ADR.
- React and WPF must preserve equivalent workflow semantics.
- Fluent guidance does not override accessibility or confirmed financial/domain
  requirements.
- Existing React UI must still be improved when it violates repository standards.