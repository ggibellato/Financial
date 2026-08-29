import { Button, Checkbox, Field, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import type { BankDto, CategoryDto, CreditCardDto } from '../api/types'
import type { PaymentMode } from '../hooks/useExpenseForm'
import { useFieldError } from '../hooks/useFieldError'
import { useFormPanelStyles } from './formPanelStyles'

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
  saveError: string | null
  saveErrorField: ExpenseFormField | null
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
  saveError,
  saveErrorField,
  onFieldChange,
  onSave,
  onCancel,
}: ExpenseFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(saveError, saveErrorField)
  const selectedBank = banks.find((b) => b.id === paymentSource)
  const showRoundUpField = paymentMode === 'bank' && selectedBank?.roundUpEnabled === true
  const selectedCategory = categories.find((c) => c.id === categoryId)
  const showCountsAsTitheField = selectedCategory?.isTithe === true
  const invoiceDateDisplay = invoiceDate || (date ? date.slice(0, 7) : '')

  return (
    <div className={styles.panel}>
      <Text as="h2" weight="semibold" size={400}>
        {isEditing ? 'Edit Expense' : 'New Expense'}
      </Text>

      <div className={styles.grid}>
        <Field
          label="Date"
          required
          validationState={fieldError('date') ? 'error' : 'none'}
          validationMessage={fieldError('date')}
        >
          <Input type="date" value={date} onChange={(e) => onFieldChange('date', e.target.value)} />
        </Field>

        <Field
          label="Description"
          required
          className={styles.spanTwo}
          validationState={fieldError('description') ? 'error' : 'none'}
          validationMessage={fieldError('description')}
        >
          <Input value={description} onChange={(e) => onFieldChange('description', e.target.value)} />
        </Field>

        {isSettled ? (
          <div className={styles.spanTwo}>
            <Text as="p" size={200}>
              Paid by {selectedBank?.name ?? paymentSource} via card {creditCardName || creditCardId}. Settled via its
              card statement — unmark the statement paid to change these fields.
            </Text>
          </div>
        ) : paymentMode === 'bank' ? (
          <Field
            label="Payment Source"
            required
            validationState={fieldError('paymentSource') ? 'error' : 'none'}
            validationMessage={fieldError('paymentSource')}
          >
            <Select value={paymentSource} onChange={(e) => onFieldChange('paymentSource', e.target.value)}>
              {banks.map((b) => (
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
          required
          validationState={fieldError('value') ? 'error' : 'none'}
          validationMessage={fieldError('value')}
        >
          <Input
            type="number"
            step="0.01"
            value={value}
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
          <Field label="Invoice Month">
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
        <Button appearance="primary" disabled={isSaving} onClick={onSave}>
          {isSaving ? 'Saving...' : isEditing ? 'Save' : 'Add Expense'}
        </Button>
        <Button appearance="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>

      {saveError && (
        <MessageBar intent="error">
          <MessageBarBody>{saveError}</MessageBarBody>
        </MessageBar>
      )}
    </div>
  )
}
