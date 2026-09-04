import { Button, Field, Input, MessageBar, MessageBarBody, Text } from '@fluentui/react-components'
import type { IncomeSplitResultDto } from '../api/types'
import type { SplitFormField } from '../hooks/useReserva'
import { useFieldError } from '../hooks/useFieldError'
import { formatN2 } from '../utils/formatters'
import { useFormPanelStyles } from './formPanelStyles'
import './IncomeSplitForm.css'

interface IncomeSplitFormProps {
  date: string
  amount: string
  description: string
  isSubmitting: boolean
  error: string | null
  errorFields: Partial<Record<SplitFormField, string>>
  lastResult: IncomeSplitResultDto | null
  onFieldChange: (field: SplitFormField, value: string) => void
  onSubmit: () => void
  onCancel: () => void
  onDismissResult: () => void
}

export default function IncomeSplitForm({
  date,
  amount,
  description,
  isSubmitting,
  error,
  errorFields,
  lastResult,
  onFieldChange,
  onSubmit,
  onCancel,
  onDismissResult,
}: IncomeSplitFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(errorFields)
  const generalError = Object.keys(errorFields).length === 0 ? error : null

  if (lastResult !== null) {
    return (
      <div className={styles.panel} data-testid="income-split-form-panel">
        <MessageBar intent="success">
          <MessageBarBody>Income Split Posted</MessageBarBody>
        </MessageBar>
        <table className="income-split-form__result-table data-table">
          <colgroup>
            <col />
            <col className="income-split-form__col-value" />
          </colgroup>
          <tbody>
            {lastResult.buckets.map((entry) => (
              <tr key={entry.bucketId}>
                <td>{entry.bucketName}</td>
                <td className="data-table__col--numeric">{formatN2(entry.amount)}</td>
              </tr>
            ))}
            <tr className="income-split-form__totals-row">
              <td>Total</td>
              <td className="data-table__col--numeric">{formatN2(lastResult.total)}</td>
            </tr>
          </tbody>
        </table>
        <div className={styles.actions}>
          <Button appearance="secondary" onClick={onDismissResult}>
            Dismiss
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className={styles.panel} data-testid="income-split-form-panel">
      <Text as="h2" weight="semibold" size={400}>
        New Income Split
      </Text>

      <div className={styles.grid}>
        <Field
          label="Date"
          required
          validationState={fieldError('splitDate') ? 'error' : 'none'}
          validationMessage={fieldError('splitDate')}
        >
          <Input type="date" value={date} onChange={(e) => onFieldChange('splitDate', e.target.value)} />
        </Field>

        <Field
          label="Description"
          required
          validationState={fieldError('splitDescription') ? 'error' : 'none'}
          validationMessage={fieldError('splitDescription')}
        >
          <Input value={description} onChange={(e) => onFieldChange('splitDescription', e.target.value)} />
        </Field>

        <Field
          label="Amount to Split"
          required
          validationState={fieldError('splitAmount') ? 'error' : 'none'}
          validationMessage={fieldError('splitAmount')}
        >
          <Input
            type="number"
            step="0.01"
            value={amount}
            onChange={(e) => onFieldChange('splitAmount', e.target.value)}
          />
        </Field>
      </div>

      <div className={styles.actions}>
        <Button appearance="primary" disabled={isSubmitting} onClick={onSubmit}>
          {isSubmitting ? 'Posting...' : 'Add Income Split'}
        </Button>
        <Button appearance="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>

      {generalError && (
        <MessageBar intent="error">
          <MessageBarBody>{generalError}</MessageBarBody>
        </MessageBar>
      )}
    </div>
  )
}
