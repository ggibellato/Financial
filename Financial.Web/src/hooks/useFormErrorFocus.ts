import { useEffect, useRef } from 'react'

const EDITABLE_CONTROL = 'input, select, textarea'

export function useFormErrorFocus(errorFields: object) {
  const formRef = useRef<HTMLFormElement | null>(null)

  useEffect(() => {
    const form = formRef.current
    const focused = document.activeElement
    if (focused && form?.contains(focused) && focused.matches(EDITABLE_CONTROL)) return
    form?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()
  }, [errorFields])

  return formRef
}
