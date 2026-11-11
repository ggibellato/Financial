export interface FieldError {
  fieldId: string
  message: string
}

const INVALID_FIELD = '[aria-invalid="true"]:not(:disabled)'

function textOf(ids: string | null): string {
  const elements = (ids ?? '')
    .split(' ')
    .map((id) => (id ? document.getElementById(id) : null))
    .filter((element): element is HTMLElement => !!element?.textContent?.trim())
  const chosen = elements.find((element) => element.getAttribute('role') === 'alert') ?? elements[0]
  return chosen?.textContent?.trim() ?? ''
}

function labelOf(field: HTMLElement): string {
  const labelledBy = textOf(field.getAttribute('aria-labelledby'))
  return labelledBy || (field as HTMLInputElement).labels?.[0]?.textContent?.trim() || field.getAttribute('aria-label') || ''
}

function validationMessageOf(field: HTMLElement): string {
  const message = textOf(field.getAttribute('aria-describedby')) || `${labelOf(field)}: check this field.`
  const group = field.closest('[role="group"][aria-label]')?.getAttribute('aria-label')
  return group ? `${group}: ${message}` : message
}

export function generalErrorOf(message: string | null, fieldErrors: object): string | null {
  return Object.keys(fieldErrors).length === 0 ? message : null
}

export function collectFieldErrors(container: HTMLElement | null): FieldError[] {
  return Array.from(container?.querySelectorAll<HTMLElement>(INVALID_FIELD) ?? [])
    .filter((field) => field.id)
    .map((field) => ({ fieldId: field.id, message: validationMessageOf(field) }))
}
