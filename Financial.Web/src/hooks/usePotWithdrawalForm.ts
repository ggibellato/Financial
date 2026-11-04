import { useState } from 'react'
import { isConflict, overdraftBalance } from '../api/apiError'
import { apiClient } from '../api/financialApiClient'
import type { AccountDto, CategoryDto, PotOperationDto, PotWithdrawalRequestDto } from '../api/types'
import { accountsWith } from '../utils/accountCapabilities'
import { getCreateFormDate, setStoredDefault } from '../utils/createFormDefaults'
import { formatCurrency, getSaveErrorMessage, positiveAmount, todayIsoDate, toPence } from '../utils/formatters'
import { accountCategoryIds, defaultExpenseCategoryId, eligibleWithdrawalCategories } from '../utils/withdrawalExpenseCategories'
import { hasChangedFields, type FieldErrors } from './inlineFormSlice'
import type { YearMonth } from './useMonthSelection'
import { usePots } from './usePots'

const DATE_KEY = 'potWithdrawal.date'
const MINUS_SIGN = '−'

const FIELDS = ['potsAccountId', 'potId', 'date', 'description', 'amount', 'bankId', 'categoryId'] as const

export type PotWithdrawalField = (typeof FIELDS)[number]

type WithdrawalValues = Record<PotWithdrawalField, string>

interface WithdrawalFormState {
  operationId: string | null
  isBankLocked: boolean
  opened: WithdrawalValues
  values: WithdrawalValues
  isSaving: boolean
  error: string | null
  errorFields: FieldErrors<PotWithdrawalField>
  overdraftMessage: string | null
}

const NO_ERRORS: FieldErrors<PotWithdrawalField> = {}

function validate(values: WithdrawalValues): FieldErrors<PotWithdrawalField> {
  const errors: FieldErrors<PotWithdrawalField> = {}
  if (!values.potId) errors.potId = 'Choose a pot.'
  if (!values.date.trim()) errors.date = 'Enter the withdrawal date.'
  else if (values.date > todayIsoDate()) errors.date = 'Date must not be in the future.'
  if (!values.description.trim()) errors.description = 'Enter a description.'
  if (positiveAmount(values.amount) === null) errors.amount = 'Amount must be greater than zero.'
  if (values.bankId && !values.categoryId) errors.categoryId = 'Category is required when a bank is selected.'
  return errors
}

function toRequest(values: WithdrawalValues, confirmed: boolean): PotWithdrawalRequestDto {
  return {
    potId: values.potId,
    amount: Number(values.amount),
    date: values.date,
    description: values.description.trim(),
    bankId: values.bankId || null,
    categoryId: values.bankId ? values.categoryId : null,
    confirmed,
  }
}

export function overdraftMessage(balance: number | null, potName: string, amount: number, currency: string): string {
  if (balance === null) return `This takes ${potName} below zero. Continue?`
  const below = (toPence(amount) - toPence(balance)) / 100
  return `This takes ${potName} below zero (${MINUS_SIGN}${formatCurrency(below, currency)}). Continue?`
}

