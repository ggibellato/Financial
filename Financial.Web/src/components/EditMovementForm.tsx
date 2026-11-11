import { Field, Input, Select, Text } from '@fluentui/react-components'
import { useFieldError } from '../hooks/useFieldError'
import { useFormErrorFocus } from '../hooks/useFormErrorFocus'
import type { EditMovementField } from '../utils/movementEdit'
import ActionErrorAlert from './ActionErrorAlert'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'

interface PotOption {
  id: string
  name: string
  isActive: boolean
}

interface EditMovementFormProps {
  title?: string
  potLabel?: string
  bucketId: string
  amount: string
  date: string
  description: string
  currency: string
  buckets: PotOption[]
  isSaving: boolean
  error: string | null
  errorFields: Partial<Record<EditMovementField, string>>
  onFieldChange: (field: EditMovementField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function EditMovementForm({
  title = 'Edit movement',
  potLabel = 'Bucket',
  bucketId,
  amount,
  date,
  description,
  currency,
  buckets,
  isSaving,
  error,
  errorFields,
  onFieldChange,
  onSave,
  onCancel,
}: EditMovementFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(errorFields)
  const generalError = Object.keys(errorFields).length === 0 ? error : null
  const formRef = useFormErrorFocus(errorFields)

  return (
    <form
      ref={formRef}
      className={styles.panel}
      data-testid="edit-movement-form-panel"
      noValidate
      aria-labelledby="edit-movement-title"
      onSubmit={(event) => {
        event.preventDefault()
        onSave()
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Text as="h2" id="edit-movement-title" weight="semibold" size={400}>
          {title}
        </Text>
        <ActionErrorAlert message={generalError} />

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
            label={potLabel}
            required
            validationState={fieldError('editMovementBucketId') ? 'error' : 'none'}
            validationMessage={fieldError('editMovementBucketId')}
          >
            <Select value={bucketId} onChange={(e) => onFieldChange('editMovementBucketId', e.target.value)}>
              {buckets.map((bucket) => (
                <option key={bucket.id} value={bucket.id}>
                  {bucket.isActive ? bucket.name : `${bucket.name} (inactive)`}
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
            label={currency ? `Amount (${currency})` : 'Amount'}
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
      </fieldset>
      <FormSaveActions saveLabel="Save" isSaving={isSaving} onCancel={onCancel} />
    </form>
  )
}
