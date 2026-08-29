/**
 * Shared per-field validation lookup (P38-F02). Returns a function that surfaces the current
 * save error only under the field it belongs to, so the caller can pass it straight to Fluent
 * `Field`'s `validationMessage`/`validationState` props instead of hand-writing the same
 * `saveErrorField === field` check per form.
 */
export function useFieldError<TField extends string>(
  saveError: string | null,
  saveErrorField: TField | null,
): (field: TField) => string | null {
  return (field) => (saveErrorField === field ? saveError : null)
}