export function usePotWithdrawalForm(
  accounts: AccountDto[],
  categories: CategoryDto[],
  viewedMonth: YearMonth,
  onSaved: () => void,
) {
  const [form, setForm] = useState<WithdrawalFormState | null>(null)
  const potsRead = usePots(form?.values.potsAccountId || null, viewedMonth.year, viewedMonth.month)
  const pots = potsRead.data?.pots ?? null
  const eligibleCategories = eligibleWithdrawalCategories(categories, accountCategoryIds(accounts))

  const potId = form ? form.values.potId || (pots?.find((pot) => pot.isActive)?.id ?? '') : ''
  const potName = pots?.find((pot) => pot.id === potId)?.name ?? ''
  const values: WithdrawalValues | null = form && {
    ...form.values,
    potId,
    categoryId: form.values.categoryId || (form.values.bankId ? defaultExpenseCategoryId(eligibleCategories, potName) : ''),
  }

  const show = (operationId: string | null, isBankLocked: boolean, opened: WithdrawalValues) =>
    setForm({ operationId, isBankLocked, opened, values: opened, isSaving: false, error: null, errorFields: {}, overdraftMessage: null })

  const openNew = (potsAccountId: string, bankId: string, isBankLocked: boolean) =>
    show(null, isBankLocked, {
      potsAccountId,
      potId: '',
      date: getCreateFormDate(DATE_KEY, viewedMonth),
      description: '',
      amount: '',
      bankId,
      categoryId: '',
    })

  const openFromPotsAccount = (potsAccountId: string) => openNew(potsAccountId, '', false)

  const openFromBank = (bankId: string) => openNew(accountsWith(accounts, 'HasPots')[0]?.id ?? '', bankId, true)

  const openEdit = (operation: PotOperationDto) =>
    show(operation.operationId, false, {
      potsAccountId: operation.potsAccountId,
      potId: operation.potId ?? '',
      date: operation.date,
      description: operation.description,
      amount: String(operation.amount),
      bankId: operation.bankId ?? '',
      categoryId: operation.categoryId ?? '',
    })

  const cancel = () => setForm(null)

  const setField = (field: PotWithdrawalField, value: string) =>
    setForm(
      (current) =>
        current && {
          ...current,
          values: {
            ...current.values,
            [field]: value,
            ...(field === 'potsAccountId' ? { potId: '' } : {}),
            ...(field === 'bankId' && value === '' ? { categoryId: '' } : {}),
          },
          error: null,
          errorFields: {},
        },
    )

  const save = (saved: WithdrawalFormState, savedValues: WithdrawalValues, confirmed: boolean) => {
    const request = toRequest(savedValues, confirmed)
    const saving = saved.operationId
      ? apiClient.updatePotWithdrawal(savedValues.potsAccountId, saved.operationId, request)
      : apiClient.createPotWithdrawal(savedValues.potsAccountId, request)
    void saving
      .then(() => {
        setStoredDefault(DATE_KEY, savedValues.date)
        setForm(null)
        onSaved()
      })
      .catch((err: unknown) => {
        const overdraft =
          !confirmed && isConflict(err)
            ? overdraftMessage(overdraftBalance(err), potName, request.amount, potsRead.data?.currency ?? '')
            : null
        setForm(
          (current) =>
            current && {
              ...current,
              isSaving: false,
              overdraftMessage: overdraft,
              error: overdraft ? null : getSaveErrorMessage(err, 'Failed to save the withdrawal'),
            },
        )
      })
  }

  const submit = () => {
    if (!form || !values || form.isSaving || pots === null) return
    const errorFields = validate(values)
    if (Object.keys(errorFields).length > 0) {
      setForm({ ...form, error: null, errorFields })
      return
    }

    setForm({ ...form, isSaving: true, error: null, errorFields: {} })
    save(form, values, false)
  }

  const confirmOverdraft = () => {
    if (!form || !values || form.isSaving) return
    setForm({ ...form, isSaving: true })
    save(form, values, true)
  }

  const cancelOverdraft = () => setForm((current) => current && { ...current, overdraftMessage: null })

  return {
    isOpen: form !== null,
    isEditing: form?.operationId != null,
    isBankLocked: form?.isBankLocked ?? false,
    operationId: form?.operationId ?? null,
    values,
    openedBankId: form?.opened.bankId ?? '',
    openedCategoryId: form?.opened.categoryId ?? '',
    potsAccounts: accountsWith(accounts, 'HasPots'),
    categories: eligibleCategories,
    currency: potsRead.data?.currency ?? '',
    pots,
    hasPotsError: potsRead.error !== null,
    retryPots: potsRead.retry,
    isSaving: form?.isSaving ?? false,
    isDirty: form !== null && hasChangedFields(form.values, form.opened, FIELDS),
    error: form?.error ?? null,
    errorFields: form?.errorFields ?? NO_ERRORS,
    overdraftMessage: form?.overdraftMessage ?? null,
    openFromPotsAccount,
    openFromBank,
    openEdit,
    cancel,
    setField,
    submit,
    confirmOverdraft,
    cancelOverdraft,
  }
}

export type PotWithdrawalFormModel = ReturnType<typeof usePotWithdrawalForm>
