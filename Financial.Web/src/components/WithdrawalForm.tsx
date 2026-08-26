import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import type { ReserveBucketDto } from '../api/types'
import type { WithdrawalFormField } from '../hooks/useReserva'
import { useFormPanelStyles } from './formPanelStyles'

interface WithdrawalFormProps {
  bucketId: string
  amount: string
  date: string
  description: string
  buckets: ReserveBucketDto[]
  isSubmitting: boolean
  error: string | null
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
  onFieldChange,
  onSubmit,
  onCancel,
}: WithdrawalFormProps) {
  const styles = useFormPanelStyles()

  return (
    <div className={styles.panel} data-testid="withdrawal-form-panel">
      <Text as="h2" weight="semibold" size={400}>
        Record a Withdrawal
      </Text>

      <div className={styles.grid}>
        <Field label="Bucket">
          <Select value={bucketId} onChange={(e) => onFieldChange('withdrawalBucketId', e.target.value)}>
            {buckets.map((bucket) => (
              <option key={bucket.id} value={bucket.id}>
                {bucket.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field label="Amount">
          <Input
            type="number"
            step="0.01"
            min="0"
            value={amount}
            onChange={(e) => onFieldChange('withdrawalAmount', e.target.value)}
          />
        </Field>

        <Field label="Date">
          <Input type="date" value={date} onChange={(e) => onFieldChange('withdrawalDate', e.target.value)} />
        </Field>

        <Field label="Description">
          <Input value={description} onChange={(e) => onFieldChange('withdrawalDescription', e.target.value)} />
        </Field>
      </div>

      <div className={styles.actions}>
        <Button appearance="primary" disabled={isSubmitting} onClick={onSubmit}>
          {isSubmitting ? 'Saving...' : 'Record Withdrawal'}
        </Button>
        <Button appearance="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}
    </div>
  )
}
