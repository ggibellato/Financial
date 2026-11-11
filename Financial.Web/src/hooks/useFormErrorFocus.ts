import { useEffect, useRef } from 'react'

const EDITABLE_CONTROL = 'input, select, textarea'

function erroredKeys(errorFields: object): Set<string> {
  return new Set(Object.entries(errorFields).filter(([, message]) => message).map(([key]) => key))
}

export function useFormErrorFocus(errorFields: object) {
  const formRef = useRef<HTMLFormElement | null>(null)
  const previousKeys = useRef<Set<string>>(new Set())

  useEffect(() => {
    const keys = erroredKeys(errorFields)
    const hasNewError = [...keys].some((key) => !previousKeys.current.has(key))
    previousKeys.current = keys
    const form = formRef.current
    const focused = document.activeElement
    const isTyping = !!focused && !!form?.contains(focused) && focused.matches(EDITABLE_CONTROL)
    if (isTyping && !hasNewError) return
    form?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()
  }, [errorFields])

  return formRef
}
