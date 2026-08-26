import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { BankDto, TransferDto } from '../api/types'
import { mapTransferErrorToField, type TransferFormField } from './mapTransferErrorToField'
import { getErrorMessage, parseValidatedNumber, todayIsoDate } from '../utils/formatters'

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
  saveErrorField: TransferFormField | null
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
  saveErrorField: null,
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
  saveErrorField: TransferFormField | null
  openCreateForm: (preselectedSourceBank?: string) => void
  openEditForm: (transfer: TransferDto) => void
  cancel: () => void
  setField: (field: TransferFormField, value: string) => void
  submit: () => void
}

export function useTransferForm(banks: BankDto[], onSaved: () => void): UseTransferFormResult {
  const [state, setState] = useState<TransferFormState>(BLANK_STATE)

  function openCreateForm(preselectedSourceBank?: string) {
    setState({
      ...BLANK_STATE,
      isOpen: true,
      date: todayIsoDate(),
      sourceBank: preselectedSourceBank ?? banks[0]?.id ?? '',
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
      saveErrorField: null,
    })
  }

  function cancel() {
    setState(BLANK_STATE)
  }

  function setField(field: TransferFormField, value: string) {
    setState((s) => ({ ...s, [field]: value, saveError: null, saveErrorField: null }))
  }

  function submit() {
    if (!state.date.trim()) {
      setState((s) => ({ ...s, saveError: 'Date is required', saveErrorField: 'date' }))
      return
    }

    if (!state.sourceBank.trim()) {
      setState((s) => ({ ...s, saveError: 'Source bank is required', saveErrorField: 'sourceBank' }))
      return
    }

    if (!state.destinationBank.trim()) {
      setState((s) => ({ ...s, saveError: 'Destination bank is required', saveErrorField: 'destinationBank' }))
      return
    }

    if (state.sourceBank === state.destinationBank) {
      setState((s) => ({
        ...s,
        saveError: 'Source and destination must be different banks.',
        saveErrorField: 'destinationBank',
      }))
      return
    }

    const amount = parseValidatedNumber(state.amount)
    if (amount === null || amount <= 0) {
      setState((s) => ({ ...s, saveError: 'Amount must be greater than zero.', saveErrorField: 'amount' }))
      return
    }

    setState((s) => ({ ...s, isSaving: true, saveError: null, saveErrorField: null }))

    const payload = {
      date: state.date,
      sourceBankId: state.sourceBank,
      destinationBankId: state.destinationBank,
      amount,
      note: state.note.trim() === '' ? null : state.note,
    }

    const request =
      state.isEditing && state.editingId
        ? apiClient.updateTransfer(state.editingId, payload)
        : apiClient.createTransfer(payload)

    void request
      .then(() => {
        setState(BLANK_STATE)
        onSaved()
      })
      .catch((err: unknown) => {
        const message = getErrorMessage(err, 'Failed to save transfer')
        const field = mapTransferErrorToField(message, state.sourceBank, state.destinationBank)
        setState((s) => ({ ...s, isSaving: false, saveError: message, saveErrorField: field }))
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
    saveErrorField: state.saveErrorField,
    openCreateForm,
    openEditForm,
    cancel,
    setField,
    submit,
  }
}
