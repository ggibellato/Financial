import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import type { ReserveBucketDto } from '../api/types'
import type { EditMovementField } from '../hooks/useReserva'
import { useFieldError } from '../hooks/useFieldError'
import { useFormPanelStyles } from './formPanelStyles'

interface EditMovementFormProps {
  bucketId: string
  amount: string
  date: string
  description: string
  buckets: ReserveBucketDto[]
  isSaving: boolean
  error: string | null
  errorField: EditMovementField | null
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
  errorField,
  onFieldChange,
  onSave,
  onCancel,
}: EditMovementFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(error, errorField)
  const generalError = errorField === null ? error : null

  return (
    <div className={styles.panel} data-testid="edit-movement-form-panel">
      <Text as="h2" weight="semibold" size={400}>
        Edit Movement
      </Text>

      <div className={styles.grid}>
        <Field
          label="Date"
          required
          validationState={fieldError('editMovementDate') ? 'error' : 'none'}
          validationMessage={fieldError('editMovementDate')}
        >
          <Input type="date" value={date} onChange={(e) => onFieldChange('editMovementDate', e.target.value)} />
        </Field>

        <Field
          label="Bucket"
          required
          validationState={fieldError('editMovementBucketId') ? 'error' : 'none'}
          validationMessage={fieldError('editMovementBucketId')}
        >
          <Select value={bucketId} onChange={(e) => onFieldChange('editMovementBucketId', e.target.value)}>
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
          validationState={fieldError('editMovementDescription') ? 'error' : 'none'}
          validationMessage={fieldError('editMovementDescription')}
        >
          <Input value={description} onChange={(e) => onFieldChange('editMovementDescription', e.target.value)} />
        </Field>

        <Field
          label="Amount"
          required
          validationState={fieldError('editMovementAmount') ? 'error' : 'none'}
          validationMessage={fieldError('editMovementAmount')}
        >
          <Input
            type="number"
            step="0.01"
            value={amount}
            onChange={(e) => onFieldChange('editMovementAmount', e.target.value)}
          />
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

      {generalError && (
        <MessageBar intent="error">
          <MessageBarBody>{generalError}</MessageBarBody>
        </MessageBar>
      )}
    </div>
  )
}
