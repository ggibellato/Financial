import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountDto, CardCreditDto } from '../api/types'
import { accountsWith } from '../utils/accountCapabilities'
import { formatN2, getSaveErrorMessage, positiveAmount, todayIsoDate } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'
import { paymentDateError } from './useCardStatements'

const RECEIVES_TRANSFERS_CAPABILITY = 'CanReceiveTransfers'

export type CardCreditRefundField = 'counterpartId' | 'amount' | 'date'

export type CreditRefundMode = 'card' | 'bank'

interface CardCreditRefundValues {
  counterpartId: string
  amount: string
  date: string
}

export interface CreditRefundAccount {
  id: string
  name: string
  currency: string
}

interface Counterpart {
  id: string
  label: string
  credit: number
}

const MODE_TEXT = {
  card: { loadError: 'Unable to load the banks', saveError: 'Failed to refund the credit', missing: 'Select a bank.' },
  bank: { loadError: 'Unable to load the cards', saveError: 'Failed to receive the card credit', missing: 'Select a card.' },
} as const

function amountFor(credit: number | undefined): string {
  return credit === undefined ? '' : credit.toFixed(2)
}

function amountError(amount: string, credit: number | undefined, currency: string): string | undefined {
  const parsed = positiveAmount(amount)
  if (parsed === null) return 'Amount must be greater than zero.'
  return credit !== undefined && parsed > credit ? `The card's credit is ${formatN2(credit)} ${currency}.` : undefined
}

function cardLabel(name: string, credit: number, isActive: boolean): string {
  return `${name} · Credit ${formatN2(credit)}${isActive ? '' : ' (inactive)'}`
}

interface CounterpartLists {
  banks: AccountDto[] | null
  cards: CardCreditDto[] | null
}

function loadCounterparts(mode: CreditRefundMode): Promise<CounterpartLists> {
  return mode === 'card'
    ? apiClient.getAccounts().then((accounts) => ({ banks: accounts, cards: null }))
    : apiClient.getCardsInCredit().then((cards) => ({ banks: null, cards }))
}

export function useCardCreditRefundForm(
  mode: CreditRefundMode,
  account: CreditRefundAccount | undefined,
  cardCredit: number,
  onRefunded: () => void,
) {
  const text = MODE_TEXT[mode]
  const [isOpen, setIsOpen] = useState(false)
  const [values, setValues] = useState<CardCreditRefundValues>(() => ({ counterpartId: '', amount: '', date: todayIsoDate() }))
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<CardCreditRefundField, string>>>({})
  const lists = useAsyncResource(() => (isOpen ? loadCounterparts(mode) : null), [isOpen, mode], text.loadError)
  const currency = account?.currency ?? ''
  const counterparts: Counterpart[] =
    mode === 'card'
      ? accountsWith(lists.data?.banks ?? [], RECEIVES_TRANSFERS_CAPABILITY)
          .filter((bank) => bank.currency === currency)
          .map((bank) => ({ id: bank.id, label: bank.name, credit: cardCredit }))
      : (lists.data?.cards ?? [])
          .filter((card) => card.currency === currency)
          .map((card) => ({ id: card.accountId, label: cardLabel(card.name, card.credit, card.isActive), credit: card.credit }))
  const chosen = counterparts.find((counterpart) => counterpart.id === values.counterpartId)
  const credit = mode === 'card' ? cardCredit : chosen?.credit
  const current = { ...values, counterpartId: chosen?.id ?? '' }
  const openedAmount = mode === 'card' ? amountFor(cardCredit) : ''
  const openedDate = todayIsoDate()

  const open = () => {
    setValues({ counterpartId: '', amount: openedAmount, date: openedDate })
    setSaveError(null)
    setFieldErrors({})
    setIsOpen(true)
  }

  const cancel = () => setIsOpen(false)

  const setField = (field: CardCreditRefundField, value: string) => {
    const isAmountUntouched = values.amount === '' || values.amount === amountFor(chosen?.credit)
    const defaultAmount =
      mode === 'bank' && field === 'counterpartId' && isAmountUntouched
        ? amountFor(counterparts.find((counterpart) => counterpart.id === value)?.credit)
        : null
    setValues((previous) => ({ ...previous, [field]: value, ...(defaultAmount === null ? {} : { amount: defaultAmount }) }))
    setSaveError(null)
    setFieldErrors((previous) => ({ ...previous, [field]: undefined, ...(defaultAmount === null ? {} : { amount: undefined }) }))
  }

  const submit = (): CardCreditRefundField | null => {
    if (isSaving || !account) return null
    const errors = {
      counterpartId: current.counterpartId ? undefined : text.missing,
      amount: amountError(current.amount, credit, currency),
      date: paymentDateError(current.date),
    }
    const firstInvalid = (['counterpartId', 'amount', 'date'] as const).find((field) => errors[field])
    if (firstInvalid) {
      setFieldErrors(errors)
      setSaveError(null)
      return firstInvalid
    }

    const [sourceAccountId, destinationAccountId] = mode === 'card' ? [account.id, current.counterpartId] : [current.counterpartId, account.id]
    setIsSaving(true)
    setSaveError(null)
    apiClient
      .createCardCreditRefund({
        date: current.date,
        sourceAccountId,
        destinationAccountId,
        amount: Number(current.amount),
        destinationAmount: null,
        note: null,
      })
      .then(() => {
        cancel()
        onRefunded()
      })
      .catch((err: unknown) => {
        setSaveError(getSaveErrorMessage(err, text.saveError))
        void lists.refresh()
      })
      .finally(() => setIsSaving(false))
    return null
  }

  return {
    mode,
    isOpen,
    accountName: account?.name ?? '',
    counterparts,
    currency,
    values: current,
    isLoading: lists.isLoading,
    loadError: lists.error,
    retry: lists.retry,
    hasNoCounterparts: isOpen && !lists.isLoading && lists.error === null && lists.data !== null && counterparts.length === 0,
    isSaving,
    saveError,
    fieldErrors,
    isDirty: isOpen && (values.counterpartId !== '' || values.amount !== openedAmount || values.date !== openedDate),
    open,
    cancel,
    setField,
    submit,
  }
}

export type CardCreditRefundForm = ReturnType<typeof useCardCreditRefundForm>
