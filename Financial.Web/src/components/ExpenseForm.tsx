import { useState } from 'react'
import { Button, Checkbox, Field, Input, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import type { BankDto, CategoryDto, CreditCardDto } from '../api/types'
import type { LockedBank, LockedCard } from '../hooks/formSettings'
import type { PaymentMode } from '../hooks/useExpenseForm'
import { useFieldError } from '../hooks/useFieldError'
import { computeDefaultInvoiceMonth } from '../utils/expenseDefaults'
import { selectableBanks } from '../utils/selectableBanks'
import { useFormPanelStyles } from './formPanelStyles'

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
  roundUpAmount: string
  countsAsTithe: boolean
  paymentMode: PaymentMode
  banks: BankDto[]
  categories: CategoryDto[]
  creditCards: CreditCardDto[]
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
  roundUpAmount,
  countsAsTithe,
  paymentMode,
  banks,
  categories,
  creditCards,
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
  const [openedPaymentSource] = useState(paymentSource)
  const fieldError = useFieldError(saveErrorFields)
  const selectedBank = banks.find((b) => b.id === paymentSource)
  const showRoundUpField = paymentMode === 'bank' && selectedBank?.roundUpEnabled === true
  const selectedCategory = categories.find((c) => c.id === categoryId)
  const showCountsAsTitheField = selectedCategory?.isTithe === true
  const settledCardName = creditCardName || creditCards.find((card) => card.id === creditCardId)?.name || creditCardId
  const invoiceDateDisplay = invoiceDate || computeDefaultInvoiceMonth(date, creditCardId, creditCards)
  const noun = lockedCard ? 'Charge' : 'Expense'

  return (
    <form
      className={styles.panel}
      noValidate
      onSubmit={(event) => {
        event.preventDefault()
        onSave()
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Text as="h2" weight="semibold" size={400}>
          {isEditing ? `Edit ${noun}` : `New ${noun}`}
        </Text>
        {Object.keys(saveErrorFields).length === 0 && saveError && (
          <MessageBar intent="error">
            <MessageBarBody>{saveError}</MessageBarBody>
          </MessageBar>
        )}

        <div className={styles.grid}>
          <Field
            label="Date"
            required={!isSettled}
            hint={isSettled ? SETTLED_FIELD_HINT : undefined}
            validationState={fieldError('date') ? 'error' : 'none'}
            validationMessage={fieldError('date')}
          >
            <Input type="date" value={date} readOnly={isSettled} onChange={(e) => onFieldChange('date', e.target.value)} />
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
                  ? `Paid by ${selectedBank?.name ?? paymentSource} via card ${settledCardName}. Date, value, payment source and invoice month are locked while its card statement is paid — unmark the statement to change them.`
                  : `Paid through ${settledCardName}'s statement. Date, value, card and invoice month are locked while the statement is paid — unmark the statement to change them.`}
              </Text>
            </div>
          ) : lockedBank ? (
            <Field label="Payment Source">
              <Text weight="semibold">{lockedBank.name}</Text>
            </Field>
          ) : lockedCard ? (
            <Field label="Card">
              <Text weight="semibold">{lockedCard.name}</Text>
            </Field>
          ) : paymentMode === 'bank' ? (
            <Field
              label="Payment Source"
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
              required
              validationState={fieldError('creditCardId') ? 'error' : 'none'}
              validationMessage={fieldError('creditCardId')}
            >
              <Select value={creditCardId} onChange={(e) => onFieldChange('creditCardId', e.target.value)}>
                <option value="">Select card…</option>
                {creditCards.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </Select>
            </Field>
          )}

          <Field
            label="Value"
            required={!isSettled}
            hint={isSettled ? SETTLED_FIELD_HINT : undefined}
            validationState={fieldError('value') ? 'error' : 'none'}
            validationMessage={fieldError('value')}
          >
            <Input
              type="number"
              step="0.01"
              value={value}
              readOnly={isSettled}
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
              label="Round-Up"
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
            <Field label={lockedCard ? 'Statement month' : 'Invoice Month'}>
              <Input
                type="month"
                value={invoiceDateDisplay}
                disabled={isSettled}
                onChange={(e) => onFieldChange('invoiceDate', e.target.value)}
              />
            </Field>
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

        <div className={styles.actions}>
          <Button type="submit" appearance="primary" disabled={isSaving} icon={isSaving ? <Spinner size="tiny" /> : undefined}>
            {isSaving ? 'Saving...' : isEditing ? 'Save' : `Add ${noun}`}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}
