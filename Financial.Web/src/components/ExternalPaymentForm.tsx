import { useRef } from 'react'
import { Field, Input, Text } from '@fluentui/react-components'
import type { ExternalPaymentField, ExternalPaymentFormModel } from '../hooks/useExternalPaymentForm'
import { useFieldErrorSummary } from '../hooks/useFieldErrorSummary'
import { ACTION_LABELS } from './accountActions'
import ActionErrorAlert from './ActionErrorAlert'
import FieldErrorSummary from './FieldErrorSummary'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'
import LockedValue from './LockedValue'

interface ExternalPaymentFormProps {
  form: ExternalPaymentFormModel
  onCancel: () => void
}

export default function ExternalPaymentForm({ form, onCancel }: ExternalPaymentFormProps) {
  const styles = useFormPanelStyles()
  const { isEditing, kind, noun, accountName, currency, values, isSaving, saveError, fieldErrors, setField, submit } = form
  const formRef = useRef<HTMLFormElement | null>(null)
  const { summaryRef, summarizedErrors, submitWithSummary } = useFieldErrorSummary(formRef)
  const validation = (field: ExternalPaymentField) => ({
    validationState: fieldErrors[field] ? ('error' as const) : ('none' as const),
    validationMessage: fieldErrors[field],
  })
  const inputProps = (field: ExternalPaymentField) => ({
    name: field,
    value: values[field],
    onChange: (event: { target: { value: string } }) => setField(field, event.target.value),
  })

  return (
    <form
      ref={formRef}
      className={styles.panel}
      noValidate
      aria-labelledby="external-payment-title"
      onSubmit={(event) => {
        event.preventDefault()
        submitWithSummary(submit)
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset} role="presentation">
        <Text as="h2" id="external-payment-title" weight="semibold" size={400}>
          {isEditing ? `Edit ${noun}` : ACTION_LABELS[kind]}
        </Text>
        {summarizedErrors.length > 0 && <FieldErrorSummary ref={summaryRef} errors={summarizedErrors} />}
        <ActionErrorAlert message={saveError} />
        <div className={styles.grid}>
          <Field label="Date" required {...validation('date')}>
            <Input type="date" {...inputProps('date')} />
          </Field>
          <LockedValue label="Account" value={accountName} />
          <Field label="Description" required className={styles.spanTwo} {...validation('description')}>
            <Input maxLength={200} {...inputProps('description')} />
          </Field>
          <Field label={`Amount (${currency})`} required {...validation('amount')}>
            <Input type="number" step="0.01" min="0.01" {...inputProps('amount')} />
          </Field>
        </div>
      </fieldset>
      <FormSaveActions saveLabel={isEditing ? 'Save' : `Add ${noun}`} isSaving={isSaving} onCancel={onCancel} />
    </form>
  )
}
