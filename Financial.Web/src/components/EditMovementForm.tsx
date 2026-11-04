import { useEffect, useRef } from 'react'
import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import type { EditMovementField } from '../hooks/useReserva'
import { useFieldError } from '../hooks/useFieldError'
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
  buckets: PotOption[]
  isSaving: boolean
  error: string | null
  errorFields: Partial<Record<EditMovementField, string>>
  onFieldChange: (field: EditMovementField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function EditMovementForm({
  title = 'Edit Movement',
  potLabel = 'Bucket',
  bucketId,
  amount,
  date,
  description,
  buckets,
  isSaving,
  error,
  errorFields,
  onFieldChange,
  onSave,
  onCancel,
}: EditMovementFormProps) {
  const styles = useFormPanelStyles()
  const formRef = useRef<HTMLFormElement | null>(null)
  const fieldError = useFieldError(errorFields)
  const generalError = Object.keys(errorFields).length === 0 ? error : null

  useEffect(() => {
    formRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()
  }, [errorFields])

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
          <Button type="submit" appearance="primary" disabled={isSaving}>
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
      </fieldset>
    </form>
  )
}
