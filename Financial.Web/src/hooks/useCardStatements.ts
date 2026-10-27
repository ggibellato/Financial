import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountStatementDto } from '../api/types'
import { getSaveErrorMessage, todayIsoDate } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'

export const STATEMENTS_LOAD_ERROR = 'Unable to load the statements'
export const PAYERS_LOAD_ERROR = 'Unable to load the banks'

export type PayStatementField = 'bankId' | 'date'

interface PayForm {
  statement: AccountStatementDto
  bankId: string
  date: string
  isSaving: boolean
  error: string | null
  fieldErrors: Partial<Record<PayStatementField, string>>
}

interface UnmarkRequest {
  statement: AccountStatementDto
  isBusy: boolean
  error: string | null
}

function validatePayForm(bankId: string, date: string): Partial<Record<PayStatementField, string>> {
  const errors: Partial<Record<PayStatementField, string>> = {}
  if (!bankId) errors.bankId = 'Select a bank'
  if (!date) errors.date = 'Enter a date'
  else if (date > todayIsoDate()) errors.date = 'Date must not be in the future.'
  return errors
}

export function useCardStatements(cardId: string | null, onChanged: (message: string) => void) {
  const statements = useAsyncResource(
    () => (cardId ? apiClient.getAccountStatements(cardId) : null),
    [cardId],
    STATEMENTS_LOAD_ERROR,
    { initialLoading: cardId !== null, keepDataWhileLoading: true },
  )
  const payers = useAsyncResource(() => (cardId ? apiClient.getBanks() : null), [cardId], PAYERS_LOAD_ERROR)
  const [payForm, setPayForm] = useState<PayForm | null>(null)
  const [unmark, setUnmark] = useState<UnmarkRequest | null>(null)

  const openPayForm = (statement: AccountStatementDto) =>
    setPayForm({ statement, bankId: '', date: todayIsoDate(), isSaving: false, error: null, fieldErrors: {} })

  const closePayForm = () => setPayForm(null)

  const setPayField = (field: PayStatementField, value: string) =>
    setPayForm((current) =>
      current && { ...current, [field]: value, fieldErrors: { ...current.fieldErrors, [field]: undefined } },
    )

  const submitPayForm = () => {
    if (!payForm || payForm.isSaving) return
    const fieldErrors = validatePayForm(payForm.bankId, payForm.date)
    if (Object.keys(fieldErrors).length > 0) {
      setPayForm({ ...payForm, fieldErrors, error: null })
      return
    }

    setPayForm({ ...payForm, isSaving: true, error: null })
    apiClient
      .markCardStatementPaid(payForm.statement.id, { paymentSourceBankId: payForm.bankId, date: payForm.date })
      .then(() => {
        setPayForm(null)
        void statements.refresh()
        onChanged('Statement paid')
      })
      .catch((err: unknown) =>
        setPayForm((current) => current && { ...current, isSaving: false, error: getSaveErrorMessage(err, 'Failed to pay the statement') }),
      )
  }

  const requestUnmark = (statement: AccountStatementDto) => setUnmark({ statement, isBusy: false, error: null })

  const cancelUnmark = () => setUnmark((current) => (current?.isBusy ? current : null))

  const confirmUnmark = () => {
    if (!unmark || unmark.isBusy) return
    setUnmark({ ...unmark, isBusy: true, error: null })
    apiClient
      .unmarkCardStatementPaid(unmark.statement.id)
      .then(() => {
        setUnmark(null)
        void statements.refresh()
        onChanged('Statement unmarked')
      })
      .catch((err: unknown) =>
        setUnmark((current) => current && { ...current, isBusy: false, error: getSaveErrorMessage(err, 'Failed to unmark the statement') }),
      )
  }

  return {
    statements: statements.data ?? [],
    isLoading: statements.isLoading && statements.data === null,
    error: statements.error,
    retry: statements.retry,
    payers: (payers.data ?? []).filter((bank) => bank.isActive),
    payersError: payers.error,
    retryPayers: payers.retry,
    payForm,
    isPayFormDirty: payForm !== null && (payForm.bankId !== '' || payForm.date !== todayIsoDate()),
    openPayForm,
    closePayForm,
    setPayField,
    submitPayForm,
    unmark,
    requestUnmark,
    cancelUnmark,
    confirmUnmark,
  }
}

export type CardStatements = ReturnType<typeof useCardStatements>
