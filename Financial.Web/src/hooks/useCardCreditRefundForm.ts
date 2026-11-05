import { useMemo, useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountDto } from '../api/types'
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
  name: string
  currency: string
}

const NO_ACCOUNTS: AccountDto[] = []

function formatAmountInput(credit: number): string {
  return credit.toFixed(2)
}

function initialValues(credit: number): CardCreditRefundValues {
  return { destinationAccountId: '', amount: formatAmountInput(credit), date: todayIsoDate() }
}

export function refundableBanksOf(accounts: readonly AccountDto[], currency: string | undefined): AccountDto[] {
  return accounts.filter(
    (account) => account.isActive && account.currency === currency && account.capabilities.includes(RECEIVES_TRANSFERS_CAPABILITY),
  )
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
  const currency = card?.currency
  const banks = useMemo(() => refundableBanksOf(accounts.data ?? NO_ACCOUNTS, currency), [accounts.data, currency])
  const destinationAccountId = banks.some((bank) => bank.id === values.destinationAccountId) ? values.destinationAccountId : ''
  const current = { ...values, destinationAccountId }

  const reset = () => {
    setValues(initialValues(credit))
    setSaveError(null)
    setFieldErrors({})
  }

  const open = () => {
    reset()
    setIsOpen(true)
  }

  const cancel = () => {
    setIsOpen(false)
    reset()
  }

  const setField = (field: CardCreditRefundField, value: string) => {
    setValues((previous) => ({ ...previous, [field]: value }))
    setSaveError(null)
    setFieldErrors((previous) => ({ ...previous, [field]: undefined }))
  }

  const submit = (): CardCreditRefundField | null => {
    if (isSaving || !card) return null
    const errors = validate(current, credit, card.currency)
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
        setIsOpen(false)
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
    currency: currency ?? '',
    values: current,
    isLoading: accounts.isLoading,
    loadError: accounts.error,
    retry: accounts.retry,
    hasNoBanks: isOpen && !accounts.isLoading && accounts.error === null && accounts.data !== null && banks.length === 0,
    isSaving,
    saveError,
    fieldErrors,
    isDirty: isOpen && (values.destinationAccountId !== '' || values.amount !== formatAmountInput(credit) || values.date !== todayIsoDate()),
    open,
    cancel,
    setField,
    submit,
  }
}

export type CardCreditRefundForm = ReturnType<typeof useCardCreditRefundForm>
