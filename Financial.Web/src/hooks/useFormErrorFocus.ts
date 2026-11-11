import { useEffect, useRef, type RefObject } from 'react'

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

export function useFocusSaveOnDialogClose(formRef: RefObject<HTMLFormElement | null>, isDialogOpen: boolean, error: string | null) {
  const wasDialogOpen = useRef(false)

  useEffect(() => {
    if (wasDialogOpen.current && !isDialogOpen && error === null) {
      formRef.current?.querySelector<HTMLButtonElement>('button[type="submit"]')?.focus()
    }
    wasDialogOpen.current = isDialogOpen
  }, [formRef, isDialogOpen, error])
}
