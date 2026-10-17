import { useState } from 'react'
import { getErrorMessage } from '../utils/formatters'

export function useDialogSubmit(fallbackErrorMessage: string) {
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const submit = async (save: () => Promise<unknown>) => {
    setIsSaving(true)
    setError(null)
    try {
      await save()
    } catch (err: unknown) {
      setError(getErrorMessage(err, fallbackErrorMessage))
      setIsSaving(false)
    }
  }

  return { isSaving, error, submit }
}
