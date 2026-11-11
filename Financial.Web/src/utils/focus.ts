export const EDITABLE_FIELD =
  'input:not([type="hidden"]):not([readonly]):not([disabled]), select:not([disabled]), textarea:not([readonly]):not([disabled])'

export function isFocusLost(): boolean {
  return document.activeElement === null || document.activeElement === document.body
}
