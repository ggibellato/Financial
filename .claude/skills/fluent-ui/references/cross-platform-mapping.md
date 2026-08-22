# React-to-WPF Equivalence Map

React is the UX source of truth.

For each cross-platform feature:

1. Validate the intended React experience against `docs/ui/`.
2. Use React as the reference for workflow, terminology, hierarchy, field order,
   action priority, state behavior, and financial formatting.
3. Implement equivalent WPF behavior.
4. Document differences only when desktop conventions, accessibility, or a better
   WPF interaction require adaptation.

This mapping does not authorize WPF to change the user-facing workflow.

| React UX reference | React implementation | WPF equivalent | Required WPF outcome |
|---|---|---|---|
| Page title | Semantic heading | Heading style + automation identity | Same title and hierarchy |
| Primary action | Fluent primary button | Fluent-styled WPF button | Same label, priority, enabled logic |
| Labelled field | Fluent field/input | Labelled WPF control | Same label, help, required/error meaning |
| Entity lookup | Combobox/autocomplete | Searchable ComboBox | Same search, selection, and empty behavior |
| Date entry | Approved date picker | DatePicker | Same format and validation |
| Form validation | Inline field/summary | Validation template/summary | Same wording and recovery |
| Data grid | Project Web grid/table | DataGrid | Same columns, sort/filter meaning, totals |
| Tree | Project tree component | TreeView | Same selection and expansion meaning |
| Chart | Web chart component | WPF chart component | Same insight, units, and data alternative |
| Dialog | Fluent Dialog | Dialog/Window | Same decision, focus, and actions |
| Drawer | Fluent Drawer | Side pane | Same context and dismissal behavior |
| Status message | Message bar/status region | InfoBar/status control | Same severity, message, and persistence |
| Loading | Skeleton/progress | Progress indicator | Same scope and explanation |
| Empty state | Empty-state region | Empty panel | Same explanation and next action |