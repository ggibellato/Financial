import { useId, useState } from 'react'
import { Checkbox, Field, Input, Select, Text } from '@fluentui/react-components'
import type { BankOption, CategoryDto } from '../api/types'
import type { LockedBank, LockedCard } from '../hooks/formSettings'
import type { PaymentMode } from '../hooks/useExpenseForm'
import { useFieldError } from '../hooks/useFieldError'
import { useFormErrorFocus } from '../hooks/useFormErrorFocus'
import { computeDefaultInvoiceMonth } from '../utils/expenseDefaults'
import { formatStatementMonthLabel, todayIsoDate } from '../utils/formatters'
import { selectableBanks } from '../utils/selectableBanks'
import ActionErrorAlert from './ActionErrorAlert'
import FormSaveActions from './FormSaveActions'
import { useFormPanelStyles } from './formPanelStyles'
import { useVisuallyHiddenStyles } from '../styles/visuallyHidden'
import LockedValue from './LockedValue'

const SETTLED_FIELD_HINT = 'Locked: its card statement is paid'

export type ExpenseFormField =
  | 'date'
  | 'description'
  | 'value'
  | 'categoryId'
  | 'paymentSource'
  | 'creditCardId'
  | 'invoiceDate'
  | 'roundUpAmount'
  | 'countsAsTithe'

