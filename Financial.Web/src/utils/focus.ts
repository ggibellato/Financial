export const EDITABLE_FIELD =
  'input:not([type="hidden"]):not([readonly]):not([disabled]), select:not([disabled]), textarea:not([readonly]):not([disabled])'

export function isFocusLost(): boolean {
  return document.activeElement === null || document.activeElement === document.body
}

export function focusField(fieldId: string) {
  const field = document.getElementById(fieldId)
  const target = field?.matches(EDITABLE_FIELD)
    ? field
    : (field?.querySelector<HTMLElement>('input:checked:not([disabled])') ?? field?.querySelector<HTMLElement>(EDITABLE_FIELD) ?? field)
  target?.focus()
}
