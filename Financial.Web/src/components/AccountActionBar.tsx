import { Button } from '@fluentui/react-components'
import { ACTION_LABELS, formActionsOf } from './accountActions'

interface AccountActionBarProps {
  allowedActions: string[]
  activeAction: string | null
  formAreaId: string
  disabled?: boolean
  onAction: (action: string, button: HTMLButtonElement) => void
}

export default function AccountActionBar({ allowedActions, activeAction, formAreaId, disabled, onAction }: AccountActionBarProps) {
  const knownActions = formActionsOf(allowedActions)
  if (knownActions.length === 0) return null

  return (
    <div className="account-screen__actions" role="group" aria-label="Account actions">
      {knownActions.map((action) => (
        <Button
          key={action}
          appearance="primary"
          disabled={disabled}
          aria-expanded={activeAction === action}
          aria-controls={formAreaId}
          onClick={(event) => onAction(action, event.currentTarget)}
        >
          {ACTION_LABELS[action]}
        </Button>
      ))}
    </div>
  )
}
