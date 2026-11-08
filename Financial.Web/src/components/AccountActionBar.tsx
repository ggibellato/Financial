import { Button, makeStyles, Text } from '@fluentui/react-components'
import { ACTION_LABELS, formActionsOf, opensDialog, type AccountFormAction } from './accountActions'

interface AccountActionBarProps {
  allowedActions: string[]
  activeAction: AccountFormAction | null
  formAreaId: string
  isActionDisabled?: (action: AccountFormAction) => boolean
  disabledReasons?: Readonly<Record<string, string>>
  onAction: (action: AccountFormAction, button: HTMLButtonElement) => void
}

const useActionBarStyles = makeStyles({
  reason: { alignSelf: 'center' },
})

const reasonId = (action: AccountFormAction) => `account-action-reason-${action}`

export default function AccountActionBar({
  allowedActions,
  activeAction,
  formAreaId,
  isActionDisabled = () => false,
  disabledReasons = {},
  onAction,
}: AccountActionBarProps) {
  const styles = useActionBarStyles()
  const knownActions = formActionsOf(allowedActions)
  if (knownActions.length === 0) return null

  return (
    <div className="account-screen__actions" role="group" aria-label="Account actions">
      {knownActions.map((action) => (
        <Button
          key={action}
          appearance="primary"
          disabled={!(action in disabledReasons) && isActionDisabled(action)}
          disabledFocusable={action in disabledReasons}
          aria-describedby={action in disabledReasons ? reasonId(action) : undefined}
          {...(opensDialog(action)
            ? { 'aria-haspopup': 'dialog' as const }
            : { 'aria-expanded': activeAction === action, 'aria-controls': formAreaId })}
          onClick={(event) => onAction(action, event.currentTarget)}
        >
          {ACTION_LABELS[action]}
        </Button>
      ))}
      {knownActions.map(
        (action) =>
          action in disabledReasons && (
            <Text key={action} id={reasonId(action)} className={styles.reason}>
              {disabledReasons[action]}
            </Text>
          ),
      )}
    </div>
  )
}
