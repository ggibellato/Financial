import { useEffect, useRef } from 'react'
import { focusReloadedContent, isAwaitingRetryOutcome } from '../utils/retryOutcomeFocus'

interface LoadingStateProps {
  message?: string
}

export default function LoadingState({ message = 'Loading…' }: LoadingStateProps) {
  const statusRef = useRef<HTMLParagraphElement>(null)

  useEffect(() => {
    const region = statusRef.current?.parentElement
    const loadsRetryOutcome = isAwaitingRetryOutcome()
    return () => {
      if (region && loadsRetryOutcome) focusReloadedContent(region)
    }
  }, [])

  return (
    <p ref={statusRef} role="status">
      {message}
    </p>
  )
}
