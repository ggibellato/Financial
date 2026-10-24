import { useMemo, useState } from 'react'
import { ApiError } from '../api/apiError'
import { apiClient } from '../api/financialApiClient'
import type { RecurringBillDto } from '../api/types'
import { billExpenseValuesFor, parseBillExpense, type BillExpenseField, type BillExpenseValues } from '../components/billExpenseValues'
import { getCreateFormDate, setStoredDefault } from '../utils/createFormDefaults'
import { getSaveErrorMessage } from '../utils/formatters'
import type { AccountFormSettings, LockedBank } from './formSettings'
import { useAsyncResource } from './useAsyncResource'

export const BILLS_LOAD_ERROR = 'Unable to load the recurring bills'
export const ALREADY_PAID_ERROR = 'This bill is already paid.'

const DATE_KEY = 'payRecurringBill.date'
const UK_AREA = 'UK'
const PAID_STATUS = 'Paid'
const BAD_REQUEST_STATUS = 400

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
  const [defaultDate, setDefaultDate] = useState('')
  const [billId, setBillId] = useState('')
  const [values, setValues] = useState<BillExpenseValues | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const bills = useAsyncResource(() => (isOpen ? apiClient.getMensaisBills() : null), [isOpen], BILLS_LOAD_ERROR)
  const unpaidBills = useMemo(() => unpaidUkBills(bills.data ?? []), [bills.data])
  const parsed = values && parseBillExpense(values)

  const clearSelection = () => {
    setBillId('')
    setValues(null)
  }

  const open = () => {
    setDefaultDate(getCreateFormDate(DATE_KEY, viewedMonth))
    clearSelection()
    setSaveError(null)
    setIsOpen(true)
  }

  const cancel = () => {
    setIsOpen(false)
    clearSelection()
    setSaveError(null)
  }

  const selectBill = (id: string) => {
    const bill = unpaidBills.find((candidate) => candidate.id === id)
    setSaveError(null)
    if (!bill) {
      clearSelection()
      return
    }
    setBillId(id)
    setValues(billExpenseValuesFor(bill, values?.date ?? defaultDate, lockedBank?.id))
  }

  const setField = (field: BillExpenseField, value: string) => {
    setValues((current) => current && { ...current, [field]: value })
    setSaveError(null)
  }

  const showRejection = (message: string) =>
    apiClient
      .getMensaisBills()
      .then((freshBills) => {
        bills.setData(() => freshBills)
        if (freshBills.find((bill) => bill.id === billId)?.status === PAID_STATUS) {
          clearSelection()
          setSaveError(ALREADY_PAID_ERROR)
        } else {
          setSaveError(message)
        }
      })
      .catch(() => setSaveError(message))

  const submit = () => {
    if (!parsed || !lockedBank || isSaving) return
    setIsSaving(true)
    setSaveError(null)
    const expense = {
      date: parsed.date,
      description: parsed.description,
      value: parsed.value,
      categoryId: parsed.categoryId,
      paymentSourceBankId: lockedBank.id,
    }
    void apiClient
      .markMensaisBillPaid(billId, { expense })
      .then(() => {
        setStoredDefault(DATE_KEY, parsed.date)
        cancel()
        onPaid()
      })
      .catch((err: unknown) => {
        const message = getSaveErrorMessage(err, 'Failed to pay the bill')
        return err instanceof ApiError && err.status === BAD_REQUEST_STATUS ? showRejection(message) : setSaveError(message)
      })
      .finally(() => setIsSaving(false))
  }

  return {
    isOpen,
    bills: unpaidBills,
    isLoadingBills: bills.isLoading,
    billsError: bills.error,
    retryBills: bills.retry,
    billId,
    values,
    canSubmit: parsed !== null && !isSaving,
    isSaving,
    isDirty: isOpen && billId !== '',
    saveError,
    open,
    cancel,
    selectBill,
    setField,
    submit,
  }
}
