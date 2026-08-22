# ADR-002: Responsive Form Layout and Field Order

## Status

Accepted

## Context

Financial forms include date, transaction type, account/broker/category
selection, descriptions, quantities, prices, amounts, fees, and notes.

An overly wide single-row form wastes space, is difficult to scan, and fails
poorly at narrow widths or increased text scale.

## Decision

Financial forms use a content-aware responsive grid:

- Four columns by default on wide layouts.
- Two columns by default on medium layouts.
- One column by default on narrow layouts.

Fields may span columns according to their content.

Default field order:

1. Date and time
2. Related entity/classification
3. Description/free-text detail
4. Quantity/financial value
5. Optional metadata
6. Actions

## Rationale

This keeps financial data-entry forms compact on desktop displays without
creating excessively wide or hard-to-scan rows. It preserves a predictable
React-led sequence while allowing WPF and React to adapt layout to available
space.

## Consequences

- Four columns are a default, not an absolute rule.
- Descriptions, notes, and complex selectors may span multiple columns.
- A different field order must be justified by workflow and documented in a page
  specification or another ADR.