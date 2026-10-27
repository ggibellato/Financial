import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountStatementDto, ExpenseDto } from '../api/types'
import { formatLongMonthYear, getSaveErrorMessage } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'

export const MOVE_TARGETS_LOAD_ERROR = 'Unable to load the statements'

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

function monthValue(year: number, month: number): string {
  return `${year}-${String(month).padStart(2, '0')}`
}

export function moveTargetsFor(invoiceDate: string, statements: readonly AccountStatementDto[]): MoveTarget[] {
  const [currentYear, currentMonth] = invoiceDate.split('-').map(Number)
  const targets: MoveTarget[] = []
  for (let offset = 1; offset <= LATER_MONTHS_OFFERED; offset++) {
    const monthIndex = currentMonth - 1 + offset
    const year = currentYear + Math.floor(monthIndex / 12)
    const month = (monthIndex % 12) + 1
    const statement = statements.find((s) => s.year === year && s.month === month)
    if (statement?.isPaid) continue
    const label = formatLongMonthYear(year, month)
    targets.push({ value: monthValue(year, month), label: statement ? label : `${label} (new)` })
  }
  return targets
}

export function useMoveChargeForm(onMoved: (message: string) => void) {
  const [form, setForm] = useState<MoveForm | null>(null)
  const [savedCount, setSavedCount] = useState(0)
  const cardId = form?.charge.creditCardId ?? null
  const statements = useAsyncResource(() => (cardId ? apiClient.getAccountStatements(cardId) : null), [cardId], MOVE_TARGETS_LOAD_ERROR)

  const open = (charge: ExpenseDto) => setForm({ charge, target: '', isSaving: false, error: null, targetError: null })

  const cancel = () => setForm(null)

  const setTarget = (target: string) => setForm((current) => current && { ...current, target, targetError: null })

  const submit = () => {
    if (!form || form.isSaving) return
    if (!form.target) {
      setForm({ ...form, targetError: 'Select a statement.', error: null })
      return
    }

    const [year, month] = form.target.split('-').map(Number)
    setForm({ ...form, isSaving: true, error: null })
    apiClient
      .moveChargeToStatement(form.charge.id, { year, month })
      .then(() => {
        setForm(null)
        setSavedCount((count) => count + 1)
        onMoved('Charge moved')
      })
      .catch((err: unknown) => {
        setForm((current) => current && { ...current, isSaving: false, error: getSaveErrorMessage(err, 'Failed to move the charge') })
      })
  }

  return {
    isOpen: form !== null,
    form,
    targets: form?.charge.invoiceDate ? moveTargetsFor(form.charge.invoiceDate, statements.data ?? []) : [],
    isLoadingTargets: statements.isLoading,
    targetsError: statements.error,
    retryTargets: statements.retry,
    isDirty: form !== null && form.target !== '',
    savedCount,
    open,
    cancel,
    setTarget,
    submit,
  }
}

export type MoveChargeFormState = ReturnType<typeof useMoveChargeForm>
