import { useEffect, useRef } from 'react'

export function useAlertFocus(message: string | null) {
  const alertRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (message) alertRef.current?.focus()
  }, [message])

  return alertRef
}

export function useFormErrorFocus(errorFields: object, generalError: string | null) {
  const formRef = useRef<HTMLFormElement | null>(null)
  const errorRef = useAlertFocus(generalError)

  useEffect(() => {
    formRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()
  }, [errorFields])

  return { formRef, errorRef }
}
