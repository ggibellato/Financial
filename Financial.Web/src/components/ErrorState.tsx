import { useId, useLayoutEffect, useRef, useState } from 'react'
import { Button } from '@fluentui/react-components'
import { awaitRetryOutcome, claimFocusForRetryOutcome } from '../utils/retryOutcomeFocus'

interface ErrorStateProps {
  message: string
  onRetry?: () => void
}

export default function ErrorState({ message, onRetry }: ErrorStateProps) {
  const errorRef = useRef<HTMLDivElement>(null)
  const messageId = useId()
  const [takesFocus, setTakesFocus] = useState(false)

  useLayoutEffect(() => {
    if (!errorRef.current || !claimFocusForRetryOutcome(errorRef.current)) return
    setTakesFocus(true)
    errorRef.current.focus()
  }, [])

  const retry = () => {
    awaitRetryOutcome(errorRef.current?.parentElement ?? null)
    onRetry?.()
  }

  return (
    <div
      ref={errorRef}
      tabIndex={-1}
      {...(takesFocus ? { role: 'group', 'aria-labelledby': messageId } : { role: 'alert' })}
    >
      <p id={messageId}>{message}</p>
      {onRetry ? (
        <Button appearance="primary" onClick={retry}>
          Try again
        </Button>
      ) : null}
    </div>
  )
}
