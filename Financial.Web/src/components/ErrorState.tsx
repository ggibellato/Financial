import { Button } from '@fluentui/react-components'

interface ErrorStateProps {
  message: string
  onRetry?: () => void
}

export default function ErrorState({ message, onRetry }: ErrorStateProps) {
  return (
    <div role="alert">
      <p>{message}</p>
      {onRetry ? (
        <Button appearance="primary" onClick={onRetry}>
          Try again
        </Button>
      ) : null}
    </div>
  )
}
