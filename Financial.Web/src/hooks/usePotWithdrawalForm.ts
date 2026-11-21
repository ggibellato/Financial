import { useState } from 'react'
import { isConflict, overdraftBalance } from '../api/apiError'
import { apiClient } from '../api/financialApiClient'
import type { AccountDto, CategoryDto, PotOperationDto, PotWithdrawalRequestDto } from '../api/types'
import { accountsWith } from '../utils/accountCapabilities'
import { getCreateFormDate, setStoredDefault } from '../utils/createFormDefaults'
import { formatCurrency, formatMonthInputValue, getSaveErrorMessage, parseMonthInputValue, positiveAmount, toPence } from '../utils/formatters'
import { accountCategoryIds, defaultExpenseCategoryId, eligibleWithdrawalCategories } from '../utils/withdrawalExpenseCategories'
import { hasChangedFields, type FieldErrors } from './inlineFormSlice'
import type { YearMonth } from './useMonthSelection'
import { usePots } from './usePots'
import { useStatementMonthOptions } from './useStatementMonthOptions'
import { futureDateError } from '../utils/validators'

const DATE_KEY = 'potWithdrawal.date'
const MINUS_SIGN = '−'
const INVESTMENT_WITHDRAWAL_DONE = 'Done'

const CATEGORY_REQUIRED: Record<PaidWith, string | null> = {
  none: null,
  bank: 'Category is required when a bank is selected.',
  card: 'Category is required when a credit card is selected.',
}

const FIELDS = ['potsAccountId', 'potId', 'date', 'description', 'amount', 'paidWith', 'bankId', 'creditCardId', 'statementMonth', 'categoryId'] as const

export type PotWithdrawalField = (typeof FIELDS)[number]

export type PaidWith = 'none' | 'bank' | 'card'

type WithdrawalValues = Record<PotWithdrawalField, string>

export interface InvestmentWithdrawal {
  status: string
  bankId: string | null
  date: string | null
}

interface WithdrawalFormState {
  operationId: string | null
  isPaymentLocked: boolean
  investmentWithdrawal: InvestmentWithdrawal | null
  opened: WithdrawalValues
  values: WithdrawalValues
  isSaving: boolean
  error: string | null
  errorFields: FieldErrors<PotWithdrawalField>
  overdraftMessage: string | null
}

const NO_ERRORS: FieldErrors<PotWithdrawalField> = {}

const PAYMENT_FIELDS_CLEARED: Pick<WithdrawalValues, 'bankId' | 'creditCardId' | 'statementMonth' | 'categoryId'> = {
  bankId: '',
  creditCardId: '',
  statementMonth: '',
  categoryId: '',
}

function paidWithIn(values: WithdrawalValues): PaidWith {
  return values.paidWith === 'bank' || values.paidWith === 'card' ? values.paidWith : 'none'
}

function validate(values: WithdrawalValues): FieldErrors<PotWithdrawalField> {
  const errors: FieldErrors<PotWithdrawalField> = {}
  if (!values.potId) errors.potId = 'Choose a pot.'
  const dateError = values.date.trim() ? futureDateError(values.date) : 'Enter the withdrawal date.'
  if (dateError) errors.date = dateError
  if (!values.description.trim()) errors.description = 'Enter a description.'
  if (positiveAmount(values.amount) === null) errors.amount = 'Amount must be greater than zero.'
  if (values.paidWith === 'bank' && !values.bankId) errors.bankId = 'Choose a bank.'
  if (values.paidWith === 'card') {
    if (!values.creditCardId) errors.creditCardId = 'Choose a card.'
    if (!values.statementMonth) errors.statementMonth = 'Choose a statement.'
  }
  const categoryRequired = CATEGORY_REQUIRED[paidWithIn(values)]
  if (categoryRequired && !values.categoryId) errors.categoryId = categoryRequired
  return errors
}

function toRequest(values: WithdrawalValues, confirmed: boolean): PotWithdrawalRequestDto {
  const statement = values.paidWith === 'card' ? parseMonthInputValue(values.statementMonth) : null
  return {
    potId: values.potId,
    amount: Number(values.amount),
    date: values.date,
    description: values.description.trim(),
    bankId: values.paidWith === 'bank' ? values.bankId : null,
    creditCardId: values.paidWith === 'card' ? values.creditCardId : null,
    statementYear: statement?.year ?? null,
    statementMonth: statement?.month ?? null,
    categoryId: values.paidWith === 'none' ? null : values.categoryId,
    confirmed,
  }
}

function paidWithOf(operation: PotOperationDto): PaidWith {
  if (operation.creditCardId) return 'card'
  return operation.bankId ? 'bank' : 'none'
}

