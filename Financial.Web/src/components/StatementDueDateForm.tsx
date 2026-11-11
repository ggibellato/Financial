import { useEffect, useRef } from 'react'
import { Button, Field, Input, MessageBar, MessageBarBody, Spinner, Text } from '@fluentui/react-components'
import { useAlertFocus } from '../hooks/useFormErrorFocus'
import { dueDateRangeOf, type DueDateFormState, type StatementDueDateFormHandle } from '../hooks/useStatementDueDateForm'
import { formatLongMonthYear } from '../utils/formatters'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'

interface StatementDueDateFormProps {
  form: DueDateFormState
  dueDateForm: StatementDueDateFormHandle
  onCancel: () => void
}

export default function StatementDueDateForm({ form, dueDateForm, onCancel }: StatementDueDateFormProps) {
  const styles = useFormPanelStyles()
  const dateRef = useRef<HTMLInputElement | null>(null)
  const { failedValidations, setDate, save, clear } = dueDateForm
  const { statement, date, pendingAction, error, dateError } = form
  const { min, max } = dueDateRangeOf(statement)
  const errorRef = useAlertFocus(error)

  useEffect(() => {
    dateRef.current?.focus()
  }, [failedValidations])

  return (
    <form
      className={styles.panel}
      noValidate
      aria-labelledby="statement-due-date-title"
      onSubmit={(event) => {
        event.preventDefault()
        save()
      }}
    >
      <fieldset disabled={pendingAction !== null} className={styles.savingFieldset}>
        <Text as="h3" id="statement-due-date-title" weight="semibold" size={400}>
          {formatLongMonthYear(statement.year, statement.month)} due date
        </Text>
        {error && (
          <MessageBar ref={errorRef} role="alert" intent="error" tabIndex={-1}>
            <MessageBarBody>{error}</MessageBarBody>
          </MessageBar>
        )}
        <div className={styles.grid}>
          <Field label="Due date" required validationState={dateError ? 'error' : 'none'} validationMessage={dateError}>
            <Input ref={dateRef} type="date" min={min} max={max} value={date} onChange={(event) => setDate(event.target.value)} />
          </Field>
        </div>
      </fieldset>
      <FormSaveActions saveLabel="Save due date" isSaving={pendingAction === 'save'} isBusy={pendingAction !== null} onCancel={onCancel}>
        {statement.dueDate && (
          <Button
            className={styles.deleteAction}
            appearance="primary"
            disabled={pendingAction === 'save'}
            disabledFocusable={pendingAction === 'clear'}
            icon={pendingAction === 'clear' ? <Spinner size="tiny" /> : undefined}
            onClick={clear}
          >
            {pendingAction === 'clear' ? 'Clearing…' : 'Clear due date'}
          </Button>
        )}
      </FormSaveActions>
    </form>
  )
}
