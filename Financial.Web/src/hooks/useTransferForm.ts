import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { BankDto, TransferDto } from '../api/types'
import { mapTransferErrorToField, type TransferFormField } from './mapTransferErrorToField'
import { getErrorMessage, parseValidatedNumber, todayIsoDate } from '../utils/formatters'
import { getStoredDefault, setStoredDefault } from '../utils/createFormDefaults'

const DATE_KEY = 'transfer.date'
const SOURCE_BANK_KEY = 'transfer.sourceBank'
const DESTINATION_BANK_KEY = 'transfer.destinationBank'

interface TransferFormState {
  isOpen: boolean
  isEditing: boolean
  editingId: string | null
  date: string
  sourceBank: string
  destinationBank: string
  amount: string
  note: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<TransferFormField, string>>
}

const BLANK_STATE: TransferFormState = {
  isOpen: false,
  isEditing: false,
  editingId: null,
  date: '',
  sourceBank: '',
  destinationBank: '',
  amount: '',
  note: '',
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
}

export interface UseTransferFormResult {
  isOpen: boolean
  isEditing: boolean
  date: string
  sourceBank: string
  destinationBank: string
  amount: string
  note: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<TransferFormField, string>>
  openCreateForm: (preselectedSourceBank?: string) => void
  openEditForm: (transfer: TransferDto) => void
  cancel: () => void
  setField: (field: TransferFormField, value: string) => void
  submit: () => void
}

export function useTransferForm(banks: BankDto[], onSaved: () => void): UseTransferFormResult {
  const [state, setState] = useState<TransferFormState>(BLANK_STATE)

  function openCreateForm(preselectedSourceBank?: string) {
    const date = getStoredDefault(DATE_KEY) ?? todayIsoDate()
    const storedSourceBank = getStoredDefault(SOURCE_BANK_KEY)
    const sourceBank =
      preselectedSourceBank ??
      (storedSourceBank && banks.some((b) => b.id === storedSourceBank) ? storedSourceBank : (banks[0]?.id ?? ''))
    const storedDestinationBank = getStoredDefault(DESTINATION_BANK_KEY)
    const destinationBank =
      storedDestinationBank && storedDestinationBank !== sourceBank && banks.some((b) => b.id === storedDestinationBank)
        ? storedDestinationBank
        : ''
    setState({
      ...BLANK_STATE,
      isOpen: true,
      date,
      sourceBank,
      destinationBank,
    })
  }

  function openEditForm(transfer: TransferDto) {
    setState({
      isOpen: true,
      isEditing: true,
      editingId: transfer.id,
      date: transfer.date,
      sourceBank: transfer.sourceBankId,
      destinationBank: transfer.destinationBankId,
      amount: String(transfer.amount),
      note: transfer.note ?? '',
      isSaving: false,
      saveError: null,
      saveErrorFields: {},
    })
  }

  function cancel() {
    setState(BLANK_STATE)
  }

  function setField(field: TransferFormField, value: string) {
    setState((s) => ({ ...s, [field]: value, saveError: null, saveErrorFields: {} }))
  }

  function submit() {
    const errors: Partial<Record<TransferFormField, string>> = {}

    if (!state.date.trim()) {
      errors.date = 'Date is required'
    }

    if (!state.sourceBank.trim()) {
      errors.sourceBank = 'Source bank is required'
    }

    if (!state.destinationBank.trim()) {
      errors.destinationBank = 'Destination bank is required'
    } else if (state.sourceBank === state.destinationBank) {
      errors.destinationBank = 'Source and destination must be different banks.'
    }

    const amount = parseValidatedNumber(state.amount)
    if (amount === null || amount <= 0) {
      errors.amount = 'Amount must be greater than zero.'
    }

    if (Object.keys(errors).length > 0) {
      setState((s) => ({ ...s, saveError: Object.values(errors)[0] ?? null, saveErrorFields: errors }))
      return
    }

    setState((s) => ({ ...s, isSaving: true, saveError: null, saveErrorFields: {} }))

    const payload = {
      date: state.date,
      sourceBankId: state.sourceBank,
      destinationBankId: state.destinationBank,
      amount: amount as number,
      note: state.note.trim() === '' ? null : state.note,
    }

    const request =
      state.isEditing && state.editingId
        ? apiClient.updateTransfer(state.editingId, payload)
        : apiClient.createTransfer(payload)

    void request
      .then(() => {
        setStoredDefault(DATE_KEY, state.date)
        setStoredDefault(SOURCE_BANK_KEY, state.sourceBank)
        setStoredDefault(DESTINATION_BANK_KEY, state.destinationBank)
        setState(BLANK_STATE)
        onSaved()
      })
      .catch((err: unknown) => {
        const message = getErrorMessage(err, 'Failed to save transfer')
        const field = mapTransferErrorToField(message, state.sourceBank, state.destinationBank)
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
    date: state.date,
    sourceBank: state.sourceBank,
    destinationBank: state.destinationBank,
    amount: state.amount,
    note: state.note,
    isSaving: state.isSaving,
    saveError: state.saveError,
    saveErrorFields: state.saveErrorFields,
    openCreateForm,
    openEditForm,
    cancel,
    setField,
    submit,
  }
}
