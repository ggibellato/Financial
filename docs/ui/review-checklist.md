# UI Review Checklist

## Workflow and financial clarity

- [ ] The primary user task is obvious.
- [ ] The relevant account, portfolio, period, category, or scope is visible.
- [ ] Financial values, units, signs, precision, and date formats are unambiguous.
- [ ] Filtered totals and grand totals are clearly distinguished.
- [ ] User context is preserved through loading, saving, success, and recoverable failure.
- [ ] The design supports efficient repeated single-user financial work.

## Fluent and visual consistency

- [ ] Existing Fluent components or project wrappers are reused.
- [ ] Semantic tokens/resources are used.
- [ ] No repeated arbitrary visual values were introduced.
- [ ] Spacing follows the shared rhythm.
- [ ] Typography provides clear hierarchy.
- [ ] Cards, borders, shadows, and surfaces are restrained.
- [ ] Light and dark behavior is considered.

## Forms

- [ ] For any changed trigger, form title, or confirm button: the full
      trigger → title → confirm chain was checked together, not just the
      element named in the task — a fix to one link can leave (or create) a
      mismatch in the other two.
- [ ] Field order follows the documented default or has a documented exception.
- [ ] Labels are visible and associated with controls.
- [ ] Field widths suit their data.
- [ ] One primary action exists per form/region.
- [ ] Validation is specific, actionable, and accessible.
- [ ] Saving state prevents duplicate actions.
- [ ] Failed saves preserve entered data.
- [ ] Unsaved changes are protected where applicable.

## Data views

- [ ] Grids provide clear headers, filtering, sorting, selection, loading, empty, and error states.
- [ ] Numeric and financial values align and format consistently.
- [ ] Totals are visually and semantically distinct.
- [ ] Trees communicate hierarchy and support keyboard navigation.
- [ ] Charts have titles, labels, units, correct state handling, and accessible equivalents.
- [ ] Dense views remain readable.

## Responsive and adaptive behavior

- [ ] Web layout works at wide, medium, and narrow widths.
- [ ] Web forms reflow without unusable controls.
- [ ] Web remains usable at required zoom/text scale.
- [ ] WPF works at narrow window sizes, high DPI, and increased text scale.
- [ ] Essential information/actions are not removed without an alternative.

## Accessibility

- [ ] All functionality is keyboard operable.
- [ ] Focus is visible and logical.
- [ ] Dialogs, drawers, and menus restore focus correctly.
- [ ] Icon-only controls have accessible names.
- [ ] Status is not communicated by colour alone.
- [ ] Contrast is sufficient.
- [ ] Errors are clear, accessible, and recoverable.
- [ ] High contrast is considered where supported.

## React-led WPF parity

- [ ] React behavior was reviewed against repository UI standards.
- [ ] WPF preserves the React-defined user task and information hierarchy.
- [ ] WPF terminology matches React.
- [ ] WPF field order matches React unless a documented desktop exception exists.
- [ ] WPF action labels and priority match React.
- [ ] WPF validation wording and meaning match React.
- [ ] WPF financial formatting and totals behavior match React.
- [ ] WPF loading, empty, saving, success, and error outcomes match React.
- [ ] WPF keyboard and focus behavior is equivalent or better for desktop use.
- [ ] Intentional WPF differences are documented and justified by desktop convention, accessibility, or better usability.