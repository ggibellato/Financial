import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountDetailDto, SettlementEntryDto } from '../api/types'
import { getCreateFormDate, setStoredDefault } from '../utils/createFormDefaults'
import { PAID_BY_OTHER, PAID_BY_YOU } from '../components/accountActions'
import { formatCurrency, getSaveErrorMessage, parseValidatedNumber, positiveAmount, toPence } from '../utils/formatters'
import { settlementCounterparty } from '../utils/settlementBalance'
import { hasChangedFields, type FieldErrors } from './inlineFormSlice'
import { paymentDateError } from './useCardStatements'

const DATE_KEY = 'settlementEntry.date'
const SETTLE_DESCRIPTION = 'Settlement'

const VALUE_FIELDS = ['date', 'paidBy', 'currency', 'description', 'amount', 'convertedValue', 'note'] as const

export type SettlementEntryField = (typeof VALUE_FIELDS)[number]

type SettlementEntryValues = Record<SettlementEntryField, string>

type SettlementEntryAccount = Pick<AccountDetailDto, 'id' | 'name' | 'currency' | 'currentBalances' | 'otherParticipant'>

type CurrencyBalances = SettlementEntryAccount['currentBalances']

interface SettlementEntryFormState {
  isSettling: boolean
  editingId: string | null
  convertedCurrency: string | null
  values: SettlementEntryValues
  openedValues: SettlementEntryValues | null
  isSaving: boolean
  saveError: string | null
  fieldErrors: FieldErrors<SettlementEntryField>
  warning: string | null
  confirmation: string | null
}

const BLANK_VALUES: SettlementEntryValues = { date: '', paidBy: '', currency: '', description: '', amount: '', convertedValue: '', note: '' }

const CLOSED_STATE: SettlementEntryFormState = {
  isSettling: false,
  editingId: null,
  convertedCurrency: null,
  values: BLANK_VALUES,
  openedValues: null,
  isSaving: false,
  saveError: null,
  fieldErrors: {},
  warning: null,
  confirmation: null,
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
  if (state.isSettling || (isEdit && !saved.warning)) return CLOSED_STATE
  const values = isEdit ? valuesOfEntry(saved) : { ...state.values, description: '', amount: '', note: '' }
  return { ...state, values, openedValues: values, isSaving: false, warning: saved.warning }
}

function otherCurrencyOf(balances: CurrencyBalances, currency: string): string | null {
  return balances.find((balance) => balance.currency !== currency)?.currency ?? null
}

function penceText(pence: number): string {
  return (Math.abs(pence) / 100).toFixed(2)
}

function settleFill(balances: CurrencyBalances, currency: string): Pick<SettlementEntryValues, 'paidBy' | 'amount' | 'convertedValue'> {
  const outstanding = toPence(balances.find((balance) => balance.currency === currency)?.balance ?? 0)
  const otherOutstanding = toPence(balances.find((balance) => balance.currency !== currency)?.balance ?? 0)
  if (outstanding === 0) return { paidBy: '', amount: '', convertedValue: '' }
  return {
    paidBy: outstanding > 0 ? PAID_BY_OTHER : PAID_BY_YOU,
    amount: penceText(outstanding),
    convertedValue: outstanding * otherOutstanding > 0 ? penceText(otherOutstanding) : '',
  }
}

function withSettleCurrency(state: SettlementEntryFormState, balances: CurrencyBalances, currency: string): SettlementEntryFormState {
  return { ...state, fieldErrors: {}, convertedCurrency: otherCurrencyOf(balances, currency), values: { ...state.values, currency, ...settleFill(balances, currency) } }
}

function remainingBalances(balances: CurrencyBalances, values: SettlementEntryValues): CurrencyBalances {
  const direction = values.paidBy === PAID_BY_YOU ? 1 : -1
  return balances.map(({ currency, balance }) => {
    const paid = Number(currency === values.currency ? values.amount : values.convertedValue)
    return { currency, balance: toPence(balance + direction * paid) / 100 }
  })
}

function amountsText(balances: CurrencyBalances): string {
  return balances.map(({ balance, currency }) => formatCurrency(Math.abs(balance), currency)).join(' / ')
}

function whoOwesText(otherParticipant: string, remaining: CurrencyBalances): string {
  const owedToYou = remaining.filter(({ balance }) => balance > 0)
  const owedByYou = remaining.filter(({ balance }) => balance < 0)
  if (owedToYou.length > 0 && owedByYou.length > 0) {
    return `${otherParticipant} owes you ${amountsText(owedToYou)}; you owe ${otherParticipant} ${amountsText(owedByYou)}`
  }
  return owedToYou.length > 0 ? `${otherParticipant} owes you` : `you owe ${otherParticipant}`
}

function settleOutcomeText(otherParticipant: string, balances: CurrencyBalances, values: SettlementEntryValues): string {
  const remaining = remainingBalances(balances, values)
  if (remaining.every(({ balance }) => balance === 0)) return balances.length > 1 ? 'Both totals become zero.' : 'The total becomes zero.'
  return `This leaves ${amountsText(remaining)} outstanding (${whoOwesText(otherParticipant, remaining)}).`
}

