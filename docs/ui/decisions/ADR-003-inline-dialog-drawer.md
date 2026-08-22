# ADR-003: Inline Forms, Dialogs, and Drawers

## Status

Accepted

## Context

Financial includes short, repeated entry tasks as well as contextual editing,
filters, detail inspection, and destructive actions.

The chosen interaction surface must preserve context and avoid unnecessary
navigation or modal interruption.

## Decision

Use:

- Inline forms for short, repeated entries that benefit from nearby grid, chart,
  or total context.
- Dialogs for self-contained focused decisions and destructive confirmation.
- Drawers for contextual editing, filters, and supporting details.
- Dedicated pages for long, complex, or multi-stage workflows.

## Transaction workspace rule

Keep transaction entry inline between a graph and transaction grid when users
benefit from entering a transaction and immediately reviewing resulting rows,
totals, and visual trends.

## Rationale

This supports efficient repeated financial entry while preserving context and
making the effect of changes easy to review.

## Consequences

- Do not move an inline transaction form into a dialog merely to reduce visible
  page content.
- Move to a drawer or dedicated page only when form complexity or workflow
  disruption justifies it.