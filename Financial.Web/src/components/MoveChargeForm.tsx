import { useEffect, useRef } from 'react'
import { Button, Field, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import { currentStatementLabel, type MoveChargeFormState } from '../hooks/useMoveChargeForm'
import { formatN2, formatShortDateUtc } from '../utils/formatters'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'

const STATEMENT_HINT = 'Months with no statement yet are marked (new); paid statements are not listed.'
const NO_TARGETS_MESSAGE = 'No unpaid statement in the next 12 months.'

interface MoveChargeFormProps {
  moveForm: MoveChargeFormState
  onCancel: () => void
}

export default function MoveChargeForm({ moveForm, onCancel }: MoveChargeFormProps) {
  const styles = useFormPanelStyles()
  const statementRef = useRef<HTMLSelectElement | null>(null)
  const { form, targets, isLoadingTargets, hasNoTargets, targetsError, retryTargets, failedSubmitCount, setTarget, submit } = moveForm

  useEffect(() => {
    if (failedSubmitCount > 0) statementRef.current?.focus()
  }, [failedSubmitCount])

  if (!form) return null
  const { charge } = form

  return (
    <form
      className={styles.panel}
      noValidate
      aria-labelledby="move-charge-title"
      onSubmit={(event) => {
        event.preventDefault()
        submit()
      }}
    >
      <fieldset disabled={form.isSaving} className={styles.savingFieldset}>
        <Text as="h2" id="move-charge-title" weight="semibold" size={400}>
          Move charge to a later statement
        </Text>
        <Text as="p">
          {`${charge.description} (${formatShortDateUtc(charge.chargeDate ?? charge.date)}, ${formatN2(charge.value)}) is on the ${currentStatementLabel(charge.invoiceDate)} statement.`}
        </Text>
        {form.error && (
          <MessageBar intent="error">
            <MessageBarBody>{form.error}</MessageBarBody>
          </MessageBar>
        )}
        {targetsError && <ErrorState message={targetsError} onRetry={retryTargets} />}
        <div className={styles.grid}>
          <Field
            label="Statement"
            required
            className={styles.spanTwo}
            hint={hasNoTargets ? NO_TARGETS_MESSAGE : STATEMENT_HINT}
            validationState={form.targetError ? 'error' : 'none'}
            validationMessage={form.targetError}
          >
            <Select
              ref={statementRef}
              value={form.target}
              aria-busy={isLoadingTargets}
              onChange={(event) => setTarget(event.target.value)}
            >
              <option value="">{isLoadingTargets ? 'Loading statements…' : 'Select a statement'}</option>
              {targets.map((target) => (
                <option key={target.value} value={target.value}>
                  {target.label}
                </option>
              ))}
            </Select>
          </Field>
        </div>
        <div className={styles.actions}>
          <Button
            type="submit"
            appearance="primary"
            disabled={form.isSaving || hasNoTargets}
            icon={form.isSaving ? <Spinner size="tiny" /> : undefined}
          >
            {form.isSaving ? 'Saving...' : 'Move charge'}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}
