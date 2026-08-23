# UI Standards Hierarchy

## Visual design system

Use Microsoft Fluent 2 as the primary visual and component design system.

Use Fluent 2 for:

- Semantic tokens
- Typography
- Spacing
- Color
- Surfaces and elevation
- Component anatomy and interaction states
- Layout patterns
- Light and dark theme behavior
- Fluent UI React usage where available

Fluent 2 provides the preferred visual language. It does not override
accessibility, financial-domain requirements, or a demonstrably more usable
workflow.

## Accessibility baseline

Use WCAG 2.2 AA as the accessibility baseline where applicable.

Apply equivalent outcomes to both React and WPF:

- Keyboard operation
- Visible focus
- Logical focus management
- Accessible names and labels
- Error identification and recovery
- Sufficient contrast
- Non-color status communication
- Zoom, text scaling, high DPI, and adaptive reflow
- Accessible status changes
- Accessible chart alternatives

## Usability review framework

Use Nielsen Norman Group usability heuristics when designing or reviewing
significant workflows.

Prioritize:

- Visibility of system status
- Match with the user’s financial language and mental model
- User control and freedom
- Consistency and standards
- Error prevention
- Recognition rather than recall
- Efficient repeated workflows
- Minimalist, task-focused interfaces
- Clear error recovery
- Contextual help for complex financial concepts

## Product and domain authority

Repository documentation and approved UI decisions are authoritative for:

- Financial terminology
- Investment and cash-flow workflows
- Form field order
- Financial data formatting
- Totals and calculations
- Grid columns and density
- Chart content
- Warning and error meaning
- Inline/dialog/drawer decisions
- React-to-WPF workflow equivalence

## Conflict resolution

When sources conflict:

1. Accessibility, privacy, security, and safety requirements win.
2. Confirmed financial/domain workflow requirements win over generic guidance.
3. Approved repository decisions win over new stylistic preferences.
4. React defines the intended cross-platform user experience.
5. Fluent 2 defines the preferred visual language.
6. Screenshots are references, not specifications.

## Prohibited design-system mixing

Do not introduce Material, Bootstrap, Ant Design, Carbon, a second styling
system, or another WPF UI framework without explicit approval and an ADR.

A platform may use native controls where required, but terminology, information
hierarchy, spacing rhythm, state meanings, and user outcomes must remain
consistent with the repository UI system.