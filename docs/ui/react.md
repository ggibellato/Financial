# React UI Rules

## React UX authority

`Financial.Web` is the UX source of truth for cross-platform workflows.

React defines the intended:

- User task sequence
- Information hierarchy
- Form field order and grouping
- Terminology
- Action labels and priority
- Validation behavior and wording
- Loading, empty, saving, success, and error outcomes
- Financial formatting
- Totals behavior
- Responsive prioritization

Before changing a React workflow, verify it against product/domain rules,
accessibility requirements, Fluent 2 principles, and the UX principles in this
directory. Current React behavior is not automatically correct merely because it
is the reference.

## Component system

- Use the Fluent UI React version adopted in `ADR-004`.
- Reuse project components and wrappers before using raw Fluent components.
- Do not add a competing component or styling system without approval.
- Use semantic HTML and native browser semantics where practical.

## Layout

- Use CSS Grid for page and form structure.
- Use Flexbox for simple linear groups.
- Use the established project token/styling system.
- Preserve logical DOM reading order when layouts reflow.
- Keep responsive behavior near the component or within the established styling
  layer.

## Forms

- Use visible labels.
- Use semantic `form` elements where submission applies.
- Use suitable HTML input type, input mode, autocomplete, and descriptive
  semantics.
- Connect help and validation messages to inputs.
- Do not use `aria-label` as a replacement for a visible label.

## Accessibility

- Use headings and landmarks appropriately.
- Prefer native semantics over custom ARIA roles.
- Manage focus after dialogs, drawers, asynchronous updates, and validation
  failure.
- Keep focus outlines or supply an equivalent compliant focus treatment.
- Use accessible live status behavior where necessary.

## Required state model

Async UI must define:

- Initial
- Loading
- Loaded
- Empty
- Error
- Retry where appropriate

Editable UI must define:

- Clean
- Dirty
- Valid
- Invalid
- Saving
- Save succeeded
- Save failed

## Verification

For meaningful UI changes, update applicable:

- Vitest component or unit tests
- Contract-related tests where API-visible data changes
- Playwright smoke coverage where a critical workflow changes
- Accessibility-oriented assertions where the test stack supports them
- `npm run lint`
- `npm test`
- `npm run build`