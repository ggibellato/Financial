import { Field, Input, makeStyles, mergeClasses, MessageBar, MessageBarBody, Radio, RadioGroup, Text, tokens } from '@fluentui/react-components'
import type { SettlementEntryField, SettlementEntryFormModel } from '../hooks/useSettlementEntryForm'
import ActionErrorAlert from './ActionErrorAlert'
import ConfirmDialog from './ConfirmDialog'
import { todayIsoDate } from '../utils/formatters'
import { PAID_BY_OTHER, PAID_BY_YOU } from '../utils/paidBy'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'
import LockedValue from './LockedValue'

interface SettlementEntryFormProps {
  form: SettlementEntryFormModel
  onCancel: () => void
}

const useEntryFormStyles = makeStyles({
  startsRow: { gridColumnStart: 1 },
  impliedRate: { minHeight: tokens.lineHeightBase300 },
})

export default function SettlementEntryForm({ form, onCancel }: SettlementEntryFormProps) {
  const styles = useFormPanelStyles()
  const entryStyles = useEntryFormStyles()
  const { isEditing, isSettling, values, currencies, otherParticipant, asksCurrency, convertedCurrency, isSaving, saveError, fieldErrors, warning, impliedRate, confirmation, setField, submit, confirmSettle, cancelConfirmation } = form
  const title = isSettling ? 'Settle' : isEditing ? 'Edit entry' : 'New entry'
  const submitLabel = isSettling ? 'Save settlement' : isEditing ? 'Save' : 'Add entry'
  const validation = (field: SettlementEntryField) => ({
    validationState: fieldErrors[field] ? ('error' as const) : ('none' as const),
    validationMessage: fieldErrors[field],
  })
  const inputProps = (field: SettlementEntryField) => ({
    name: field,
    value: values[field],
    onChange: (event: { target: { value: string } }) => setField(field, event.target.value),
  })
  const currencyChoice = asksCurrency && (
    <Field label="Currency" required {...validation('currency')}>
      <RadioGroup name="currency" layout="horizontal" className={styles.radioRow} value={values.currency} onChange={(_, data) => setField('currency', data.value)}>
        {currencies.map((currency) => (
          <Radio key={currency} value={currency} label={currency} />
        ))}
      </RadioGroup>
    </Field>
  )

  return (
    <>
      <form
        className={styles.panel}
        noValidate
        aria-labelledby="settlement-entry-title"
        onSubmit={(event) => {
          event.preventDefault()
          const firstInvalid = submit()
          if (firstInvalid) event.currentTarget.querySelector<HTMLElement>(`[name="${firstInvalid}"]`)?.focus()
        }}
      >
        <fieldset disabled={isSaving} className={styles.savingFieldset}>
          <Text as="h2" id="settlement-entry-title" weight="semibold" size={400}>
            {title}
          </Text>
          <ActionErrorAlert message={saveError} />
          {warning && (
            <MessageBar role="status" intent="warning">
              <MessageBarBody>{warning}</MessageBarBody>
            </MessageBar>
          )}
          <div className={styles.grid}>
            {isSettling && currencyChoice}
            <Field label="Date" required {...validation('date')}>
              <Input type="date" max={todayIsoDate()} {...inputProps('date')} />
            </Field>
            <Field label="Paid by" required {...validation('paidBy')}>
              <RadioGroup name="paidBy" layout="horizontal" className={styles.radioRow} value={values.paidBy} onChange={(_, data) => setField('paidBy', data.value)}>
                <Radio value={PAID_BY_YOU} label={PAID_BY_YOU} />
                <Radio value={PAID_BY_OTHER} label={otherParticipant} />
              </RadioGroup>
            </Field>
            {isEditing && (
              <LockedValue label="Currency" value={values.currency} />
            )}
            {!isSettling && currencyChoice}
            <Field label="Description" required className={mergeClasses(styles.spanTwo, entryStyles.startsRow)} {...validation('description')}>
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
            {isSettling && convertedCurrency && (
              <Text aria-live="polite" className={mergeClasses(styles.spanAll, entryStyles.impliedRate)}>
                {impliedRate}
              </Text>
            )}
            <Field label="Note" className={styles.spanAll}>
              <Input {...inputProps('note')} />
            </Field>
          </div>
        </fieldset>
        <FormSaveActions saveLabel={submitLabel} isSaving={isSaving} onCancel={onCancel} />
      </form>
      <ConfirmDialog
        open={confirmation !== null}
        title="Save this settlement?"
        message={confirmation ?? ''}
        confirmLabel="Save settlement"
        cancelLabel="Cancel"
        onConfirm={confirmSettle}
        onCancel={cancelConfirmation}
      />
    </>
  )
}
