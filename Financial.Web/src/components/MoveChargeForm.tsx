import { useRef } from 'react'
import { Field, Select, Text } from '@fluentui/react-components'
import { moveRecordNoun, type MoveChargeFormState } from '../hooks/useMoveChargeForm'
import { formatN2, formatShortDateUtc, formatStatementMonthLabel } from '../utils/formatters'
import ActionErrorAlert from './ActionErrorAlert'
import ErrorState from './ErrorState'
import FormSaveActions from './FormSaveActions'
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
  const { form, targets, isLoadingTargets, hasNoTargets, targetsError, retryTargets, setTarget, submit } = moveForm
  if (!form) return null
  const { charge } = form
  const noun = moveRecordNoun(charge)

  return (
    <form
      className={styles.panel}
      noValidate
      aria-labelledby="move-charge-title"
      onSubmit={(event) => {
        event.preventDefault()
        if (!submit()) statementRef.current?.focus()
      }}
    >
      <fieldset disabled={form.isSaving} className={styles.savingFieldset} role="presentation">
        <Text as="h2" id="move-charge-title" weight="semibold" size={400}>
          {`Move ${noun} to a later statement`}
        </Text>
        <Text as="p">
          {`${charge.description} (${formatShortDateUtc(charge.chargeDate ?? charge.date)}, ${formatN2(Math.abs(charge.value))}) is on the ${formatStatementMonthLabel(charge.invoiceDate)} statement.`}
        </Text>
        <ActionErrorAlert message={form.error} />
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
      </fieldset>
      <FormSaveActions saveLabel={`Move ${noun}`} isSaving={form.isSaving} isSaveDisabled={hasNoTargets} onCancel={onCancel} />
    </form>
  )
}
