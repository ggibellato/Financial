import { useState } from 'react'
import { isConflict } from '../api/apiError'
import { apiClient } from '../api/financialApiClient'
import type { AccountStatementDto, BankDto } from '../api/types'
import { getSaveErrorMessage, todayIsoDate } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'

export const STATEMENTS_LOAD_ERROR = 'Unable to load the statements'
export const PAYERS_LOAD_ERROR = 'Unable to load the banks'

const NO_PAYERS: BankDto[] = []

export type PayStatementField = 'bankId' | 'date'

export interface PayForm {
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

export function paymentDateError(date: string): string | undefined {
  if (!date) return 'Enter a date.'
  if (date > todayIsoDate()) return 'Date must not be in the future.'
  return undefined
}

function validatePayForm(bankId: string, date: string): Partial<Record<PayStatementField, string>> {
  const errors: Partial<Record<PayStatementField, string>> = {}
  if (!bankId) errors.bankId = 'Select a bank.'
  const dateError = paymentDateError(date)
  if (dateError) errors.date = dateError
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
  const [savedCount, setSavedCount] = useState(0)
  const [failedSubmit, setFailedSubmit] = useState<{ firstInvalidField: PayStatementField } | null>(null)
  const [formsCardId, setFormsCardId] = useState(cardId)

  if (formsCardId !== cardId) {
    setFormsCardId(cardId)
    setPayForm(null)
    setUnmark(null)
  }

  const openPayForm = (statement: AccountStatementDto) => {
    if (payForm?.isSaving) return
    setPayForm({ statement, bankId: '', date: todayIsoDate(), isSaving: false, error: null, fieldErrors: {} })
  }

  const closePayForm = () => setPayForm(null)

  const afterSave = (message: string) => {
    setSavedCount((count) => count + 1)
    void statements.refresh()
    onChanged(message)
  }

  const refreshOnConflict = (err: unknown) => {
    if (isConflict(err)) void statements.refresh()
  }

  const setPayField = (field: PayStatementField, value: string) =>
    setPayForm((current) =>
      current && { ...current, [field]: value, fieldErrors: { ...current.fieldErrors, [field]: undefined } },
    )

  const submitPayForm = () => {
    if (!payForm || payForm.isSaving) return
    const fieldErrors = validatePayForm(payForm.bankId, payForm.date)
    if (Object.keys(fieldErrors).length > 0) {
      setPayForm({ ...payForm, fieldErrors, error: null })
      setFailedSubmit({ firstInvalidField: fieldErrors.bankId ? 'bankId' : 'date' })
      return
    }

    setPayForm({ ...payForm, isSaving: true, error: null })
    apiClient
      .markCardStatementPaid(payForm.statement.id, { paymentSourceBankId: payForm.bankId, date: payForm.date })
      .then(() => {
        setPayForm(null)
        afterSave('Statement paid')
      })
      .catch((err: unknown) => {
        setPayForm((current) => current && { ...current, isSaving: false, error: getSaveErrorMessage(err, 'Failed to pay the statement') })
        refreshOnConflict(err)
      })
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
        afterSave('Statement unmarked')
      })
      .catch((err: unknown) => {
        setUnmark((current) => current && { ...current, isBusy: false, error: getSaveErrorMessage(err, 'Failed to unmark the statement') })
        refreshOnConflict(err)
      })
  }

  return {
    statements: statements.data ?? [],
    isLoading: statements.isLoading && statements.data === null,
    error: statements.error,
    retry: statements.retry,
    payers: payers.data ?? NO_PAYERS,
    isLoadingPayers: payers.isLoading,
    payersError: payers.error,
    retryPayers: payers.retry,
    payForm,
    savedCount,
    failedSubmit,
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
