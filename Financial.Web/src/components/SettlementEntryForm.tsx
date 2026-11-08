import { useEffect, useRef } from 'react'
import { Button, Field, Input, MessageBar, MessageBarBody, Radio, RadioGroup, Spinner, Text } from '@fluentui/react-components'
import type { SettlementEntryField, SettlementEntryFormModel } from '../hooks/useSettlementEntryForm'
import { todayIsoDate } from '../utils/formatters'
import { PAID_BY_OTHER, PAID_BY_YOU } from './accountActions'
import { useFormPanelStyles } from './formPanelStyles'

interface SettlementEntryFormProps {
  form: SettlementEntryFormModel
  onCancel: () => void
}

export default function SettlementEntryForm({ form, onCancel }: SettlementEntryFormProps) {
  const styles = useFormPanelStyles()
  const formRef = useRef<HTMLFormElement | null>(null)
  const submitRef = useRef<HTMLButtonElement | null>(null)
  const { isEditing, values, currencies, otherParticipant, asksCurrency, convertedCurrency, isSaving, saveError, fieldErrors, warning, setField, submit } = form
  const title = isEditing ? 'Edit entry' : 'Add entry'
  const validation = (field: SettlementEntryField) => ({
    validationState: fieldErrors[field] ? ('error' as const) : ('none' as const),
    validationMessage: fieldErrors[field],
  })
  const inputProps = (field: SettlementEntryField) => ({
    name: field,
    value: values[field],
    onChange: (event: { target: { value: string } }) => setField(field, event.target.value),
  })

  useEffect(() => {
    if (saveError && !isSaving) submitRef.current?.focus()
  }, [saveError, isSaving])

  return (
    <form
      ref={formRef}
      className={styles.panel}
      noValidate
      aria-labelledby="settlement-entry-title"
      onSubmit={(event) => {
        event.preventDefault()
        const firstInvalid = submit()
        if (firstInvalid) formRef.current?.querySelector<HTMLElement>(`[name="${firstInvalid}"]`)?.focus()
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Text as="h2" id="settlement-entry-title" weight="semibold" size={400}>
          {title}
        </Text>
        {saveError && (
          <MessageBar role="alert" intent="error">
            <MessageBarBody>{saveError}</MessageBarBody>
          </MessageBar>
        )}
        {warning && (
          <MessageBar role="status" intent="warning">
            <MessageBarBody>{warning}</MessageBarBody>
          </MessageBar>
        )}
        <div className={styles.grid}>
          <Field label="Date" required {...validation('date')}>
            <Input type="date" max={todayIsoDate()} {...inputProps('date')} />
          </Field>
          <Field label="Paid by" required {...validation('paidBy')}>
            <RadioGroup name="paidBy" layout="horizontal" className={styles.radioRow} value={values.paidBy} onChange={(_, data) => setField('paidBy', data.value)}>
              <Radio value={PAID_BY_YOU} label="You" />
              <Radio value={PAID_BY_OTHER} label={otherParticipant} />
            </RadioGroup>
          </Field>
          {isEditing && (
            <Field label="Currency">
              <Text weight="semibold">{values.currency}</Text>
            </Field>
          )}
          {asksCurrency && (
            <Field label="Currency" required {...validation('currency')}>
              <RadioGroup name="currency" layout="horizontal" className={styles.radioRow} value={values.currency} onChange={(_, data) => setField('currency', data.value)}>
                {currencies.map((currency) => (
                  <Radio key={currency} value={currency} label={currency} />
                ))}
              </RadioGroup>
            </Field>
          )}
          <Field label="Description" required className={styles.spanTwo} {...validation('description')}>
            <Input {...inputProps('description')} />
          </Field>
          <Field label={`Amount (${values.currency})`} required {...validation('amount')}>
            <Input type="number" step="0.01" {...inputProps('amount')} />
          </Field>
          {convertedCurrency && (
            <Field label={`${convertedCurrency} value`} hint={`Leave empty when there is no ${convertedCurrency} value.`} {...validation('convertedValue')}>
              <Input type="number" step="0.01" {...inputProps('convertedValue')} />
            </Field>
          )}
          <Field label="Note" className={styles.spanAll}>
            <Input {...inputProps('note')} />
          </Field>
        </div>
        <div className={styles.actions}>
          <Button ref={submitRef} type="submit" appearance="primary" disabled={isSaving} icon={isSaving ? <Spinner size="tiny" /> : undefined}>
            {isSaving ? 'Saving...' : isEditing ? 'Save' : title}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}
