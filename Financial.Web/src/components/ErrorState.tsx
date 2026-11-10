import { useEffect, useRef } from 'react'
import { Button } from '@fluentui/react-components'

interface ErrorStateProps {
  message: string
  onRetry?: () => void
}

let isRetryAwaitingOutcome = false

function isFocusLost() {
  return document.activeElement === null || document.activeElement === document.body
}

export default function ErrorState({ message, onRetry }: ErrorStateProps) {
  const alertRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (isRetryAwaitingOutcome && isFocusLost()) alertRef.current?.focus()
    isRetryAwaitingOutcome = false
  }, [])

  const retry = () => {
    isRetryAwaitingOutcome = true
    document.addEventListener('focusin', () => (isRetryAwaitingOutcome = false), { once: true })
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
