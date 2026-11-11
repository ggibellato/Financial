export interface FieldError {
  fieldId: string
  message: string
}

const INVALID_FIELD = '[aria-invalid="true"]:not(:disabled)'

function validationMessageOf(field: HTMLElement): string {
  const messageId = field.getAttribute('aria-describedby')?.split(' ')[0]
  const message = (messageId && document.getElementById(messageId)?.textContent?.trim()) || ''
  const group = field.closest('[role="group"][aria-label]')?.getAttribute('aria-label')
  return group ? `${group}: ${message}` : message
}

export function collectFieldErrors(container: HTMLElement | null): FieldError[] {
  return Array.from(container?.querySelectorAll<HTMLElement>(INVALID_FIELD) ?? []).map((field) => ({
    fieldId: field.id,
    message: validationMessageOf(field),
  }))
}
