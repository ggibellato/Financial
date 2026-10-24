import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountTransferDto, TransferAccountOptionDto } from '../api/types'
import type { LockedTransferSide } from '../components/TransferForm'
import type { AccountFormSettings } from './formSettings'
import { hasChangedFields, snapshotFields } from './inlineFormSlice'
import { counterpartField, mapAccountTransferErrorToField, type TransferDirection } from './mapAccountTransferErrorToField'
import type { TransferFormField } from './mapTransferErrorToField'
import type { TransferCounterparts } from './useAccountFormOptions'
import { getSaveErrorMessage, parseValidatedNumber } from '../utils/formatters'
import { getCreateFormDate, getStoredDefault, setStoredDefault } from '../utils/createFormDefaults'

const DATE_KEY = 'transfer.date'
const COUNTERPART_KEYS: Record<TransferDirection, string> = {
  to: 'accountTransfer.destination',
  from: 'accountTransfer.source',
}

const EDITABLE_FIELDS = ['date', 'sourceBank', 'destinationBank', 'amount', 'destinationAmount', 'note'] as const

type EditableValues = Pick<AccountTransferFormState, (typeof EDITABLE_FIELDS)[number]>

type FieldErrors = Partial<Record<TransferFormField, string>>

interface AccountTransferFormState {
  openedValues: EditableValues | null
  isOpen: boolean
  direction: TransferDirection
  editingId: string | null
  editedCounterpart: TransferAccountOptionDto | null
  date: string
  sourceBank: string
  destinationBank: string
  amount: string
  destinationAmount: string
  note: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: FieldErrors
}

const CLOSED_STATE: AccountTransferFormState = {
  openedValues: null,
  isOpen: false,
  direction: 'to',
  editingId: null,
  editedCounterpart: null,
  date: '',
  sourceBank: '',
  destinationBank: '',
  amount: '',
  destinationAmount: '',
  note: '',
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
}

function withOpenedValues(state: AccountTransferFormState): AccountTransferFormState {
  return { ...state, openedValues: snapshotFields(state, EDITABLE_FIELDS) }
}

function counterpartIdOf(state: AccountTransferFormState): string {
  return state[counterpartField(state.direction)]
}

export interface UseAccountTransferFormResult {
  isOpen: boolean
  isEditing: boolean
  direction: TransferDirection
  lockedSide: LockedTransferSide | undefined
  date: string
  sourceBank: string
  destinationBank: string
  amount: string
  destinationAmount: string
  note: string
  counterpartOptions: TransferAccountOptionDto[]
  sourceCurrency: string | null
  destinationCurrency: string | null
  isSaving: boolean
  isDirty: boolean
  saveError: string | null
  saveErrorFields: FieldErrors
  openCreateForm: (direction: TransferDirection) => void
  openEditForm: (transfer: AccountTransferDto) => void
  cancel: () => void
  setField: (field: TransferFormField, value: string) => void
  submit: () => void
}

