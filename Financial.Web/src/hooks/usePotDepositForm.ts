import { useRef, useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { PotDepositRequestDto, PotOperationDto, TransferAccountOptionDto } from '../api/types'
import { getCreateFormDate, setStoredDefault } from '../utils/createFormDefaults'
import { getSaveErrorMessage, todayIsoDate } from '../utils/formatters'
import {
  fundingLineErrorKey,
  fundingLinesMismatch,
  MAX_FUNDING_LINES,
  positiveAmount,
  remainingAmount,
  type FundingLineValues,
} from '../utils/fundingLines'
import type { YearMonth } from './useMonthSelection'
import { usePots } from './usePots'

const DATE_KEY = 'potDeposit.date'
const NO_ERRORS: Partial<Record<string, string>> = {}

export type DepositInto = 'ActivePots' | 'OnePot'
export type PotDepositField = 'potId' | 'amount' | 'date' | 'description'

interface DepositValues {
  into: DepositInto
  potId: string
  amount: string
  date: string
  description: string
  hasNoBank: boolean
  lines: FundingLineValues[]
}

interface DepositFormState {
  operationId: string | null
  opened: DepositValues
  values: DepositValues
  isSaving: boolean
  error: string | null
  errorFields: Partial<Record<string, string>>
}

function comparableValues({ lines, ...values }: DepositValues): string {
  return JSON.stringify({ ...values, lines: lines.map(({ bankId, amount }) => ({ bankId, amount })) })
}

function validate(values: DepositValues, amount: number | null): Partial<Record<string, string>> {
  const errors: Partial<Record<string, string>> = {}
  if (!values.date.trim()) errors.date = 'Enter the deposit date.'
  else if (values.date > todayIsoDate()) errors.date = 'Date must not be in the future.'
  if (values.into === 'OnePot' && !values.potId) errors.potId = 'Choose a pot.'
  if (amount === null) errors.amount = 'Amount must be greater than zero.'
  if (!values.description.trim()) errors.description = 'Enter a description.'
  if (values.hasNoBank) return errors
  for (const line of values.lines) {
    if (!line.bankId) errors[fundingLineErrorKey(line, 'bankId')] = 'Choose a bank.'
    if (positiveAmount(line.amount) === null) errors[fundingLineErrorKey(line, 'amount')] = 'Amount must be greater than zero.'
  }
  return errors
}

function toRequest(values: DepositValues, amount: number): PotDepositRequestDto {
  return {
    amount,
    date: values.date,
    description: values.description.trim(),
    potId: values.into === 'OnePot' ? values.potId : null,
    fundingLines: values.hasNoBank ? [] : values.lines.map((line) => ({ bankId: line.bankId, amount: Number(line.amount) })),
  }
}

export function usePotDepositForm(potsAccount: TransferAccountOptionDto | undefined, viewedMonth: YearMonth, onSaved: () => void) {
  const [form, setForm] = useState<DepositFormState | null>(null)
  const lineCount = useRef(0)
  const pots = usePots(form && potsAccount ? potsAccount.id : null, viewedMonth.year, viewedMonth.month)
  const currency = potsAccount?.currency ?? ''
  const depositAmount = form ? positiveAmount(form.values.amount) : null
  const mismatch = form && potsAccount && !form.values.hasNoBank ? fundingLinesMismatch(form.values.lines, depositAmount, currency) : null

  const newLine = (bankId = '', amount = ''): FundingLineValues => {
    lineCount.current += 1
    return { key: `line-${lineCount.current}`, bankId, amount }
  }

  const show = (operationId: string | null, values: DepositValues) =>
    setForm({ operationId, opened: values, values, isSaving: false, error: null, errorFields: {} })

  const open = () =>
    show(null, {
      into: 'ActivePots',
      potId: '',
      amount: '',
      date: getCreateFormDate(DATE_KEY, viewedMonth),
      description: '',
      hasNoBank: false,
      lines: [newLine()],
    })

  const openEdit = (operation: PotOperationDto) => {
    const { fundingLines } = operation
    show(operation.operationId, {
      into: operation.potId ? 'OnePot' : 'ActivePots',
      potId: operation.potId ?? '',
      amount: String(operation.amount),
      date: operation.date,
      description: operation.description,
      hasNoBank: fundingLines.length === 0,
      lines: fundingLines.length > 0 ? fundingLines.map((line) => newLine(line.bankId, String(line.amount))) : [newLine()],
    })
  }

  const cancel = () => setForm(null)

  const update = (change: (values: DepositValues) => DepositValues) =>
    setForm((current) => current && { ...current, values: change(current.values), error: null, errorFields: {} })

  const setField = <F extends 'into' | PotDepositField>(field: F, value: DepositValues[F]) =>
    update((values) => {
      const onlyLine = values.lines.length === 1 ? values.lines[0] : null
      const lineFollowsAmount = field === 'amount' && onlyLine?.amount === values.amount
      return {
        ...values,
        [field]: value,
        lines: lineFollowsAmount && onlyLine ? [{ ...onlyLine, amount: value as string }] : values.lines,
      }
    })

  const setHasNoBank = (hasNoBank: boolean) => update((values) => ({ ...values, hasNoBank }))

  const changeLine = (key: string, change: Partial<Pick<FundingLineValues, 'bankId' | 'amount'>>) =>
    update((values) => ({ ...values, lines: values.lines.map((line) => (line.key === key ? { ...line, ...change } : line)) }))

  const addLine = () =>
    update((values) =>
      values.lines.length >= MAX_FUNDING_LINES
        ? values
        : { ...values, lines: [...values.lines, newLine('', remainingAmount(values.lines, positiveAmount(values.amount)))] },
    )

  const removeLine = (key: string) => update((values) => ({ ...values, lines: values.lines.filter((line) => line.key !== key) }))

  const submit = () => {
    if (!form || form.isSaving || !potsAccount || mismatch) return
    const errorFields = validate(form.values, depositAmount)
    if (Object.keys(errorFields).length > 0 || depositAmount === null) {
      setForm({ ...form, error: null, errorFields })
      return
    }

    setForm({ ...form, isSaving: true, error: null, errorFields: {} })
    const request = toRequest(form.values, depositAmount)
    const saving = form.operationId
      ? apiClient.updatePotDeposit(potsAccount.id, form.operationId, request)
      : apiClient.createPotDeposit(potsAccount.id, request)
    void saving
      .then(() => {
        setStoredDefault(DATE_KEY, form.values.date)
        setForm(null)
        onSaved()
      })
      .catch((err: unknown) => {
        setForm((current) => current && { ...current, isSaving: false, error: getSaveErrorMessage(err, 'Failed to save the deposit') })
      })
  }

  return {
    isOpen: form !== null,
    isEditing: form?.operationId != null,
    operationId: form?.operationId ?? null,
    values: form?.values ?? null,
    openedBankIds: form?.opened.lines.map((line) => line.bankId) ?? [],
    currency,
    depositAmount,
    mismatch,
    pots: pots.data?.pots ?? null,
    hasPotsError: pots.error !== null,
    retryPots: pots.retry,
    isSaving: form?.isSaving ?? false,
    isDirty: form !== null && comparableValues(form.values) !== comparableValues(form.opened),
    error: form?.error ?? null,
    errorFields: form?.errorFields ?? NO_ERRORS,
    open,
    openEdit,
    cancel,
    setField,
    setHasNoBank,
    changeLine,
    addLine,
    removeLine,
    submit,
  }
}

export type PotDepositFormModel = ReturnType<typeof usePotDepositForm>
