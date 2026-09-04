/**
 * Shared per-field validation lookup. Returns a function that surfaces each field's own error
 * text (all invalid fields are shown at once, not just the first one hit), so the caller can pass
 * it straight to Fluent `Field`'s `validationMessage`/`validationState` props instead of
 * hand-writing the same `saveErrorFields[field]` lookup per form.
 */
export function useFieldError<TField extends string>(
  saveErrorFields: Partial<Record<TField, string>> | null | undefined,
): (field: TField) => string | null {
  return (field) => saveErrorFields?.[field] ?? null
}
