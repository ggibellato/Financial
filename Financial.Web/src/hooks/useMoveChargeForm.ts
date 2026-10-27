import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountStatementDto, ExpenseDto } from '../api/types'
import { formatLongMonthYear, formatMonthInputValue, getSaveErrorMessage, parseMonthInputValue } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'
import { STATEMENTS_LOAD_ERROR } from './useCardStatements'

const LATER_MONTHS_OFFERED = 12

export interface MoveTarget {
  value: string
  label: string
}

interface MoveForm {
  charge: ExpenseDto
  target: string
  isSaving: boolean
  error: string | null
  targetError: string | null
}

function invoiceMonthOf(invoiceDate: string): { year: number; month: number } | null {
  return parseMonthInputValue(invoiceDate.slice(0, 7))
}

export function moveTargetsFor(invoiceDate: string, statements: readonly AccountStatementDto[]): MoveTarget[] {
  const current = invoiceMonthOf(invoiceDate)
  if (!current) return []
  const targets: MoveTarget[] = []
  for (let offset = 1; offset <= LATER_MONTHS_OFFERED; offset++) {
    const monthIndex = current.month - 1 + offset
    const year = current.year + Math.floor(monthIndex / 12)
    const month = (monthIndex % 12) + 1
    const statement = statements.find((s) => s.year === year && s.month === month)
    if (statement?.isPaid) continue
    const label = formatLongMonthYear(year, month)
    targets.push({ value: formatMonthInputValue(year, month), label: statement ? label : `${label} (new)` })
  }
  return targets
}

export function currentStatementLabel(invoiceDate: string | null | undefined): string {
  const current = invoiceDate ? invoiceMonthOf(invoiceDate) : null
  return current ? formatLongMonthYear(current.year, current.month) : ''
}

export function useMoveChargeForm(onMoved: (message: string) => void) {
  const [form, setForm] = useState<MoveForm | null>(null)
  const cardId = form?.charge.creditCardId ?? null
  const statements = useAsyncResource(() => (cardId ? apiClient.getAccountStatements(cardId) : null), [cardId], STATEMENTS_LOAD_ERROR, {
    keepDataWhileLoading: true,
  })

  const open = (charge: ExpenseDto) => setForm({ charge, target: '', isSaving: false, error: null, targetError: null })

  const cancel = () => setForm(null)

  const setTarget = (target: string) => setForm((current) => current && { ...current, target, targetError: null })

  const submit = (): boolean => {
    if (!form || form.isSaving) return true
    const target = parseMonthInputValue(form.target)
    if (!target) {
      setForm({ ...form, targetError: 'Select a statement.', error: null })
      return false
    }

    setForm({ ...form, isSaving: true, error: null })
    apiClient
      .moveChargeToStatement(form.charge.id, target)
      .then(() => {
        setForm(null)
        onMoved('Charge moved')
      })
      .catch((err: unknown) => {
        setForm((current) => current && { ...current, isSaving: false, error: getSaveErrorMessage(err, 'Failed to move the charge') })
        void statements.refresh()
      })
    return true
  }

  const targets = form?.charge.invoiceDate ? moveTargetsFor(form.charge.invoiceDate, statements.data ?? []) : []

  return {
    isOpen: form !== null,
    form,
    targets,
    isLoadingTargets: statements.isLoading,
    hasNoTargets: form !== null && !statements.isLoading && statements.error === null && statements.data !== null && targets.length === 0,
    targetsError: statements.error,
    retryTargets: statements.retry,
    isDirty: form !== null && form.target !== '',
    open,
    cancel,
    setTarget,
    submit,
  }
}

export type MoveChargeFormState = ReturnType<typeof useMoveChargeForm>
