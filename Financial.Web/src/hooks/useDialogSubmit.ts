import { useState } from 'react'
import { ApiError } from '../api/apiError'
import { getErrorMessage } from '../utils/formatters'

export function useDialogSubmit(fallbackErrorMessage: string) {
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [errorStatus, setErrorStatus] = useState<number | null>(null)

  const submit = async (save: () => Promise<unknown>) => {
    setIsSaving(true)
    setError(null)
    setErrorStatus(null)
    try {
      await save()
    } catch (err: unknown) {
      setError(getErrorMessage(err, fallbackErrorMessage))
      setErrorStatus(err instanceof ApiError ? err.status : null)
      setIsSaving(false)
    }
  }

  return { isSaving, error, errorStatus, submit }
}
