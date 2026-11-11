import { useId, type FormEvent, type KeyboardEvent, type ReactNode } from 'react'
import { Field, Input, Select, Text } from '@fluentui/react-components'
import { AREAS, BILL_STATUSES, type AddField, type EditField } from '../hooks/useMensais'
import { useFieldError } from '../hooks/useFieldError'
import { useFormErrorFocus } from '../hooks/useFormErrorFocus'
import { generalErrorOf } from '../utils/fieldErrors'
import ActionErrorAlert from './ActionErrorAlert'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'


interface BillFormProps<TField extends string> {
  isSaving: boolean
  error: string | null
  errorFields: Partial<Record<TField, string>>
  onSave: () => void
  onCancel: () => void
}

interface BillFormPanelProps {
  title: string
  saveLabel: string
  isSaving: boolean
  generalError: string | null
  errorFields: object
  onSave: () => void
  onCancel: () => void
  children: ReactNode
}

function BillFormPanel({ title, saveLabel, isSaving, generalError, errorFields, onSave, onCancel, children }: BillFormPanelProps) {
  const styles = useFormPanelStyles()
  const formRef = useFormErrorFocus(errorFields)
  const titleId = useId()

  const submit = (event: FormEvent) => {
    event.preventDefault()
    onSave()
  }

  const cancelOnEscape = (event: KeyboardEvent) => {
    if (event.key !== 'Escape' || event.defaultPrevented || isSaving) return
    event.preventDefault()
    onCancel()
  }

  return (
    <form ref={formRef} className={styles.panel} aria-labelledby={titleId} noValidate onSubmit={submit} onKeyDown={cancelOnEscape}>
      <fieldset disabled={isSaving} className={styles.savingFieldset} role="presentation">
        <Text as="h2" id={titleId} weight="semibold" size={400}>
          {title}
        </Text>
        <ActionErrorAlert message={generalError} />
        <div className={styles.grid}>{children}</div>
      </fieldset>
      <FormSaveActions saveLabel={saveLabel} isSaving={isSaving} onCancel={onCancel} />
    </form>
  )
}

interface AddBillFormProps extends BillFormProps<AddField> {
  values: Record<AddField, string>
  onFieldChange: (field: AddField, value: string) => void
}

export function AddBillForm({ values, isSaving, error, errorFields, onFieldChange, onSave, onCancel }: AddBillFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(errorFields)

  return (
    <BillFormPanel
      title="New bill"
      saveLabel="Add bill"
      isSaving={isSaving}
      generalError={generalErrorOf(error, errorFields)}
      errorFields={errorFields}
      onSave={onSave}
      onCancel={onCancel}
    >
      <Field label="Area">
        <Select value={values.newArea} onChange={(e) => onFieldChange('newArea', e.target.value)}>
          {AREAS.map((area) => (
            <option key={area} value={area}>
              {area}
            </option>
          ))}
        </Select>
      </Field>

      <Field
        label="Description"
        required
        className={styles.spanTwo}
        validationState={fieldError('newDescription') ? 'error' : 'none'}
        validationMessage={fieldError('newDescription')}
      >
        <Input value={values.newDescription} onChange={(e) => onFieldChange('newDescription', e.target.value)} />
      </Field>

      <Field
        label="Due day"
        required
        validationState={fieldError('newDueDay') ? 'error' : 'none'}
        validationMessage={fieldError('newDueDay')}
      >
        <Input type="number" min="1" max="31" value={values.newDueDay} onChange={(e) => onFieldChange('newDueDay', e.target.value)} />
      </Field>

      <Field
        label="Value"
        required
        validationState={fieldError('newValue') ? 'error' : 'none'}
        validationMessage={fieldError('newValue')}
      >
        <Input type="number" step="0.01" value={values.newValue} onChange={(e) => onFieldChange('newValue', e.target.value)} />
      </Field>

      <Field label="Note" className={styles.spanTwo}>
        <Input value={values.newNote} onChange={(e) => onFieldChange('newNote', e.target.value)} />
      </Field>
    </BillFormPanel>
  )
}

interface EditBillFormProps extends BillFormProps<EditField> {
  values: Record<EditField, string>
  onFieldChange: (field: EditField, value: string) => void
}

export function EditBillForm({ values, isSaving, error, errorFields, onFieldChange, onSave, onCancel }: EditBillFormProps) {
  const fieldError = useFieldError(errorFields)

  return (
    <BillFormPanel
      title="Edit bill"
      saveLabel="Save"
      isSaving={isSaving}
      generalError={generalErrorOf(error, errorFields)}
      errorFields={errorFields}
      onSave={onSave}
      onCancel={onCancel}
    >
      <Field
        label="Value"
        required
        validationState={fieldError('editValue') ? 'error' : 'none'}
        validationMessage={fieldError('editValue')}
      >
        <Input type="number" step="0.01" value={values.editValue} onChange={(e) => onFieldChange('editValue', e.target.value)} />
      </Field>

      <Field
        label="Status"
        required
        validationState={fieldError('editStatus') ? 'error' : 'none'}
        validationMessage={fieldError('editStatus')}
      >
        <Select value={values.editStatus} onChange={(e) => onFieldChange('editStatus', e.target.value)}>
          {BILL_STATUSES.map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </Select>
      </Field>
    </BillFormPanel>
  )
}
