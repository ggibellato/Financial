import { useMemo, useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { PayableStatementDto } from '../api/types'
import { getSaveErrorMessage, todayIsoDate } from '../utils/formatters'
import type { LockedBank } from './formSettings'
import { useAsyncResource } from './useAsyncResource'

const PAYABLE_STATEMENTS_LOAD_ERROR = 'Unable to load the card statements'

export type PayCardStatementField = 'cardId' | 'statementId' | 'date'

export interface PayableCard {
  id: string
  name: string
}

interface PayCardStatementValues {
  cardId: string
  statementId: string
  date: string
}

const NO_STATEMENTS: PayableStatementDto[] = []

function initialValues(): PayCardStatementValues {
  return { cardId: '', statementId: '', date: todayIsoDate() }
}

export function payableCardsOf(statements: readonly PayableStatementDto[]): PayableCard[] {
  const cards = new Map<string, PayableCard>()
  for (const statement of statements) {
    if (!cards.has(statement.creditCardId)) cards.set(statement.creditCardId, { id: statement.creditCardId, name: statement.creditCardName })
  }
  return [...cards.values()]
}

function validate(values: PayCardStatementValues): Partial<Record<PayCardStatementField, string>> {
  const errors: Partial<Record<PayCardStatementField, string>> = {}
  if (!values.cardId) errors.cardId = 'Select a card.'
  if (!values.statementId) errors.statementId = 'Select a statement.'
  if (!values.date) errors.date = 'Enter a date.'
  else if (values.date > todayIsoDate()) errors.date = 'Date must not be in the future.'
  return errors
}

export function usePayCardStatementForm(lockedBank: (LockedBank & { currency: string }) | undefined, onPaid: () => void) {
  const [isOpen, setIsOpen] = useState(false)
  const [values, setValues] = useState<PayCardStatementValues>(initialValues)
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<PayCardStatementField, string>>>({})
  const payable = useAsyncResource(() => (isOpen ? apiClient.getPayableStatements() : null), [isOpen], PAYABLE_STATEMENTS_LOAD_ERROR)
  const bankCurrency = lockedBank?.currency
  const statements = useMemo(
    () => (payable.data ?? NO_STATEMENTS).filter((statement) => statement.currency === bankCurrency),
    [payable.data, bankCurrency],
  )
  const cards = useMemo(() => payableCardsOf(statements), [statements])
  const cardId = cards.some((card) => card.id === values.cardId) ? values.cardId : ''
  const cardStatements = statements.filter((statement) => statement.creditCardId === cardId)
  const statement = cardStatements.find((candidate) => candidate.id === values.statementId) ?? null
  const current = { ...values, cardId, statementId: statement?.id ?? '' }

  const reset = () => {
    setValues(initialValues())
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

  const setField = (field: PayCardStatementField, value: string) => {
    setValues((previous) => {
      if (field !== 'cardId') return { ...previous, [field]: value }
      const onlyStatement = statements.filter((candidate) => candidate.creditCardId === value)
      return { ...previous, cardId: value, statementId: onlyStatement.length === 1 ? onlyStatement[0].id : '' }
    })
    setSaveError(null)
    setFieldErrors((previous) => ({ ...previous, [field]: undefined, ...(field === 'cardId' ? { statementId: undefined } : {}) }))
  }

  const submit = (): PayCardStatementField | null => {
    if (isSaving || !lockedBank) return null
    const errors = validate(current)
    const firstInvalid = (['cardId', 'statementId', 'date'] as const).find((field) => errors[field])
    if (firstInvalid) {
      setFieldErrors(errors)
      setSaveError(null)
      return firstInvalid
    }

    setIsSaving(true)
    setSaveError(null)
    apiClient
      .markCardStatementPaid(current.statementId, { paymentSourceBankId: lockedBank.id, date: current.date })
      .then(() => {
        cancel()
        onPaid()
      })
      .catch((err: unknown) => {
        setSaveError(getSaveErrorMessage(err, 'Failed to pay the statement'))
        void payable.refresh()
      })
      .finally(() => setIsSaving(false))
    return null
  }

  return {
    isOpen,
    cards,
    statements: cardStatements,
    statement,
    values: current,
    isLoading: payable.isLoading,
    loadError: payable.error,
    retry: payable.retry,
    hasNothingToPay: isOpen && !payable.isLoading && payable.error === null && payable.data !== null && cards.length === 0,
    isSaving,
    saveError,
    fieldErrors,
    isDirty: isOpen && (values.cardId !== '' || values.statementId !== '' || values.date !== todayIsoDate()),
    open,
    cancel,
    setField,
    submit,
  }
}

export type PayCardStatementForm = ReturnType<typeof usePayCardStatementForm>