function investmentWithdrawalOf(operation: PotOperationDto): InvestmentWithdrawal | null {
  return operation.investmentWithdrawal
    ? { status: operation.investmentWithdrawal, bankId: operation.investmentWithdrawalBankId, date: operation.investmentWithdrawalDate }
    : null
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
  const statementMonths = useStatementMonthOptions(
    form?.values.paidWith === 'card' ? form.values.creditCardId : '',
    form?.values.date ?? '',
    form?.opened.statementMonth ?? '',
  )

  const potId = form ? form.values.potId || (pots?.find((pot) => pot.isActive)?.id ?? '') : ''
  const potName = pots?.find((pot) => pot.id === potId)?.name ?? ''
  const isStatementOffered = (month: string) => statementMonths.options.some((option) => option.value === month)
  const values: WithdrawalValues | null = form && {
    ...form.values,
    potId,
    statementMonth: isStatementOffered(form.values.statementMonth) ? form.values.statementMonth : (statementMonths.options[0]?.value ?? ''),
    categoryId: form.values.categoryId || (form.values.paidWith === 'none' ? '' : defaultExpenseCategoryId(eligibleCategories, potName)),
  }

  const show = (operationId: string | null, isPaymentLocked: boolean, opened: WithdrawalValues, investmentWithdrawal: InvestmentWithdrawal | null = null) =>
    setForm({ operationId, isPaymentLocked, investmentWithdrawal, opened, values: opened, isSaving: false, error: null, errorFields: {}, overdraftMessage: null })

  const openNew = (paidWith: PaidWith, isPaymentLocked: boolean, payment: Partial<WithdrawalValues>, potsAccountId: string) =>
    show(null, isPaymentLocked, {
      potsAccountId,
      potId: '',
      date: getCreateFormDate(DATE_KEY, viewedMonth),
      description: '',
      amount: '',
      paidWith,
      ...PAYMENT_FIELDS_CLEARED,
      ...payment,
    })

  const firstPotsAccountId = () => accountsWith(accounts, 'HasPots')[0]?.id ?? ''

  const openFromPotsAccount = (potsAccountId: string) => openNew('none', false, {}, potsAccountId)

  const openFromBank = (bankId: string) => openNew('bank', true, { bankId }, firstPotsAccountId())

  const openFromCard = (creditCardId: string) => openNew('card', true, { creditCardId }, firstPotsAccountId())

  const openEdit = (operation: PotOperationDto) =>
    show(
      operation.operationId,
      false,
      {
        potsAccountId: operation.potsAccountId,
        potId: operation.potId ?? '',
        date: operation.date,
        description: operation.description,
        amount: String(operation.amount),
        paidWith: paidWithOf(operation),
        bankId: operation.bankId ?? '',
        creditCardId: operation.creditCardId ?? '',
        statementMonth: operation.statementYear && operation.statementMonth ? formatMonthInputValue(operation.statementYear, operation.statementMonth) : '',
        categoryId: operation.categoryId ?? '',
      },
      investmentWithdrawalOf(operation),
    )

  const cancel = () => setForm(null)

  const setField = (field: PotWithdrawalField, value: string) =>
    setForm(
      (current) =>
        current && {
          ...current,
          values: {
            ...current.values,
            ...(field === 'paidWith' ? PAYMENT_FIELDS_CLEARED : {}),
            [field]: value,
            ...(field === 'potsAccountId' ? { potId: '' } : {}),
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
              error: overdraft ? null : getSaveErrorMessage(err, 'Failed to save the withdrawal.'),
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
    isPaymentLocked: form?.isPaymentLocked ?? false,
    isStatementPaidLocked: form?.investmentWithdrawal?.status === INVESTMENT_WITHDRAWAL_DONE,
    paidWith: values ? paidWithIn(values) : 'none',
    investmentWithdrawal: form?.investmentWithdrawal ?? null,
    operationId: form?.operationId ?? null,
    values,
    openedBankId: form?.opened.bankId ?? '',
    openedCategoryId: form?.opened.categoryId ?? '',
    potsAccounts: accountsWith(accounts, 'HasPots'),
    cards: accountsWith(accounts, 'HasStatements', form?.opened.creditCardId),
    categories: eligibleCategories,
    statementMonths,
    currency: potsRead.data?.currency ?? '',
    pots,
    potName,
    hasPotsError: potsRead.error !== null,
    retryPots: potsRead.retry,
    isSaving: form?.isSaving ?? false,
    isDirty: form !== null && hasChangedFields(form.values, form.opened, FIELDS),
    error: form?.error ?? null,
    errorFields: form?.errorFields ?? NO_ERRORS,
    overdraftMessage: form?.overdraftMessage ?? null,
    openFromPotsAccount,
    openFromBank,
    openFromCard,
    openEdit,
    cancel,
    setField,
    submit,
    confirmOverdraft,
    cancelOverdraft,
  }
}

export type PotWithdrawalFormModel = ReturnType<typeof usePotWithdrawalForm>
