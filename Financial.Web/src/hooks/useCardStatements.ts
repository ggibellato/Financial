import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountStatementDto, BankOption } from '../api/types'
import { getSaveErrorMessage, todayIsoDate } from '../utils/formatters'
import { isSettledStatement } from '../utils/owedBalance'
import { paymentDateError } from '../utils/validators'
import { billPayingAccounts } from './useAccountFormOptions'
import { useAsyncResource } from './useAsyncResource'
import { useStatementDueDateForm } from './useStatementDueDateForm'

export const STATEMENTS_LOAD_ERROR = 'Unable to load the statements.'
export const PAYERS_LOAD_ERROR = 'Unable to load the banks.'

const NO_PAYERS: BankOption[] = []

export type PayStatementField = 'bankId' | 'date'

export type StoredStatement = AccountStatementDto & { id: string }

export function isStoredStatement(statement: AccountStatementDto): statement is StoredStatement {
  return statement.id !== null
}

export type PayFormKind = 'pay' | 'settle'

export interface PayForm {
  kind: PayFormKind
  statement: StoredStatement
  bankId: string
  date: string
  isSaving: boolean
  error: string | null
  fieldErrors: Partial<Record<PayStatementField, string>>
}

interface UnmarkRequest {
  statement: StoredStatement
  isBusy: boolean
  error: string | null
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
  const payers = useAsyncResource(() => (cardId ? apiClient.getAccounts().then(billPayingAccounts) : null), [cardId], PAYERS_LOAD_ERROR)
  const [payForm, setPayForm] = useState<PayForm | null>(null)
  const [unmark, setUnmark] = useState<UnmarkRequest | null>(null)
  const [savedCount, setSavedCount] = useState(0)
  const [failedSubmit, setFailedSubmit] = useState<{ firstInvalidField: PayStatementField } | null>(null)
  const [formsCardId, setFormsCardId] = useState(cardId)

  const afterSave = (message: string) => {
    setSavedCount((count) => count + 1)
    void statements.refresh()
    onChanged(message)
  }

  const dueDateForm = useStatementDueDateForm(cardId, afterSave)

  if (formsCardId !== cardId) {
    setFormsCardId(cardId)
    setPayForm(null)
    setUnmark(null)
    dueDateForm.close()
  }

  const openForm = (kind: PayFormKind, statement: StoredStatement) => {
    if (payForm?.isSaving) return
    setPayForm({ kind, statement, bankId: '', date: todayIsoDate(), isSaving: false, error: null, fieldErrors: {} })
  }

  const openPayForm = (statement: StoredStatement) => openForm('pay', statement)

  const openSettleForm = (statement: StoredStatement) => openForm('settle', statement)

  const closePayForm = () => setPayForm(null)

  const closeForms = () => {
    closePayForm()
    dueDateForm.close()
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
    const settling = payForm.kind === 'settle'
    const request = { paymentSourceBankId: payForm.bankId, date: payForm.date }
    const save = settling
      ? apiClient.settlePotWithdrawals(payForm.statement.id, request)
      : apiClient.markCardStatementPaid(payForm.statement.id, request)
    save
      .then(() => {
        setPayForm(null)
        afterSave(settling ? 'Pot withdrawals settled' : 'Statement paid')
      })
      .catch((err: unknown) => {
        const failed = settling ? "Couldn't settle the statement. Try again." : 'Failed to pay the statement.'
        setPayForm((current) => current && { ...current, isSaving: false, error: getSaveErrorMessage(err, failed) })
        void statements.refresh()
      })
  }

  const requestUnmark = (statement: StoredStatement) => setUnmark({ statement, isBusy: false, error: null })

  const cancelUnmark = () => setUnmark((current) => (current?.isBusy ? current : null))

  const confirmUnmark = () => {
    if (!unmark || unmark.isBusy) return
    setUnmark({ ...unmark, isBusy: true, error: null })
    const settled = isSettledStatement(unmark.statement)
    const save = settled ? apiClient.unsettlePotWithdrawals(unmark.statement.id) : apiClient.unmarkCardStatementPaid(unmark.statement.id)
    save
      .then(() => {
        setUnmark(null)
        afterSave(settled ? 'Statement unmarked as settled' : 'Statement unmarked')
      })
      .catch((err: unknown) => {
        const failed = settled ? "Couldn't unmark the statement as settled. Try again." : 'Failed to unmark the statement.'
        setUnmark((current) => current && { ...current, isBusy: false, error: getSaveErrorMessage(err, failed) })
        void statements.refresh()
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
    isFormDirty: dueDateForm.isDirty || (payForm !== null && (payForm.bankId !== '' || payForm.date !== todayIsoDate())),
    dueDateForm,
    openPayForm,
    openSettleForm,
    closeForms,
    setPayField,
    submitPayForm,
    unmark,
    requestUnmark,
    cancelUnmark,
    confirmUnmark,
  }
}

export type CardStatements = ReturnType<typeof useCardStatements>
