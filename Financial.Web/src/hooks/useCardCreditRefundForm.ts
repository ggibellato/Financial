import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import { accountsWith } from '../utils/accountCapabilities'
import { formatN2, getSaveErrorMessage, positiveAmount, todayIsoDate } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'
import { paymentDateError } from './useCardStatements'

const BANKS_LOAD_ERROR = 'Unable to load the banks'
const RECEIVES_TRANSFERS_CAPABILITY = 'CanReceiveTransfers'

export type CardCreditRefundField = 'destinationAccountId' | 'amount' | 'date'

interface CardCreditRefundValues {
  destinationAccountId: string
  amount: string
  date: string
}

interface RefundedCard {
  id: string
  currency: string
}

function initialValues(credit: number): CardCreditRefundValues {
  return { destinationAccountId: '', amount: credit.toFixed(2), date: todayIsoDate() }
}

function amountError(amount: string, credit: number, currency: string): string | undefined {
  const parsed = positiveAmount(amount)
  if (parsed === null) return 'Amount must be greater than zero.'
  return parsed > credit ? `The card's credit is ${formatN2(credit)} ${currency}.` : undefined
}

function validate(values: CardCreditRefundValues, credit: number, currency: string): Partial<Record<CardCreditRefundField, string>> {
  return {
    destinationAccountId: values.destinationAccountId ? undefined : 'Select a bank.',
    amount: amountError(values.amount, credit, currency),
    date: paymentDateError(values.date),
  }
}

export function useCardCreditRefundForm(card: RefundedCard | undefined, credit: number, onRefunded: () => void) {
  const [isOpen, setIsOpen] = useState(false)
  const [values, setValues] = useState<CardCreditRefundValues>(() => initialValues(credit))
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<CardCreditRefundField, string>>>({})
  const accounts = useAsyncResource(() => (isOpen ? apiClient.getAccounts() : null), [isOpen], BANKS_LOAD_ERROR)
  const currency = card?.currency ?? ''
  const banks = accountsWith(accounts.data ?? [], RECEIVES_TRANSFERS_CAPABILITY).filter((bank) => bank.currency === currency)
  const destinationAccountId = banks.some((bank) => bank.id === values.destinationAccountId) ? values.destinationAccountId : ''
  const current = { ...values, destinationAccountId }
  const opened = initialValues(credit)

  const open = () => {
    setValues(opened)
    setSaveError(null)
    setFieldErrors({})
    setIsOpen(true)
  }

  const cancel = () => setIsOpen(false)

  const setField = (field: CardCreditRefundField, value: string) => {
    setValues((previous) => ({ ...previous, [field]: value }))
    setSaveError(null)
    setFieldErrors((previous) => ({ ...previous, [field]: undefined }))
  }

  const submit = (): CardCreditRefundField | null => {
    if (isSaving || !card) return null
    const errors = validate(current, credit, currency)
    const firstInvalid = (['destinationAccountId', 'amount', 'date'] as const).find((field) => errors[field])
    if (firstInvalid) {
      setFieldErrors(errors)
      setSaveError(null)
      return firstInvalid
    }

    setIsSaving(true)
    setSaveError(null)
    apiClient
      .createCardCreditRefund({
        date: current.date,
        sourceAccountId: card.id,
        destinationAccountId: current.destinationAccountId,
        amount: Number(current.amount),
        destinationAmount: null,
        note: null,
      })
      .then(() => {
        cancel()
        onRefunded()
      })
      .catch((err: unknown) => {
        setSaveError(getSaveErrorMessage(err, 'Failed to refund the credit'))
        void accounts.refresh()
      })
      .finally(() => setIsSaving(false))
    return null
  }

  return {
    isOpen,
    banks,
    currency,
    values: current,
    isLoading: accounts.isLoading,
    loadError: accounts.error,
    retry: accounts.retry,
    hasNoBanks: isOpen && !accounts.isLoading && accounts.error === null && accounts.data !== null && banks.length === 0,
    isSaving,
    saveError,
    fieldErrors,
    isDirty: isOpen && (values.destinationAccountId !== '' || values.amount !== opened.amount || values.date !== opened.date),
    open,
    cancel,
    setField,
    submit,
  }
}

export type CardCreditRefundForm = ReturnType<typeof useCardCreditRefundForm>
