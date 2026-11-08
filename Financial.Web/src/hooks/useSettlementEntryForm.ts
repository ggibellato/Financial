import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountDetailDto, SettlementEntryDto } from '../api/types'
import { getCreateFormDate, setStoredDefault } from '../utils/createFormDefaults'
import { getSaveErrorMessage, parseValidatedNumber, positiveAmount, toPence } from '../utils/formatters'
import { settlementCounterparty } from '../utils/settlementBalance'
import { hasChangedFields, type FieldErrors } from './inlineFormSlice'
import { paymentDateError } from './useCardStatements'

const DATE_KEY = 'settlementEntry.date'

const VALUE_FIELDS = ['date', 'paidBy', 'currency', 'description', 'amount', 'convertedValue', 'note'] as const

export type SettlementEntryField = (typeof VALUE_FIELDS)[number]

type SettlementEntryValues = Record<SettlementEntryField, string>

type SettlementEntryAccount = Pick<AccountDetailDto, 'id' | 'name' | 'currency' | 'currentBalances' | 'otherParticipant'>

interface SettlementEntryFormState {
  editingId: string | null
  convertedCurrency: string | null
  values: SettlementEntryValues
  openedValues: SettlementEntryValues | null
  isSaving: boolean
  saveError: string | null
  fieldErrors: FieldErrors<SettlementEntryField>
  warning: string | null
}

const BLANK_VALUES: SettlementEntryValues = { date: '', paidBy: '', currency: '', description: '', amount: '', convertedValue: '', note: '' }

const CLOSED_STATE: SettlementEntryFormState = {
  editingId: null,
  convertedCurrency: null,
  values: BLANK_VALUES,
  openedValues: null,
  isSaving: false,
  saveError: null,
  fieldErrors: {},
  warning: null,
}

function valuesOfEntry(entry: SettlementEntryDto): SettlementEntryValues {
  return {
    date: entry.date,
    paidBy: entry.paidBy,
    currency: entry.currency,
    description: entry.description,
    amount: String(entry.amount),
    convertedValue: entry.convertedValue === null ? '' : String(entry.convertedValue),
    note: entry.note,
  }
}

function amountError(amount: string): string | undefined {
  const parsed = positiveAmount(amount)
  return parsed === null || toPence(parsed) === 0 ? 'Amount must be greater than zero.' : undefined
}

function convertedValueError(convertedValue: string): string | undefined {
  return convertedValue.trim() !== '' && parseValidatedNumber(convertedValue, { min: 0 }) === null
    ? 'The converted value must be greater than zero.'
    : undefined
}

function validate(values: SettlementEntryValues, asksCurrency: boolean, hasConvertedValue: boolean): FieldErrors<SettlementEntryField> {
  return {
    date: paymentDateError(values.date),
    paidBy: values.paidBy ? undefined : 'Choose who paid.',
    currency: asksCurrency && !values.currency ? 'Choose the currency.' : undefined,
    description: values.description.trim() ? undefined : 'Description is required.',
    amount: amountError(values.amount),
    convertedValue: hasConvertedValue ? convertedValueError(values.convertedValue) : undefined,
  }
}

function afterSave(state: SettlementEntryFormState, saved: SettlementEntryDto): SettlementEntryFormState {
  const isEdit = state.editingId !== null
  if (isEdit && !saved.warning) return CLOSED_STATE
  const values = isEdit ? valuesOfEntry(saved) : { ...state.values, description: '', amount: '', note: '' }
  return { ...state, values, openedValues: values, isSaving: false, warning: saved.warning }
}

export function useSettlementEntryForm(
  account: SettlementEntryAccount | null,
  viewedMonth: { year: number; month: number },
  onSaved: (message: string) => void,
) {
  const [state, setState] = useState<SettlementEntryFormState>(CLOSED_STATE)
  const { values, editingId, convertedCurrency, isSaving } = state
  const currencies = account?.currentBalances.map((balance) => balance.currency) ?? []
  const isEditing = editingId !== null
  const asksCurrency = !isEditing && currencies.length > 1

  const openAdd = () => {
    if (!account) return
    const opened = { ...BLANK_VALUES, date: getCreateFormDate(DATE_KEY, viewedMonth), currency: account.currency }
    setState({ ...CLOSED_STATE, values: opened, openedValues: opened })
  }

  const openEdit = (entry: SettlementEntryDto) => {
    const opened = valuesOfEntry(entry)
    setState({ ...CLOSED_STATE, editingId: entry.id, convertedCurrency: entry.convertedCurrency, values: opened, openedValues: opened })
  }

  const cancel = () => setState(CLOSED_STATE)

  const setField = (field: SettlementEntryField, value: string) =>
    setState((previous) => ({
      ...previous,
      values: { ...previous.values, [field]: value },
      saveError: null,
      warning: null,
      fieldErrors: { ...previous.fieldErrors, [field]: undefined },
    }))

  const request = (accountId: string) => {
    const details = {
      date: values.date,
      description: values.description.trim(),
      note: values.note.trim() === '' ? null : values.note,
      paidBy: values.paidBy,
      amount: Number(values.amount),
    }
    if (editingId === null) return apiClient.createSettlementEntry(accountId, { ...details, currency: values.currency })
    const convertedValue = convertedCurrency && values.convertedValue.trim() !== '' ? Number(values.convertedValue) : null
    return apiClient.updateSettlementEntry(accountId, editingId, { ...details, convertedValue })
  }

  const submit = (): SettlementEntryField | null => {
    if (isSaving || !account) return null
    const errors = validate(values, asksCurrency, convertedCurrency !== null)
    const firstInvalid = VALUE_FIELDS.find((field) => errors[field])
    if (firstInvalid) {
      setState((previous) => ({ ...previous, fieldErrors: errors, saveError: null }))
      return firstInvalid
    }

    setState((previous) => ({ ...previous, isSaving: true, saveError: null, fieldErrors: {}, warning: null }))
    request(account.id)
      .then((saved) => {
        setStoredDefault(DATE_KEY, values.date)
        setState((previous) => (previous.isSaving ? afterSave(previous, saved) : previous))
        onSaved(isEditing ? 'Entry updated' : 'Entry added')
      })
      .catch((err: unknown) => {
        setState((previous) => (previous.isSaving ? { ...previous, isSaving: false, saveError: getSaveErrorMessage(err, 'Failed to save the entry') } : previous))
      })
    return null
  }

  return {
    isOpen: state.openedValues !== null,
    isEditing,
    values,
    currencies,
    otherParticipant: account ? settlementCounterparty(account) : '',
    asksCurrency,
    convertedCurrency,
    isSaving,
    saveError: state.saveError,
    fieldErrors: state.fieldErrors,
    warning: state.warning,
    isDirty: hasChangedFields(values, state.openedValues, VALUE_FIELDS),
    openAdd,
    openEdit,
    cancel,
    setField,
    submit,
  }
}

export type SettlementEntryFormModel = ReturnType<typeof useSettlementEntryForm>
