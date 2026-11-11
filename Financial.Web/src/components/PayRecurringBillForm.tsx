import { useEffect, useRef, useState } from 'react'
import { Button, Field, MessageBar, MessageBarBody, Select, Spinner, Text } from '@fluentui/react-components'
import type { CategoryDto, RecurringBillDto } from '../api/types'
import type { LockedBank } from '../hooks/formSettings'
import { useAlertFocus } from '../hooks/useFormErrorFocus'
import { BILL_CURRENCY } from '../hooks/usePayRecurringBill'
import { formatCurrency } from '../utils/formatters'
import BillExpenseFields from './BillExpenseFields'
import type { BillExpenseErrors, BillExpenseField, BillExpenseValues } from './billExpenseValues'
import ErrorState from './ErrorState'
import FormSaveActions from './FormSaveActions'
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
  const formRef = useRef<HTMLFormElement | null>(null)
  const [submitCount, setSubmitCount] = useState(0)
  const errorRef = useAlertFocus(saveError)
  const canPickBill = !billsError && !isLoadingBills && (bills.length > 0 || billId !== '')

  useEffect(() => {
    if (submitCount > 0) formRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()
  }, [submitCount])

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
      ref={formRef}
      className={styles.panel}
      noValidate
      onSubmit={(event) => {
        event.preventDefault()
        onSave()
        setSubmitCount((count) => count + 1)
      }}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Text as="h2" weight="semibold" size={400}>
          Pay recurring bill
        </Text>
        {saveError && (
          <MessageBar ref={errorRef} role="alert" intent="error" tabIndex={-1}>
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
      </fieldset>
      {canPickBill ? (
        <FormSaveActions saveLabel="Pay recurring bill" isSaving={isSaving} onCancel={onCancel} />
      ) : (
        <div className={styles.actions}>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      )}
    </form>
  )
}