interface ExpenseFormProps {
  isEditing: boolean
  date: string
  description: string
  value: string
  categoryId: string
  paymentSource: string
  creditCardId: string
  creditCardName: string
  invoiceDate: string
  invoiceMonthMovedFrom: string | null
  roundUpAmount: string
  countsAsTithe: boolean
  paymentMode: PaymentMode
  banks: BankOption[]
  categories: CategoryDto[]
  isRefund?: boolean
  isSettled: boolean
  isSaving: boolean
  lockedBank?: LockedBank
  lockedCard?: LockedCard
  saveError: string | null
  saveErrorFields: Partial<Record<ExpenseFormField, string>>
  onFieldChange: (field: ExpenseFormField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function ExpenseForm({
  isEditing,
  date,
  description,
  value,
  categoryId,
  paymentSource,
  creditCardId,
  creditCardName,
  invoiceDate,
  invoiceMonthMovedFrom,
  roundUpAmount,
  countsAsTithe,
  paymentMode,
  banks,
  categories,
  isRefund = false,
  isSettled,
  isSaving,
  lockedBank,
  lockedCard,
  saveError,
  saveErrorFields,
  onFieldChange,
  onSave,
  onCancel,
}: ExpenseFormProps) {
  const styles = useFormPanelStyles()
  const hidden = useVisuallyHiddenStyles()
  const [openedPaymentSource] = useState(paymentSource)
  const fieldError = useFieldError(saveErrorFields)
  const hasFieldErrors = Object.keys(saveErrorFields).length > 0
  const formRef = useFormErrorFocus(saveErrorFields)
  const selectedBank = banks.find((b) => b.id === paymentSource)
  const showRoundUpField = paymentMode === 'bank' && selectedBank?.roundUpEnabled === true
  const selectedCategory = categories.find((c) => c.id === categoryId)
  const showCountsAsTitheField = selectedCategory?.isTithe === true
  const cardName = lockedCard?.name || creditCardName || creditCardId
  const invoiceDateDisplay = invoiceDate || computeDefaultInvoiceMonth(date)
  const invoiceMonthFieldLabel = lockedCard ? 'Statement month' : 'Invoice month'
  const invoiceMonthMovedHint = invoiceMonthMovedFrom
    ? `${invoiceMonthFieldLabel} moved to ${formatStatementMonthLabel(invoiceDateDisplay)} with the date (was ${formatStatementMonthLabel(invoiceMonthMovedFrom)}).`
    : undefined
  const noun = isRefund ? 'refund' : lockedCard ? 'charge' : 'expense'
  const titleId = useId()

  return (
    <form
      ref={formRef}
      aria-labelledby={titleId}
      className={styles.panel}
      noValidate
      onSubmit={(event) => {
        event.preventDefault()
        onSave()
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset} role="presentation">
        <Text as="h2" id={titleId} weight="semibold" size={400}>
          {isEditing ? `Edit ${noun}` : `New ${noun}`}
        </Text>
        <ActionErrorAlert message={hasFieldErrors ? null : saveError} />

        <div className={styles.grid}>
          <Field
            label="Date"
            required={!isSettled}
            hint={isSettled ? SETTLED_FIELD_HINT : undefined}
            validationState={fieldError('date') ? 'error' : 'none'}
            validationMessage={fieldError('date')}
          >
            <Input
              type="date"
              value={date}
              max={isRefund ? todayIsoDate() : undefined}
              readOnly={isSettled}
              appearance={isSettled ? 'filled-darker' : undefined}
              onChange={(e) => onFieldChange('date', e.target.value)}
            />
          </Field>

          <Field
            label="Description"
            required
            className={styles.spanTwo}
            validationState={fieldError('description') ? 'error' : 'none'}
            validationMessage={fieldError('description')}
          >
            <Input name="description" value={description} onChange={(e) => onFieldChange('description', e.target.value)} />
          </Field>

          {isSettled ? (
            <div className={styles.spanTwo}>
              <Text as="p" size={200}>
                {paymentSource
                  ? `Paid by ${selectedBank?.name ?? paymentSource} via card ${cardName}. Date, value, payment source and invoice month are locked while its card statement is paid — unmark the statement to change them.`
                  : `Paid through ${cardName}'s statement. Date, value, card and invoice month are locked while the statement is paid — unmark the statement to change them.`}
              </Text>
            </div>
          ) : lockedBank ? (
            <LockedValue label="Payment source" value={lockedBank.name} />
          ) : paymentMode === 'bank' && !lockedCard ? (
            <Field
              label="Payment source"
              required
              validationState={fieldError('paymentSource') ? 'error' : 'none'}
              validationMessage={fieldError('paymentSource')}
            >
              <Select value={paymentSource} onChange={(e) => onFieldChange('paymentSource', e.target.value)}>
                {selectableBanks(banks, paymentSource, openedPaymentSource).map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
              </Select>
            </Field>
          ) : (
            <Field
              label="Card"
              validationState={fieldError('creditCardId') ? 'error' : 'none'}
              validationMessage={fieldError('creditCardId')}
            >
              <Text weight="semibold">{cardName}</Text>
            </Field>
          )}

          <Field
            label={isRefund ? 'Amount' : 'Value'}
            required={!isSettled}
            hint={isSettled ? SETTLED_FIELD_HINT : undefined}
            validationState={fieldError('value') ? 'error' : 'none'}
            validationMessage={fieldError('value')}
          >
            <Input
              type="number"
              step="0.01"
              min={isRefund ? 0.01 : undefined}
              value={value}
              readOnly={isSettled}
              appearance={isSettled ? 'filled-darker' : undefined}
              onChange={(e) => onFieldChange('value', e.target.value)}
            />
          </Field>

          <Field label="Category" required>
            <Select value={categoryId} onChange={(e) => onFieldChange('categoryId', e.target.value)}>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </Select>
          </Field>

          {!isSettled && paymentMode === 'bank' && showRoundUpField && (
            <Field
              label="Round-up"
              validationState={fieldError('roundUpAmount') ? 'error' : 'none'}
              validationMessage={fieldError('roundUpAmount')}
            >
              <Input
                type="number"
                step="0.01"
                value={roundUpAmount}
                onChange={(e) => onFieldChange('roundUpAmount', e.target.value)}
              />
            </Field>
          )}

          {(isSettled || paymentMode === 'card') && (
            <>
              <Field label={invoiceMonthFieldLabel} hint={invoiceMonthMovedHint}>
                <Input
                  type="month"
                  value={invoiceDateDisplay}
                  readOnly={isSettled}
                  appearance={isSettled ? 'filled-darker' : undefined}
                  onChange={(e) => onFieldChange('invoiceDate', e.target.value)}
                />
              </Field>
              <span role="status" className={hidden.visuallyHidden}>
                {invoiceMonthMovedHint}
              </span>
            </>
          )}

          {showCountsAsTitheField && (
            <div className={styles.spanTwo}>
              <Checkbox
                label="Counts toward tithe"
                checked={countsAsTithe}
                onChange={(_, data) => onFieldChange('countsAsTithe', data.checked ? 'true' : 'false')}
              />
            </div>
          )}
        </div>
      </fieldset>
      <FormSaveActions saveLabel={isEditing ? 'Save' : `Add ${noun}`} isSaving={isSaving} onCancel={onCancel} />
    </form>
  )
}
