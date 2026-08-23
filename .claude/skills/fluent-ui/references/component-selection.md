# Component Selection

## Use an inline form when

- The task is short.
- Users may enter several records in sequence.
- Nearby graph, grid, or totals context matters.
- The user should see results immediately after saving.

## Use a dialog when

- The task is self-contained.
- The user must make a focused decision.
- A destructive action needs confirmation.
- The interaction should temporarily block the underlying page.

## Use a drawer when

- Details, filters, or secondary editing must preserve page context.
- The work relates to a selected grid/tree item.
- The task should not dominate the main page.

## Use a dedicated page when

- The workflow is long, complex, or multi-stage.
- The user needs uninterrupted focus.
- There are many sections, validations, or review steps.

## Control selection

| Need | Preferred pattern |
|---|---|
| Main action | One primary button |
| Secondary path | Secondary button |
| Destructive action | Separate placement plus danger styling |
| Several row actions | Visible primary action plus overflow menu |
| Navigation | Link, navigation item, breadcrumb, or tab |
| Immediate boolean setting | Switch |
| Independent choice | Checkbox |
| One choice from a few options | Radio group |
| Search/select entity | Combobox/autocomplete |
| Short known list | Select |
| Date value | Date picker |
| Long descriptive input | Textarea |

## Status messages

| Situation | Use |
|---|---|
| One invalid field | Inline validation |
| Several invalid fields | Summary plus inline validation |
| Main page cannot load | Persistent page-level error with retry |
| Successful background action | Non-blocking success notification |
| Critical/important failure | Persistent message, not only a toast |
| Destructive confirmation | Dialog |
| Contextual guidance | Inline help/message |