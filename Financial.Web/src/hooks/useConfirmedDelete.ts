import { useState } from 'react'
import { getDeleteErrorMessage } from '../utils/formatters'

export function useConfirmedDelete<T>(
  remove: (target: T) => Promise<void>,
  errorFallback: (target: T) => string,
  onDeleted: (target: T) => void,
) {
  const [target, setTarget] = useState<T | null>(null)
  const [isOpen, setIsOpen] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const request = (requested: T) => {
    setTarget(requested)
    setIsOpen(true)
    setError(null)
  }

  const cancel = () => {
    if (!isDeleting) setIsOpen(false)
  }

  const confirm = () => {
    if (target === null || !isOpen) return
    const deleted = target
    setIsDeleting(true)
    setError(null)
    void remove(deleted)
      .then(() => {
        setIsOpen(false)
        onDeleted(deleted)
      })
      .catch((err: unknown) => setError(getDeleteErrorMessage(err, errorFallback(deleted))))
      .finally(() => setIsDeleting(false))
  }

  return { target, isOpen, isDeleting, error, request, cancel, confirm }
}
