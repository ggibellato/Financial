import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { ExternalPaymentDto } from '../api/types'
import { getCreateFormDate, setStoredDefault } from '../utils/createFormDefaults'
import { getSaveErrorMessage, hasAtMostTwoDecimals, positiveAmount } from '../utils/formatters'
import { hasChangedFields, type FieldErrors } from './inlineFormSlice'
import type { YearMonth } from './useMonthSelection'

const DATE_KEY = 'externalPayment.date'

const VALUE_FIELDS = ['date', 'description', 'amount'] as const

export type ExternalPaymentField = (typeof VALUE_FIELDS)[number]

export type PaymentDirection = 'In' | 'Out'

type ExternalPaymentValues = Record<ExternalPaymentField, string>

interface PaymentAccount {
  id: string
  name: string
  currency: string
}

interface ExternalPaymentFormState {
  direction: PaymentDirection
  editingId: string | null
  values: ExternalPaymentValues
  openedValues: ExternalPaymentValues | null
  isSaving: boolean
  saveError: string | null
  fieldErrors: FieldErrors<ExternalPaymentField>
}

const BLANK_VALUES: ExternalPaymentValues = { date: '', description: '', amount: '' }

const CLOSED_STATE: ExternalPaymentFormState = {
  direction: 'In',
  editingId: null,
  values: BLANK_VALUES,
  openedValues: null,
  isSaving: false,
  saveError: null,
  fieldErrors: {},
}

const NOUNS: Record<PaymentDirection, string> = { In: 'pay-in', Out: 'pay-out' }

function amountError(amount: string): string | undefined {
  return positiveAmount(amount) === null || !hasAtMostTwoDecimals(amount) ? 'Enter an amount above 0 with at most 2 decimal places.' : undefined
}

function validate(values: ExternalPaymentValues): FieldErrors<ExternalPaymentField> {
  return {
    date: values.date ? undefined : 'Enter a date.',
    description: values.description.trim() ? undefined : 'Enter a description.',
    amount: amountError(values.amount),
  }
}

function capitalized(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1)
}

export function useExternalPaymentForm(account: PaymentAccount | undefined, viewedMonth: YearMonth, onSaved: (message: string) => void) {
  const [state, setState] = useState<ExternalPaymentFormState>(CLOSED_STATE)
  const { direction, editingId, values, isSaving } = state
  const isEditing = editingId !== null
  const noun = NOUNS[direction]

  const openCreate = (opened: PaymentDirection) => {
    const fresh = { ...BLANK_VALUES, date: getCreateFormDate(DATE_KEY, viewedMonth) }
    setState({ ...CLOSED_STATE, direction: opened, values: fresh, openedValues: fresh })
  }

  const openEdit = (payment: ExternalPaymentDto) => {
    const opened = { date: payment.date, description: payment.description, amount: String(payment.amount) }
    setState({ ...CLOSED_STATE, direction: payment.direction === 'Out' ? 'Out' : 'In', editingId: payment.id, values: opened, openedValues: opened })
  }

  const cancel = () => setState(CLOSED_STATE)

  const setField = (field: ExternalPaymentField, value: string) =>
    setState((previous) => ({
      ...previous,
      values: { ...previous.values, [field]: value },
      saveError: null,
      fieldErrors: { ...previous.fieldErrors, [field]: undefined },
    }))

  const request = (accountId: string) => {
    const details = { date: values.date, description: values.description.trim(), amount: Number(values.amount) }
    return editingId === null
      ? apiClient.createExternalPayment(accountId, { ...details, direction })
      : apiClient.updateExternalPayment(accountId, editingId, details)
  }

  const submit = (): ExternalPaymentField | null => {
    if (isSaving || !account) return null
    const errors = validate(values)
    const firstInvalid = VALUE_FIELDS.find((field) => errors[field])
    if (firstInvalid) {
      setState((previous) => ({ ...previous, fieldErrors: errors, saveError: null }))
      return firstInvalid
    }

    setState((previous) => ({ ...previous, isSaving: true, saveError: null, fieldErrors: {} }))
    request(account.id)
      .then(() => {
        setStoredDefault(DATE_KEY, values.date)
        setState((previous) => {
          if (!previous.isSaving) return previous
          if (isEditing) return CLOSED_STATE
          const next = { ...previous.values, description: '', amount: '' }
          return { ...previous, isSaving: false, values: next, openedValues: next }
        })
        onSaved(`${capitalized(noun)} ${isEditing ? 'updated' : 'added'}`)
      })
      .catch((err: unknown) => {
        const message = getSaveErrorMessage(err, `Failed to save the ${noun}.`)
        setState((previous) => (previous.isSaving ? { ...previous, isSaving: false, saveError: message } : previous))
      })
    return null
  }

  return {
    isOpen: state.openedValues !== null,
    isEditing,
    direction,
    noun,
    accountName: account?.name ?? '',
    currency: account?.currency ?? '',
    values,
    isSaving,
    saveError: state.saveError,
    fieldErrors: state.fieldErrors,
    isDirty: hasChangedFields(values, state.openedValues, VALUE_FIELDS),
    openCreate,
    openEdit,
    cancel,
    setField,
    submit,
  }
}

export type ExternalPaymentFormModel = ReturnType<typeof useExternalPaymentForm>
