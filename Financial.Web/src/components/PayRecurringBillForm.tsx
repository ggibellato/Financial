import { Button, Field, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import type { CategoryDto, RecurringBillDto } from '../api/types'
import type { LockedBank } from '../hooks/formSettings'
import { BILL_CURRENCY } from '../hooks/usePayRecurringBill'
import { formatCurrency } from '../utils/formatters'
import BillExpenseFields from './BillExpenseFields'
import type { BillExpenseErrors, BillExpenseField, BillExpenseValues } from './billExpenseValues'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'

function billLabel(bill: RecurringBillDto): string {
  return `Due ${bill.dueDay} · ${bill.description} · ${formatCurrency(bill.value, BILL_CURRENCY)}`
}

interface PayRecurringBillFormProps {
  bills: RecurringBillDto[]
  isLoadingBills: boolean
  billsError: string | null
  billId: string
  values: BillExpenseValues | null
  categories: CategoryDto[]
  lockedBank: LockedBank & { currency: string }
  isSaving: boolean
  saveError: string | null
  fieldErrors: BillExpenseErrors
  billError: string | null
  onRetryBills: () => void
  onSelectBill: (billId: string) => void
  onFieldChange: (field: BillExpenseField, value: string) => void
  onSave: () => void
  onCancel: () => void
}

export default function PayRecurringBillForm({
  bills,
  isLoadingBills,
  billsError,
  billId,
  values,
  categories,
  lockedBank,
  isSaving,
  saveError,
  fieldErrors,
  billError,
  onRetryBills,
  onSelectBill,
  onFieldChange,
  onSave,
  onCancel,
}: PayRecurringBillFormProps) {
  const styles = useFormPanelStyles()
  const canPickBill = !billsError && !isLoadingBills && (bills.length > 0 || billId !== '')

  const billPicker = () => {
    if (billsError) return <ErrorState message={billsError} onRetry={onRetryBills} />
    if (isLoadingBills) return <Spinner size="tiny" label="Loading bills…" labelPosition="after" />
    if (!canPickBill) return <Text>No unpaid UK bills. Bills are managed on Mensais.</Text>
    return (
      <div className={styles.grid}>
        <Field
          label="Bill"
          required
          className={styles.spanTwo}
          validationState={billError ? 'error' : 'none'}
          validationMessage={billError}
        >
          <Select value={billId} autoFocus onChange={(e) => onSelectBill(e.target.value)}>
            {billId === '' && <option value="">Select a bill</option>}
            {bills.map((bill) => (
              <option key={bill.id} value={bill.id}>
                {billLabel(bill)}
              </option>
            ))}
          </Select>
        </Field>
      </div>
    )
  }

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
          Pay recurring bill
        </Text>
        {saveError && (
          <MessageBar intent="error">
            <MessageBarBody>{saveError}</MessageBarBody>
          </MessageBar>
        )}

        {billPicker()}
        {lockedBank.currency !== BILL_CURRENCY && (
          <MessageBar intent="warning">
            <MessageBarBody>
              {`Recurring bills are in ${BILL_CURRENCY}; the expense is recorded in ${lockedBank.name}'s ${lockedBank.currency}.`}
            </MessageBarBody>
          </MessageBar>
        )}

        {values && (
          <div className={styles.grid}>
            <BillExpenseFields
              values={values}
              categories={categories}
              errors={fieldErrors}
              lockedBank={lockedBank}
              onFieldChange={onFieldChange}
            />
          </div>
        )}

        <div className={styles.actions}>
          {canPickBill && (
            <Button type="submit" appearance="primary" disabled={isSaving} icon={isSaving ? <Spinner size="tiny" /> : undefined}>
              {isSaving ? 'Saving...' : 'Pay recurring bill'}
            </Button>
          )}
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}
