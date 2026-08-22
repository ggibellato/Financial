# Accessibility Standard

## Baseline

Meet WCAG 2.2 AA where applicable.

Apply equivalent accessibility outcomes in React and WPF even where their
technical mechanisms differ.

## Keyboard and focus

All interactive UI must be keyboard operable.

Required behavior:

- Logical Tab and Shift+Tab order
- Visible focus indicator
- No unexpected focus loss
- Escape closes temporary UI where appropriate
- Dialogs keep focus while open and restore it to the invoking control on close
- Menus, trees, grids, and composite controls follow expected keyboard patterns
- No keyboard trap except deliberate modal behavior

## Semantics and accessible names

### React

- Use native semantic HTML first.
- Associate labels with inputs.
- Use logical heading levels.
- Use ARIA only where native semantics are insufficient.
- Announce relevant asynchronous status changes.

### WPF

- Preserve UI Automation support.
- Use `AutomationProperties.Name`, help text, and related properties where
  control purpose is not evident.
- Ensure logical tab navigation.
- Ensure validation state and messages are discoverable by assistive technology.

## Visual accessibility

- Do not use colour alone for status, error, warning, selection, or required
  fields.
- Meet contrast requirements.
- Provide visible focus.
- Support light and dark themes.
- Support high contrast where the platform provides it.
- Do not make essential information dependent on small text.
- Do not clip labels, values, validation messages, or actions under text scaling.

## Zoom and adaptive layout

Web must remain usable at:

- 200% text scaling
- 400% browser zoom
- 320px effective viewport width

WPF must remain usable with:

- High DPI
- Increased system text scaling
- Narrower application windows
- High-contrast settings where supported

## Forms and errors

- Labels remain visible.
- Required fields are identifiable without colour alone.
- Errors identify the affected field or region.
- Errors explain how to correct the problem.
- Error summaries and inline messages are accessible.
- Server errors preserve entered data where possible.

## Charts, icons, and status

- Icon-only controls have accessible names.
- Tooltips supplement but do not replace accessible names.
- Charts provide an accessible summary or equivalent data.
- Status messages contain text or accessible status information.