function impliedRateText(values: SettlementEntryValues, convertedCurrency: string | null): string | null {
  if (!convertedCurrency) return null
  const paid = { currency: values.currency, value: Number(values.amount) }
  const converted = { currency: convertedCurrency, value: Number(values.convertedValue) }
  if (!(paid.value > 0) || !(converted.value > 0)) return null
  const [unit, quoted] = paid.value <= converted.value ? [paid, converted] : [converted, paid]
  return `Implied rate: 1 ${unit.currency} = ${formatCurrency(quoted.value / unit.value, quoted.currency)}`
}

function settlementSavedMessage(warning: string | null): string {
  return warning ? `Settlement saved. ${warning}` : 'Settlement saved'
}

export function useSettlementEntryForm(
  account: SettlementEntryAccount | null,
  viewedMonth: { year: number; month: number },
  onSaved: (message: string) => void,
) {
  const [state, setState] = useState<SettlementEntryFormState>(CLOSED_STATE)
  const { isSettling, values, editingId, convertedCurrency, isSaving } = state
  const balances = account?.currentBalances ?? []
  const currencies = balances.map((balance) => balance.currency)
  const isEditing = editingId !== null
  const otherParticipant = account ? settlementCounterparty(account) : ''
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

  const openSettle = () => {
    if (!account) return
    const currency = balances.find((balance) => toPence(balance.balance) !== 0)?.currency ?? account.currency
    const blank = { ...BLANK_VALUES, date: getCreateFormDate(DATE_KEY, viewedMonth), description: SETTLE_DESCRIPTION }
    const opened = withSettleCurrency({ ...CLOSED_STATE, isSettling: true, values: blank }, balances, currency)
    setState({ ...opened, openedValues: opened.values })
  }

  const cancel = () => setState(CLOSED_STATE)

  const setField = (field: SettlementEntryField, value: string) =>
    setState((previous) => {
      const next = previous.isSettling && field === 'currency' ? withSettleCurrency(previous, balances, value) : previous
      return {
        ...next,
        values: { ...next.values, [field]: value },
        saveError: null,
        warning: null,
        fieldErrors: { ...next.fieldErrors, [field]: undefined },
      }
    })

  const request = (accountId: string) => {
    const details = {
      date: values.date,
      description: values.description.trim(),
      note: values.note.trim() === '' ? null : values.note,
      paidBy: values.paidBy,
      amount: Number(values.amount),
    }
    const convertedValue = convertedCurrency && values.convertedValue.trim() !== '' ? Number(values.convertedValue) : null
    if (isSettling) return apiClient.settleAccount(accountId, { ...details, currency: values.currency, convertedValue })
    if (editingId === null) return apiClient.createSettlementEntry(accountId, { ...details, currency: values.currency })
    return apiClient.updateSettlementEntry(accountId, editingId, { ...details, convertedValue })
  }

  const save = (accountId: string) => {
    setState((previous) => ({ ...previous, isSaving: true, saveError: null, fieldErrors: {}, warning: null, confirmation: null }))
    request(accountId)
      .then((saved) => {
        setStoredDefault(DATE_KEY, values.date)
        setState((previous) => (previous.isSaving ? afterSave(previous, saved) : previous))
        onSaved(isSettling ? settlementSavedMessage(saved.warning) : isEditing ? 'Entry updated' : 'Entry added')
      })
      .catch((err: unknown) => {
        const message = getSaveErrorMessage(err, isSettling ? 'Failed to save the settlement' : 'Failed to save the entry')
        setState((previous) => (previous.isSaving ? { ...previous, isSaving: false, saveError: message } : previous))
      })
  }

  const submit = (): SettlementEntryField | null => {
    if (isSaving || !account) return null
    const errors = validate(values, asksCurrency, convertedCurrency !== null)
    const firstInvalid = VALUE_FIELDS.find((field) => errors[field])
    if (firstInvalid) {
      setState((previous) => ({ ...previous, fieldErrors: errors, saveError: null }))
      return firstInvalid
    }

    if (isSettling) {
      const confirmation = settleOutcomeText(otherParticipant, balances, values)
      setState((previous) => ({ ...previous, fieldErrors: {}, saveError: null, confirmation }))
    } else {
      save(account.id)
    }
    return null
  }

  const confirmSettle = () => {
    if (account && state.confirmation !== null) save(account.id)
  }

  const cancelConfirmation = () => setState((previous) => ({ ...previous, confirmation: null }))

  return {
    isOpen: state.openedValues !== null,
    isEditing,
    isSettling,
    values,
    currencies,
    otherParticipant,
    asksCurrency,
    convertedCurrency,
    isSaving,
    saveError: state.saveError,
    fieldErrors: state.fieldErrors,
    warning: state.warning,
    impliedRate: isSettling ? impliedRateText(values, convertedCurrency) : null,
    confirmation: state.confirmation,
    isDirty: hasChangedFields(values, state.openedValues, VALUE_FIELDS),
    openAdd,
    openEdit,
    openSettle,
    cancel,
    setField,
    submit,
    confirmSettle,
    cancelConfirmation,
  }
}

export type SettlementEntryFormModel = ReturnType<typeof useSettlementEntryForm>
