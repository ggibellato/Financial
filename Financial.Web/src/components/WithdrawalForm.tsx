import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import type { ReserveBucketDto } from '../api/types'
import type { WithdrawalFormField } from '../hooks/useReserva'
import { useFieldError } from '../hooks/useFieldError'
import { useFormPanelStyles } from './formPanelStyles'

interface WithdrawalFormProps {
  bucketId: string
  amount: string
  date: string
  description: string
  buckets: ReserveBucketDto[]
  isSubmitting: boolean
  error: string | null
  errorFields: Partial<Record<WithdrawalFormField, string>>
  onFieldChange: (field: WithdrawalFormField, value: string) => void
  onSubmit: () => void
  onCancel: () => void
}

export default function WithdrawalForm({
  bucketId,
  amount,
  date,
  description,
  buckets,
  isSubmitting,
  error,
  errorFields,
  onFieldChange,
  onSubmit,
  onCancel,
}: WithdrawalFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(errorFields)
  const generalError = Object.keys(errorFields).length === 0 ? error : null

  return (
    <div className={styles.panel} data-testid="withdrawal-form-panel">
      <Text as="h2" weight="semibold" size={400}>
        New Withdrawal
      </Text>

      <div className={styles.grid}>
        <Field
          label="Date"
          required
          validationState={fieldError('withdrawalDate') ? 'error' : 'none'}
          validationMessage={fieldError('withdrawalDate')}
        >
          <Input type="date" value={date} onChange={(e) => onFieldChange('withdrawalDate', e.target.value)} />
        </Field>

        <Field
          label="Bucket"
          required
          validationState={fieldError('withdrawalBucketId') ? 'error' : 'none'}
          validationMessage={fieldError('withdrawalBucketId')}
        >
          <Select value={bucketId} onChange={(e) => onFieldChange('withdrawalBucketId', e.target.value)}>
            {buckets.map((bucket) => (
              <option key={bucket.id} value={bucket.id}>
                {bucket.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field
          label="Description"
          required
          validationState={fieldError('withdrawalDescription') ? 'error' : 'none'}
          validationMessage={fieldError('withdrawalDescription')}
        >
          <Input value={description} onChange={(e) => onFieldChange('withdrawalDescription', e.target.value)} />
        </Field>

        <Field
          label="Amount"
          required
          validationState={fieldError('withdrawalAmount') ? 'error' : 'none'}
          validationMessage={fieldError('withdrawalAmount')}
        >
          <Input
            type="number"
            step="0.01"
            min="0"
            value={amount}
            onChange={(e) => onFieldChange('withdrawalAmount', e.target.value)}
          />
        </Field>
      </div>

      <div className={styles.actions}>
        <Button appearance="primary" disabled={isSubmitting} onClick={onSubmit}>
          {isSubmitting ? 'Saving...' : 'Add Withdrawal'}
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
