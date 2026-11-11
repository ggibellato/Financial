import { useEffect, useRef } from 'react'
import { Button } from '@fluentui/react-components'
import { awaitRetryOutcome, claimFocusForRetryOutcome } from '../utils/retryOutcomeFocus'

interface ErrorStateProps {
  message: string
  onRetry?: () => void
}

export default function ErrorState({ message, onRetry }: ErrorStateProps) {
  const alertRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (claimFocusForRetryOutcome()) alertRef.current?.focus()
  }, [])

  const retry = () => {
    awaitRetryOutcome()
    onRetry?.()
  }

  return (
    <div role="alert" ref={alertRef} tabIndex={-1}>
      <p>{message}</p>
      {onRetry ? (
        <Button appearance="primary" onClick={retry}>
          Try again
        </Button>
      ) : null}
    </div>
  )
}
