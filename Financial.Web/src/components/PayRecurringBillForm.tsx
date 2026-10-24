import { Button, Field, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import type { CategoryDto, RecurringBillDto } from '../api/types'
import type { LockedBank } from '../hooks/formSettings'
import { formatCurrency } from '../utils/formatters'
import BillExpenseFields from './BillExpenseFields'
import type { BillExpenseField, BillExpenseValues } from './billExpenseValues'
import ErrorState from './ErrorState'
import { useFormPanelStyles } from './formPanelStyles'

const BILL_CURRENCY = 'GBP'

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
  lockedBank: LockedBank
  canSubmit: boolean
  isSaving: boolean
  saveError: string | null
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
  canSubmit,
  isSaving,
  saveError,
  onRetryBills,
  onSelectBill,
  onFieldChange,
  onSave,
  onCancel,
}: PayRecurringBillFormProps) {
  const styles = useFormPanelStyles()

  const billPicker = () => {
    if (billsError) return <ErrorState message={billsError} onRetry={onRetryBills} />
    if (isLoadingBills) return <Spinner size="tiny" label="Loading bills…" labelPosition="after" />
    if (bills.length === 0 && billId === '') return <Text>No unpaid UK bills.</Text>
    return (
      <div className={styles.grid}>
        <Field label="Bill" required className={styles.spanTwo}>
          <Select value={billId} onChange={(e) => onSelectBill(e.target.value)}>
            <option value="">Select a bill</option>
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

        {values && (
          <div className={styles.grid}>
            <BillExpenseFields
              values={values}
              banks={[]}
              categories={categories}
              lockedBank={lockedBank}
              onFieldChange={onFieldChange}
            />
          </div>
        )}

        <div className={styles.actions}>
          <Button
            type="submit"
            appearance="primary"
            disabled={!canSubmit}
            icon={isSaving ? <Spinner size="tiny" /> : undefined}
          >
            {isSaving ? 'Saving...' : 'Pay recurring bill'}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </fieldset>
    </form>
  )
}
