import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountStatementDto } from '../api/types'
import { formatLongMonthYear, formatMonthInputValue, getSaveErrorMessage, lastDayOfMonthIsoDate } from '../utils/formatters'

export interface DueDateFormState {
  statement: AccountStatementDto
  date: string
  pendingAction: 'save' | 'clear' | null
  error: string | null
  dateError: string | undefined
}

export function dueDateRangeOf(statement: AccountStatementDto): { min: string; max: string } {
  return {
    min: `${formatMonthInputValue(statement.year, statement.month)}-01`,
    max: lastDayOfMonthIsoDate(statement.year, statement.month),
  }
}

function dueDateError(statement: AccountStatementDto, date: string): string | undefined {
  if (!date) return 'Enter a due date.'
  const { min, max } = dueDateRangeOf(statement)
  if (date < min || date > max) return `The due date must be in ${formatLongMonthYear(statement.year, statement.month)}.`
  return undefined
}

export function useStatementDueDateForm(cardId: string | null, onSaved: (message: string) => void) {
  const [form, setForm] = useState<DueDateFormState | null>(null)
  const [failedValidations, setFailedValidations] = useState(0)

  const open = (statement: AccountStatementDto) => {
    if (form?.pendingAction) return
    setForm({ statement, date: statement.dueDate ?? '', pendingAction: null, error: null, dateError: undefined })
  }

  const close = () => setForm(null)

  const setDate = (date: string) => setForm((current) => current && { ...current, date, dateError: undefined })

  const send = (current: DueDateFormState, dueDate: string | null) => {
    if (!cardId) return
    setForm({ ...current, pendingAction: dueDate === null ? 'clear' : 'save', error: null, dateError: undefined })
    apiClient
      .setStatementDueDate(cardId, current.statement.year, current.statement.month, { dueDate })
      .then(() => {
        setForm((latest) => (latest?.statement === current.statement ? null : latest))
        onSaved(dueDate === null ? 'Due date cleared' : 'Due date saved')
      })
      .catch((err: unknown) => {
        const error = getSaveErrorMessage(err, 'Failed to save the due date.')
        setForm((latest) => (latest?.statement === current.statement ? { ...latest, pendingAction: null, error } : latest))
      })
  }

  const save = () => {
    if (!form || form.pendingAction) return
    const dateError = dueDateError(form.statement, form.date)
    if (dateError) {
      setForm({ ...form, dateError, error: null })
      setFailedValidations((count) => count + 1)
      return
    }
    send(form, form.date)
  }

  const clear = () => {
    if (!form || form.pendingAction) return
    send(form, null)
  }

  return {
    form,
    failedValidations,
    isDirty: form !== null && form.date !== (form.statement.dueDate ?? ''),
    open,
    close,
    setDate,
    save,
    clear,
  }
}

export type StatementDueDateFormHandle = ReturnType<typeof useStatementDueDateForm>
