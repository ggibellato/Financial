import { useState } from 'react'
import { getErrorMessage } from '../utils/formatters'

export function useDialogSubmit(fallbackErrorMessage: string) {
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [failure, setFailure] = useState<unknown>(null)

  const submit = async (save: () => Promise<unknown>) => {
    setIsSaving(true)
    setError(null)
    setFailure(null)
    try {
      await save()
    } catch (err: unknown) {
      setError(getErrorMessage(err, fallbackErrorMessage))
      setFailure(err)
      setIsSaving(false)
    }
  }

  return { isSaving, error, failure, submit }
}
