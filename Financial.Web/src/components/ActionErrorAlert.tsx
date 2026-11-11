import { useEffect, useRef } from 'react'
import { MessageBar, MessageBarBody } from '@fluentui/react-components'

interface ActionErrorAlertProps {
  message: string | null | undefined
  className?: string
}

export default function ActionErrorAlert({ message, className }: ActionErrorAlertProps) {
  const ref = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (message) ref.current?.focus()
  }, [message])

  if (!message) return null
  return (
    <MessageBar ref={ref} role="alert" intent="error" tabIndex={-1} className={className}>
      <MessageBarBody>{message}</MessageBarBody>
    </MessageBar>
  )
}
