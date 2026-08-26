import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import type { ReserveBucketDto } from '../api/types'
import type { EditMovementField } from '../hooks/useReserva'
import { useFormPanelStyles } from './formPanelStyles'

interface EditMovementFormProps {
  bucketId: string
  amount: string
  date: string
  description: string
  buckets: ReserveBucketDto[]
  isSaving: boolean
  error: string | null
  onFieldChange: (field: EditMovementField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function EditMovementForm({
  bucketId,
  amount,
  date,
  description,
  buckets,
  isSaving,
  error,
  onFieldChange,
  onSave,
  onCancel,
}: EditMovementFormProps) {
  const styles = useFormPanelStyles()

  return (
    <div className={styles.panel} data-testid="edit-movement-form-panel">
      <Text as="h2" weight="semibold" size={400}>
        Edit Movement
      </Text>

      <div className={styles.grid}>
        <Field label="Bucket">
          <Select value={bucketId} onChange={(e) => onFieldChange('editMovementBucketId', e.target.value)}>
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
            value={amount}
            onChange={(e) => onFieldChange('editMovementAmount', e.target.value)}
          />
        </Field>

        <Field label="Date">
          <Input type="date" value={date} onChange={(e) => onFieldChange('editMovementDate', e.target.value)} />
        </Field>

        <Field label="Description">
          <Input value={description} onChange={(e) => onFieldChange('editMovementDescription', e.target.value)} />
        </Field>
      </div>

      <div className={styles.actions}>
        <Button appearance="primary" disabled={isSaving} onClick={onSave}>
          {isSaving ? 'Saving...' : 'Save'}
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
