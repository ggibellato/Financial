import { useEffect, useRef } from 'react'
import { Button, Field, Input, MessageBar, MessageBarBody, Spinner, Text } from '@fluentui/react-components'
import { dueDateRangeOf, type DueDateFormState, type StatementDueDateFormHandle } from '../hooks/useStatementDueDateForm'
import { formatLongMonthYear } from '../utils/formatters'
import { useFormPanelStyles } from './formPanelStyles'

interface StatementDueDateFormProps {
  form: DueDateFormState
  dueDateForm: StatementDueDateFormHandle
  onCancel: () => void
}

export default function StatementDueDateForm({ form, dueDateForm, onCancel }: StatementDueDateFormProps) {
  const styles = useFormPanelStyles()
  const dateRef = useRef<HTMLInputElement | null>(null)
  const { failedSubmits, setDate, save, clear } = dueDateForm
  const { statement, date, pendingAction, error, dateError } = form
  const { min, max } = dueDateRangeOf(statement)
  const isSaving = pendingAction !== null

  useEffect(() => {
    dateRef.current?.focus()
  }, [failedSubmits])

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
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Text as="h3" id="statement-due-date-title" weight="semibold" size={400}>
          {formatLongMonthYear(statement.year, statement.month)} due date
        </Text>
        {error && (
          <MessageBar role="alert" intent="error">
            <MessageBarBody>{error}</MessageBarBody>
          </MessageBar>
        )}
        <div className={styles.grid}>
          <Field label="Due date" required validationState={dateError ? 'error' : 'none'} validationMessage={dateError}>
            <Input ref={dateRef} type="date" min={min} max={max} value={date} onChange={(event) => setDate(event.target.value)} />
          </Field>
        </div>
        <div className={styles.actions}>
          <Button type="submit" appearance="primary" disabled={isSaving} icon={pendingAction === 'save' ? <Spinner size="tiny" /> : undefined}>
            {pendingAction === 'save' ? 'Saving...' : 'Save due date'}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
          {statement.dueDate && (
            <Button
              className={styles.deleteAction}
              appearance="primary"
              disabled={isSaving}
              icon={pendingAction === 'clear' ? <Spinner size="tiny" /> : undefined}
              onClick={clear}
            >
              {pendingAction === 'clear' ? 'Clearing...' : 'Clear due date'}
            </Button>
          )}
        </div>
      </fieldset>
    </form>
  )
}
