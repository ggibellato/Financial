import { useMemo, useState } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { BalanceAdjustmentDto } from '../api/types'
import type { BankTotal } from './useMonthly'
import { mapBalanceAdjustmentErrorToField, type BalanceAdjustmentFormField } from './mapBalanceAdjustmentErrorToField'

interface BalanceAdjustmentFormState {
  isOpen: boolean
  isEditing: boolean
  editingId: string | null
  bankName: string
  currentBalance: number
  date: string
  targetBalance: string
  note: string
  isSaving: boolean
  saveError: string | null
  saveErrorField: BalanceAdjustmentFormField | null
  savedDelta: number | null
}

const BLANK_STATE: BalanceAdjustmentFormState = {
  isOpen: false,
  isEditing: false,
  editingId: null,
  bankName: '',
  currentBalance: 0,
  date: '',
  targetBalance: '',
  note: '',
  isSaving: false,
  saveError: null,
  saveErrorField: null,
  savedDelta: null,
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10)
}

function resolveCurrentBalance(bankTotals: BankTotal[], bankName: string): number {
  return bankTotals.find((b) => b.bank === bankName)?.balance ?? 0
}

export interface UseBalanceAdjustmentFormResult {
  isOpen: boolean
  isEditing: boolean
  bankName: string
  currentBalance: number
  date: string
  targetBalance: string
  note: string
  isSaving: boolean
  saveError: string | null
  saveErrorField: BalanceAdjustmentFormField | null
  savedDelta: number | null
  openCreateForm: () => void
  openEditForm: (adjustment: BalanceAdjustmentDto) => void
  cancel: () => void
  setField: (field: BalanceAdjustmentFormField, value: string) => void
  submit: () => void
}

/**
 * Owns a balance adjustment's create/edit form state and orchestrates F02's create/update
 * endpoints. Create opens with no bank pre-selected; picking a bank resolves its current
 * calculated balance client-side from the already-fetched bankTotals (no new network call).
 */
export function useBalanceAdjustmentForm(
  bankTotals: BankTotal[],
  onSaved: () => void,
): UseBalanceAdjustmentFormResult {
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, setState] = useState<BalanceAdjustmentFormState>(BLANK_STATE)

  function openCreateForm() {
    setState({
      ...BLANK_STATE,
      isOpen: true,
      date: todayIsoDate(),
    })
  }

  function openEditForm(adjustment: BalanceAdjustmentDto) {
    setState({
      isOpen: true,
      isEditing: true,
      editingId: adjustment.id,
      bankName: adjustment.bank,
      currentBalance: resolveCurrentBalance(bankTotals, adjustment.bank),
      date: adjustment.date,
      targetBalance: String(adjustment.targetBalance),
      note: adjustment.note ?? '',
      isSaving: false,
      saveError: null,
      saveErrorField: null,
      savedDelta: null,
    })
  }

  function cancel() {
    setState(BLANK_STATE)
  }

  function setField(field: BalanceAdjustmentFormField, value: string) {
    if (field === 'bankName') {
      setState((s) => ({
        ...s,
        bankName: value,
        currentBalance: resolveCurrentBalance(bankTotals, value),
        saveError: null,
        saveErrorField: null,
      }))
      return
    }
    setState((s) => ({ ...s, [field]: value, saveError: null, saveErrorField: null }))
  }

  function submit() {
    if (!state.bankName.trim()) {
      // Defensive guard: Save is disabled client-side until a bank is chosen (PRD Error
      // Handling), so this path is unreachable via the UI and needs no error message.
      return
    }

    if (!state.date.trim()) {
      setState((s) => ({ ...s, saveError: 'Date is required', saveErrorField: 'date' }))
      return
    }

    const targetBalance = Number(state.targetBalance)
    if (!state.targetBalance.trim() || !isFinite(targetBalance) || targetBalance < 0) {
      setState((s) => ({ ...s, saveError: 'Balance cannot be negative.', saveErrorField: 'targetBalance' }))
      return
    }

    setState((s) => ({ ...s, isSaving: true, saveError: null, saveErrorField: null }))

    const payload = {
      date: state.date,
      targetBalance,
      note: state.note.trim() === '' ? null : state.note,
    }

    const request =
      state.isEditing && state.editingId
        ? apiClient.updateBalanceAdjustment(state.bankName, state.editingId, payload)
        : apiClient.createBalanceAdjustment(state.bankName, payload)

    void request
      .then((result) => {
        setState((s) => ({ ...s, isSaving: false, savedDelta: result.delta }))
        onSaved()
      })
      .catch((err: unknown) => {
        const message = err instanceof Error ? err.message : 'Failed to save balance adjustment'
        const field = mapBalanceAdjustmentErrorToField(message)
        setState((s) => ({ ...s, isSaving: false, saveError: message, saveErrorField: field }))
      })
  }

  return {
    isOpen: state.isOpen,
    isEditing: state.isEditing,
    bankName: state.bankName,
    currentBalance: state.currentBalance,
    date: state.date,
    targetBalance: state.targetBalance,
    note: state.note,
    isSaving: state.isSaving,
    saveError: state.saveError,
    saveErrorField: state.saveErrorField,
    savedDelta: state.savedDelta,
    openCreateForm,
    openEditForm,
    cancel,
    setField,
    submit,
  }
}
