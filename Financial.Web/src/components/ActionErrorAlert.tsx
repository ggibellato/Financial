import { useEffect, useId, useRef } from 'react'
import { MessageBar, MessageBarBody } from '@fluentui/react-components'

interface ActionErrorAlertProps {
  message: string | null | undefined
  className?: string
}

export default function ActionErrorAlert({ message, className }: ActionErrorAlertProps) {
  const ref = useRef<HTMLDivElement | null>(null)
  const bodyId = useId()

  useEffect(() => {
    if (!message) return
    ref.current?.scrollIntoView?.({ block: 'nearest' })
    ref.current?.focus()
  }, [message])

  if (!message) return null
  return (
    <MessageBar ref={ref} intent="error" tabIndex={-1} aria-labelledby={bodyId} className={className}>
      <MessageBarBody id={bodyId}>{message}</MessageBarBody>
    </MessageBar>
  )
}
