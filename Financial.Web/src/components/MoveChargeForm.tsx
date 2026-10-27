import { Button, Field, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import type { MoveChargeFormState } from '../hooks/useMoveChargeForm'
import { formatLongMonthYear } from '../utils/formatters'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'

interface MoveChargeFormProps {
  moveForm: MoveChargeFormState
  onCancel: () => void
}

function currentStatementOf(invoiceDate: string | null | undefined): string {
  if (!invoiceDate) return ''
  const [year, month] = invoiceDate.split('-').map(Number)
  return formatLongMonthYear(year, month)
}

export default function MoveChargeForm({ moveForm, onCancel }: MoveChargeFormProps) {
  const styles = useFormPanelStyles()
  const { form, targets, isLoadingTargets, targetsError, retryTargets, setTarget, submit } = moveForm
  if (!form) return null

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
          {form.charge.description} is on the {currentStatementOf(form.charge.invoiceDate)} statement.
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
            hint="Months with no statement yet are marked (new); paid statements are not listed."
            validationState={form.targetError ? 'error' : 'none'}
            validationMessage={form.targetError}
          >
            <Select value={form.target} disabled={isLoadingTargets} onChange={(event) => setTarget(event.target.value)}>
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
          <Button type="submit" appearance="primary" disabled={form.isSaving} icon={form.isSaving ? <Spinner size="tiny" /> : undefined}>
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
