import { Button } from '@fluentui/react-components'

const ACTION_LABELS: Record<string, string> = {
  AddExpense: 'Add expense',
  AddIncome: 'Add income',
}

interface AccountActionBarProps {
  allowedActions: string[]
  activeAction: string | null
  formAreaId: string
  disabled?: boolean
  onAction: (action: string, button: HTMLButtonElement) => void
}

export default function AccountActionBar({ allowedActions, activeAction, formAreaId, disabled, onAction }: AccountActionBarProps) {
  const knownActions = allowedActions.filter((action) => action in ACTION_LABELS)
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
