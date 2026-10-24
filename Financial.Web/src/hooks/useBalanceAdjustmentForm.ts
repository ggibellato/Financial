import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { BalanceAdjustmentDto, BankDto } from '../api/types'
import type { BankTotal } from './useMonthly'
import type { AccountFormSettings } from './formSettings'
import { hasChangedFields, snapshotFields } from './inlineFormSlice'
import { mapBalanceAdjustmentErrorToField, type BalanceAdjustmentFormField } from './mapBalanceAdjustmentErrorToField'
import { getSaveErrorMessage, parseValidatedNumber } from '../utils/formatters'
import { getCreateFormDate, getStoredDefault, setStoredDefault } from '../utils/createFormDefaults'
import { selectableBanks } from '../utils/selectableBanks'

const DATE_KEY = 'balanceAdjustment.date'
const BANK_KEY = 'balanceAdjustment.bank'

type BankBalance = Pick<BankTotal, 'bankId' | 'bank' | 'balance'>

const EDITABLE_FIELDS = ['bankName', 'date', 'targetBalance', 'note'] as const

type EditableValues = Pick<BalanceAdjustmentFormState, (typeof EDITABLE_FIELDS)[number]>

interface BalanceAdjustmentFormState {
  openedValues: EditableValues | null
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
  saveErrorFields: Partial<Record<BalanceAdjustmentFormField, string>>
  savedDelta: number | null
}

const BLANK_STATE: BalanceAdjustmentFormState = {
  openedValues: null,
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
  saveErrorFields: {},
  savedDelta: null,
}

function withOpenedValues(state: BalanceAdjustmentFormState): BalanceAdjustmentFormState {
  return { ...state, openedValues: snapshotFields(state, EDITABLE_FIELDS) }
}

function resolveCurrentBalance(bankTotals: BankBalance[], bankId: string): number {
  return bankTotals.find((b) => b.bankId === bankId)?.balance ?? 0
}

export interface UseBalanceAdjustmentFormResult {
  isOpen: boolean
  isEditing: boolean
  bankName: string
  bankDisplayName: string
  currentBalance: number
  date: string
  targetBalance: string
  note: string
  isSaving: boolean
  isDirty: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<BalanceAdjustmentFormField, string>>
  savedDelta: number | null
  openCreateForm: () => void
  openEditForm: (adjustment: BalanceAdjustmentDto) => void
  cancel: () => void
  setField: (field: BalanceAdjustmentFormField, value: string) => void
  submit: () => void
}

/**
 * Create defaults the bank to the last successfully used one (P38-F10), validated against the
 * currently fetched bankTotals and the active banks so a removed or inactive bank never gets
 * silently re-selected; picking a bank resolves its current calculated balance client-side from
 * bankTotals (no new network call).
 */
export function useBalanceAdjustmentForm(
  bankTotals: BankBalance[],
  banks: BankDto[],
  onSaved: (isEdit: boolean) => void,
  { lockedBank, viewedMonth }: AccountFormSettings = {},
): UseBalanceAdjustmentFormResult {
  const [state, setState] = useState<BalanceAdjustmentFormState>(BLANK_STATE)

  function openCreateForm() {
    const date = getCreateFormDate(DATE_KEY, viewedMonth)
    const storedBank = getStoredDefault(BANK_KEY)
    const bankName = lockedBank?.id ?? (storedBank && bankTotals.some((b) => b.bankId === storedBank) && selectableBanks(banks).some((b) => b.id === storedBank) ? storedBank : '')
    setState(
      withOpenedValues({
        ...BLANK_STATE,
        isOpen: true,
        date,
        bankName,
        currentBalance: resolveCurrentBalance(bankTotals, bankName),
      }),
    )
  }

  function openEditForm(adjustment: BalanceAdjustmentDto) {
    setState(
      withOpenedValues({
        ...BLANK_STATE,
        isOpen: true,
        isEditing: true,
        editingId: adjustment.id,
        bankName: adjustment.bankId,
        currentBalance: resolveCurrentBalance(bankTotals, adjustment.bankId),
        date: adjustment.date,
        targetBalance: String(adjustment.targetBalance),
        note: adjustment.note ?? '',
      }),
    )
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
        saveErrorFields: {},
      }))
      return
    }
    setState((s) => ({ ...s, [field]: value, saveError: null, saveErrorFields: {} }))
  }

  function submit() {
    if (!state.bankName.trim()) {
      // Defensive guard: Save is disabled client-side until a bank is chosen (PRD Error
      // Handling), so this path is unreachable via the UI and needs no error message.
      return
    }

    const errors: Partial<Record<BalanceAdjustmentFormField, string>> = {}

    if (!state.date.trim()) {
      errors.date = 'Date is required'
    }

    const targetBalance = parseValidatedNumber(state.targetBalance, { min: 0 })
    if (targetBalance === null) {
      errors.targetBalance = 'Balance cannot be negative.'
    }

    if (Object.keys(errors).length > 0) {
      setState((s) => ({ ...s, saveError: Object.values(errors)[0] ?? null, saveErrorFields: errors }))
      return
    }

    setState((s) => ({ ...s, isSaving: true, saveError: null, saveErrorFields: {} }))

    const payload = {
      date: state.date,
      targetBalance: targetBalance as number,
      note: state.note.trim() === '' ? null : state.note,
    }

    const request =
      state.isEditing && state.editingId
        ? apiClient.updateBalanceAdjustment(state.bankName, state.editingId, payload)
        : apiClient.createBalanceAdjustment(state.bankName, payload)

    void request
      .then((result) => {
        setStoredDefault(DATE_KEY, state.date)
        if (!lockedBank) setStoredDefault(BANK_KEY, state.bankName)
        setState((s) => ({ ...s, isSaving: false, savedDelta: result.delta }))
        onSaved(state.isEditing)
      })
      .catch((err: unknown) => {
        const message = getSaveErrorMessage(err, 'Failed to save balance adjustment')
        const field = mapBalanceAdjustmentErrorToField(message)
        setState((s) => ({
          ...s,
          isSaving: false,
          saveError: message,
          saveErrorFields: field === null ? {} : { [field]: message },
        }))
      })
  }

  return {
    isOpen: state.isOpen,
    isEditing: state.isEditing,
    bankName: state.bankName,
    bankDisplayName: bankTotals.find((b) => b.bankId === state.bankName)?.bank ?? '',
    currentBalance: state.currentBalance,
    date: state.date,
    targetBalance: state.targetBalance,
    note: state.note,
    isSaving: state.isSaving,
    isDirty: state.isOpen && state.savedDelta === null && hasChangedFields(state, state.openedValues, EDITABLE_FIELDS),
    saveError: state.saveError,
    saveErrorFields: state.saveErrorFields,
    savedDelta: state.savedDelta,
    openCreateForm,
    openEditForm,
    cancel,
    setField,
    submit,
  }
}
