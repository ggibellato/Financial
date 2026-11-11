import { useState } from 'react'

export function useEntityForm(values: object) {
  const [initialValues] = useState(() => JSON.stringify(values))
  const [isValidationShown, setIsValidationShown] = useState(false)

  return {
    isDirty: JSON.stringify(values) !== initialValues,
    shown: (message: string) => (isValidationShown ? message : ''),
    revealValidation: () => setIsValidationShown(true),
  }
}
