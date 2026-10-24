import { useMemo, useState } from 'react'
import { ApiError } from '../api/apiError'
import { apiClient } from '../api/financialApiClient'
import type { RecurringBillDto } from '../api/types'
import {
  billExpenseValuesFor,
  mapBillExpenseErrorToField,
  parseBillExpense,
  toBillPaymentExpense,
  validateBillExpense,
  type BillExpenseErrors,
  type BillExpenseField,
  type BillExpenseValues,
} from '../components/billExpenseValues'
import { getCreateFormDate, setStoredDefault } from '../utils/createFormDefaults'
import { getSaveErrorMessage } from '../utils/formatters'
import type { AccountFormSettings, LockedBank } from './formSettings'
import { useAsyncResource } from './useAsyncResource'

export const BILLS_LOAD_ERROR = 'Unable to load the recurring bills'
export const ALREADY_PAID_ERROR = 'This bill is already paid.'
export const CHOOSE_BILL_ERROR = 'Choose a bill.'
export const BILL_CURRENCY = 'GBP'

const DATE_KEY = 'payRecurringBill.date'
const UK_AREA = 'UK'
const PAID_STATUS = 'Paid'
const BAD_REQUEST_STATUS = 400

interface BillSelection {
  billId: string
  values: BillExpenseValues
}

export function unpaidUkBills(bills: readonly RecurringBillDto[]): RecurringBillDto[] {
  return bills
    .filter((bill) => bill.area === UK_AREA && bill.status !== PAID_STATUS)
    .sort((a, b) => a.dueDay - b.dueDay || a.description.localeCompare(b.description))
}

export function usePayRecurringBill(
  lockedBank: LockedBank | undefined,
  onPaid: () => void,
  { viewedMonth }: AccountFormSettings = {},
) {
  const [isOpen, setIsOpen] = useState(false)
  const [selection, setSelection] = useState<BillSelection | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<BillExpenseErrors>({})
  const [billError, setBillError] = useState<string | null>(null)
  const bills = useAsyncResource(() => (isOpen ? apiClient.getMensaisBills() : null), [isOpen], BILLS_LOAD_ERROR)
  const unpaidBills = useMemo(() => unpaidUkBills(bills.data ?? []), [bills.data])

  const clearErrors = () => {
    setSaveError(null)
    setFieldErrors({})
    setBillError(null)
  }

  const open = () => {
    setSelection(null)
    clearErrors()
    setIsOpen(true)
  }

  const cancel = () => {
    setIsOpen(false)
    setSelection(null)
    clearErrors()
  }

  const selectBill = (id: string) => {
    const bill = unpaidBills.find((candidate) => candidate.id === id)
    if (!bill) return
    clearErrors()
    setSelection((current) => ({
      billId: id,
      values: billExpenseValuesFor(
        bill,
        current?.values.date ?? getCreateFormDate(DATE_KEY, viewedMonth),
        lockedBank?.id,
        current?.values.categoryId,
      ),
    }))
  }

  const setField = (field: BillExpenseField, value: string) => {
    setSelection((current) => current && { ...current, values: { ...current.values, [field]: value } })
    setSaveError(null)
    setFieldErrors((current) => ({ ...current, [field]: undefined }))
  }

  const refreshAfterRejection = (billId: string, message: string) =>
    apiClient
      .getMensaisBills()
      .then((freshBills) => {
        bills.setData(() => freshBills)
        if (freshBills.find((bill) => bill.id === billId)?.status === PAID_STATUS) {
          setSelection(null)
          setSaveError(ALREADY_PAID_ERROR)
          return
        }
        const field = mapBillExpenseErrorToField(message)
        if (field) setFieldErrors({ [field]: message })
        else setSaveError(message)
      })
      .catch(() => setSaveError(message))

  const submit = () => {
    if (isSaving) return
    if (!selection) {
      setBillError(CHOOSE_BILL_ERROR)
      return
    }
    const errors = validateBillExpense(selection.values)
    const expense = parseBillExpense(selection.values)
    if (Object.keys(errors).length > 0 || !expense) {
      setFieldErrors(errors)
      return
    }
    const { billId } = selection
    setIsSaving(true)
    clearErrors()
    void apiClient
      .markMensaisBillPaid(billId, { expense: toBillPaymentExpense(expense) })
      .then(() => {
        setStoredDefault(DATE_KEY, expense.date)
        cancel()
        onPaid()
      })
      .catch((err: unknown) => {
        const message = getSaveErrorMessage(err, 'Failed to pay the bill')
        return err instanceof ApiError && err.status === BAD_REQUEST_STATUS
          ? refreshAfterRejection(billId, message)
          : setSaveError(message)
      })
      .finally(() => setIsSaving(false))
  }

  return {
    isOpen,
    bills: unpaidBills,
    isLoadingBills: bills.isLoading || (isOpen && bills.data === null && bills.error === null),
    billsError: bills.error,
    retryBills: bills.retry,
    billId: selection?.billId ?? '',
    values: selection?.values ?? null,
    isSaving,
    isDirty: selection !== null,
    saveError,
    fieldErrors,
    billError,
    open,
    cancel,
    selectBill,
    setField,
    submit,
  }
}
