import { useEffect, useRef } from 'react'

export function useFormErrorFocus(errorFields: object, generalError: string | null) {
  const formRef = useRef<HTMLFormElement | null>(null)
  const saveRef = useRef<HTMLButtonElement | null>(null)
  const errorRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    formRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()
  }, [errorFields])

  useEffect(() => {
    if (generalError) (errorRef.current ?? saveRef.current)?.focus()
  }, [generalError])

  return { formRef, saveRef, errorRef }
}