export function useAccountTransferForm(
  account: TransferAccountOptionDto | undefined,
  counterparts: TransferCounterparts,
  onSaved: (isEdit: boolean) => void,
  { viewedMonth }: AccountFormSettings = {},
): UseAccountTransferFormResult {
  const [state, setState] = useState<AccountTransferFormState>(CLOSED_STATE)

  const listedCounterparts = state.direction === 'to' ? counterparts.destinations : counterparts.sources
  const counterpartOptions =
    state.editedCounterpart && !listedCounterparts.some((option) => option.id === state.editedCounterpart?.id)
      ? [...listedCounterparts, state.editedCounterpart]
      : listedCounterparts
  const counterpart = counterpartOptions.find((option) => option.id === counterpartIdOf(state))
  const [sourceCurrency, receivedCurrency] =
    state.direction === 'to' ? [account?.currency, counterpart?.currency] : [counterpart?.currency, account?.currency]
  const destinationCurrency = counterpart && sourceCurrency !== receivedCurrency ? (receivedCurrency ?? null) : null
  const lockedSide: LockedTransferSide | undefined = account && {
    side: state.direction === 'to' ? 'source' : 'destination',
    name: account.name,
  }

  function openCreateForm(direction: TransferDirection) {
    if (!account) return
    const options = direction === 'to' ? counterparts.destinations : counterparts.sources
    const storedCounterpart = getStoredDefault(COUNTERPART_KEYS[direction])
    const counterpartId = storedCounterpart && options.some((option) => option.id === storedCounterpart) ? storedCounterpart : ''
    setState(
      withOpenedValues({
        ...CLOSED_STATE,
        isOpen: true,
        direction,
        date: getCreateFormDate(DATE_KEY, viewedMonth),
        sourceBank: direction === 'to' ? account.id : counterpartId,
        destinationBank: direction === 'to' ? counterpartId : account.id,
      }),
    )
  }

  function openEditForm(transfer: AccountTransferDto) {
    if (!account) return
    const direction: TransferDirection = transfer.sourceAccountId === account.id ? 'to' : 'from'
    const editedCounterpart =
      direction === 'to'
        ? { id: transfer.destinationAccountId, name: transfer.destinationAccountName, currency: transfer.destinationCurrency }
        : { id: transfer.sourceAccountId, name: transfer.sourceAccountName, currency: transfer.sourceCurrency }
    const currenciesDiffer = transfer.sourceCurrency !== transfer.destinationCurrency
    setState(
      withOpenedValues({
        ...CLOSED_STATE,
        isOpen: true,
        direction,
        editingId: transfer.id,
        editedCounterpart,
        date: transfer.date,
        sourceBank: transfer.sourceAccountId,
        destinationBank: transfer.destinationAccountId,
        amount: String(transfer.amount),
        destinationAmount: currenciesDiffer ? String(transfer.destinationAmount) : '',
        note: transfer.note ?? '',
      }),
    )
  }

  const cancel = () => setState(CLOSED_STATE)

  function setField(field: TransferFormField, value: string) {
    setState((s) => ({ ...s, [field]: value, saveError: null, saveErrorFields: {} }))
  }

  function validate() {
    const errors: FieldErrors = {}
    if (!state.date.trim()) errors.date = 'Enter the transfer date.'
    if (!counterpartIdOf(state)) {
      errors[counterpartField(state.direction)] = `Choose the account to transfer ${state.direction}.`
    }
    const amount = parseValidatedNumber(state.amount)
    if (amount === null || amount <= 0) errors.amount = 'Transfer amount must be greater than zero.'
    const destinationAmount = destinationCurrency ? parseValidatedNumber(state.destinationAmount) : null
    if (destinationCurrency && (destinationAmount === null || destinationAmount <= 0)) {
      errors.destinationAmount = `Enter the amount received in ${destinationCurrency}.`
    }
    return { errors, amount: amount ?? 0, destinationAmount }
  }

  function submit() {
    const { errors, amount, destinationAmount } = validate()
    if (Object.keys(errors).length > 0) {
      setState((s) => ({ ...s, saveError: Object.values(errors)[0] ?? null, saveErrorFields: errors }))
      return
    }

    setState((s) => ({ ...s, isSaving: true, saveError: null, saveErrorFields: {} }))

    const payload = {
      date: state.date,
      sourceAccountId: state.sourceBank,
      destinationAccountId: state.destinationBank,
      amount,
      destinationAmount,
      note: state.note.trim() === '' ? null : state.note,
    }
    const { editingId } = state
    const request = editingId ? apiClient.updateAccountTransfer(editingId, payload) : apiClient.createAccountTransfer(payload)

    void request
      .then(() => {
        setStoredDefault(DATE_KEY, state.date)
        setStoredDefault(COUNTERPART_KEYS[state.direction], counterpartIdOf(state))
        setState(CLOSED_STATE)
        onSaved(editingId !== null)
      })
      .catch((err: unknown) => {
        const message = getSaveErrorMessage(err, 'Failed to save transfer')
        const mappedField = mapAccountTransferErrorToField(message, state.direction)
        const field = mappedField === 'destinationAmount' && !destinationCurrency ? null : mappedField
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
    isEditing: state.editingId !== null,
    direction: state.direction,
    lockedSide,
    date: state.date,
    sourceBank: state.sourceBank,
    destinationBank: state.destinationBank,
    amount: state.amount,
    destinationAmount: state.destinationAmount,
    note: state.note,
    counterpartOptions,
    sourceCurrency: sourceCurrency ?? null,
    destinationCurrency,
    isSaving: state.isSaving,
    isDirty: state.isOpen && hasChangedFields(state, state.openedValues, EDITABLE_FIELDS),
    saveError: state.saveError,
    saveErrorFields: state.saveErrorFields,
    openCreateForm,
    openEditForm,
    cancel,
    setField,
    submit,
  }
}
