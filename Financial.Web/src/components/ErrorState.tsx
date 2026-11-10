import { useEffect, useRef } from 'react'
import { Button } from '@fluentui/react-components'

interface ErrorStateProps {
  message: string
  onRetry?: () => void
}

function isFocusLost() {
  return document.activeElement === null || document.activeElement === document.body
}

export default function ErrorState({ message, onRetry }: ErrorStateProps) {
  const alertRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (isFocusLost()) alertRef.current?.focus()
  }, [])

  return (
    <div role="alert" ref={alertRef} tabIndex={-1}>
      <p>{message}</p>
      {onRetry ? (
        <Button appearance="primary" onClick={onRetry}>
          Try again
        </Button>
      ) : null}
    </div>
  )
}
