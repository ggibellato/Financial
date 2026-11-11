import { useState } from 'react'

export function useEntityForm(values: object) {
  const [initialValues] = useState(() => JSON.stringify(values))
  const [isValidationShown, setIsValidationShown] = useState(false)
  const shown = (message: string) => (isValidationShown ? message : '')

  return {
    isDirty: JSON.stringify(values) !== initialValues,
    shown,
    fieldValidation: (message: string) => ({
      validationState: shown(message) ? ('error' as const) : ('none' as const),
      validationMessage: shown(message),
    }),
    revealValidation: () => setIsValidationShown(true),
  }
}
