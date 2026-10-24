import { Button } from '@fluentui/react-components'
import { ACTION_LABELS, formActionsOf, type AccountFormAction } from './accountActions'

interface AccountActionBarProps {
  allowedActions: string[]
  activeAction: AccountFormAction | null
  formAreaId: string
  isActionDisabled?: (action: AccountFormAction) => boolean
  onAction: (action: AccountFormAction, button: HTMLButtonElement) => void
}

export default function AccountActionBar({
  allowedActions,
  activeAction,
  formAreaId,
  isActionDisabled = () => false,
  onAction,
}: AccountActionBarProps) {
  const knownActions = formActionsOf(allowedActions)
  if (knownActions.length === 0) return null

  return (
    <div className="account-screen__actions" role="group" aria-label="Account actions">
      {knownActions.map((action) => (
        <Button
          key={action}
          appearance="primary"
          disabled={isActionDisabled(action)}
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
