import { useEffect, useRef, useState } from 'react'
import { isDuplicateName } from '../api/apiError'

interface RejectedName {
  failure: unknown
  nameKey: string
}

export function useDuplicateNameField<T extends HTMLElement = HTMLInputElement>(
  failure: unknown,
  error: string | null,
  nameKey: string,
) {
  const nameRef = useRef<T>(null)
  const [rejected, setRejected] = useState<RejectedName | null>(null)
  const isDuplicate = isDuplicateName(failure)
  if (isDuplicate && rejected?.failure !== failure) setRejected({ failure, nameKey })
  const isNameConflict = isDuplicate && rejected?.nameKey === nameKey

  useEffect(() => {
    if (isNameConflict) nameRef.current?.focus()
  }, [isNameConflict])

  return {
    nameRef,
    nameError: isNameConflict ? error : null,
    summaryError: isDuplicate ? null : error,
  }
}
