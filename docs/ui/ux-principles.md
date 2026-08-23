# UX Principles

## Accuracy before decoration

Financial information must be clear, accurate, and unambiguous.

Prioritize:

- Correct currency, unit, date, sign, precision, and total formatting
- Clear distinction between planned, pending, confirmed, and historical values
- Clear distinction between values, percentages, quantities, prices, and rates
- Visible calculation scope, such as filtered total versus all-time total
- Error prevention before visual flourish

## Make the task and context obvious

Every page should let the user understand:

- Where they are
- Which account, investment, period, or category applies
- What data they are viewing
- What requires attention
- Which action is primary
- What happens after an action

Use clear titles, headings, stable layout, action labels, status messages, and
visible context.

## Preserve context

Do not unexpectedly reset:

- Filters
- Sort order
- Selection
- Date range
- Account/portfolio scope
- Entered values
- The current page or detail view

Saving should preserve context. Failed saves must preserve entered data unless
there is a documented technical reason they cannot.

## Design for scanning

Financial work often involves dense information.

Use:

- Predictable headings
- Stable columns
- Right-aligned numeric values
- Clear totals
- Restrained grouping
- Consistent status indicators
- Adequate whitespace
- Visible filters and sort state
- Meaningful empty and error states

## One primary action per region

A page, form, or focused task area normally has one primary action.

Use specific labels:

- Save transaction
- Add expense
- Retry
- Delete investment
- Export transactions

Avoid vague labels such as OK, Submit, or Continue unless their effect is
unambiguous.

## Prevent and recover from errors

Prevent invalid actions where practical.

When errors occur:

- Explain what happened.
- Explain how to correct it.
- Preserve entered values.
- Keep the message near the relevant field or region.
- Offer retry where appropriate.
- Do not rely only on a disappearing notification.

## Recognition over recall

Keep relevant state visible:

- Selected account, portfolio, category, or period
- Active filters and sort order
- Currency and units
- Required fields
- Date range
- Chart legends and data meaning
- Unsaved-change state
- Relevant prior values where comparison matters

## Efficient repeated use

This is a single-user financial tool. Support efficient repeat workflows with:

- Predictable field order
- Good tab order
- Keyboard operation
- Sensible defaults
- Useful data density
- Retained context where appropriate
- Minimal unnecessary confirmation dialogs
- Clear, fast data entry

## Progressive disclosure

Show what is needed for the current task and keep secondary content available
without competing with the primary task.

Use drawers, expandable sections, overflow menus, detail panes, or dialogs when
they preserve context and reduce cognitive load.

## Screenshot rule

A screenshot reflects the current implementation, not the final required design.

Improve a screenshot-derived design whenever Fluent principles, accessibility,
responsiveness, workflow clarity, financial accuracy, or React/WPF consistency
indicate a better solution.