import { useRef, useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { PotDepositRequestDto, PotOperationDto, TransferAccountOptionDto } from '../api/types'
import type { FundingLineChange } from '../components/FundingLinesField'
import { getCreateFormDate, setStoredDefault } from '../utils/createFormDefaults'
import { getSaveErrorMessage, positiveAmount, todayIsoDate } from '../utils/formatters'
import { fundingLineErrorKey, fundingLinesMismatch, MAX_FUNDING_LINES, remainingAmount, type FundingLineValues } from '../utils/fundingLines'
import type { FieldErrors } from './inlineFormSlice'
import type { YearMonth } from './useMonthSelection'
import { usePots } from './usePots'

const DATE_KEY = 'potDeposit.date'
const NO_ERRORS: FieldErrors<string> = {}

export type DepositInto = 'ActivePots' | 'OnePot'

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
  errorFields: FieldErrors<string>
}

function comparableValues({ lines, ...values }: DepositValues): string {
  return JSON.stringify({ ...values, lines: lines.map(({ bankId, amount }) => ({ bankId, amount })) })
}

function validate(values: DepositValues): FieldErrors<string> {
  const errors: FieldErrors<string> = {}
  if (!values.date.trim()) errors.date = 'Enter the deposit date.'
  else if (values.date > todayIsoDate()) errors.date = 'Date must not be in the future.'
  if (values.into === 'OnePot' && !values.potId) errors.potId = 'Choose a pot.'
  if (positiveAmount(values.amount) === null) errors.amount = 'Amount must be greater than zero.'
  if (!values.description.trim()) errors.description = 'Enter a description.'
  if (values.hasNoBank) return errors
  for (const line of values.lines) {
    if (!line.bankId) errors[fundingLineErrorKey(line, 'bankId')] = 'Choose a bank.'
    if (positiveAmount(line.amount) === null) errors[fundingLineErrorKey(line, 'amount')] = 'Amount must be greater than zero.'
  }
  return errors
}

function toRequest(values: DepositValues): PotDepositRequestDto {
  return {
    amount: Number(values.amount),
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

  const update = (change: Partial<DepositValues> | ((values: DepositValues) => Partial<DepositValues>)) =>
    setForm(
      (current) =>
        current && {
          ...current,
          values: { ...current.values, ...(typeof change === 'function' ? change(current.values) : change) },
          error: null,
          errorFields: {},
        },
    )

  const setField = (field: 'potId' | 'date' | 'description', value: string) => update({ [field]: value })

  const setInto = (into: DepositInto) => update({ into })

  const setAmount = (amount: string) =>
    update(({ lines, amount: previous }) => ({
      amount,
      lines: lines.length === 1 && lines[0].amount === previous ? [{ ...lines[0], amount }] : lines,
    }))

  const setHasNoBank = (hasNoBank: boolean) => update({ hasNoBank })

  const changeLine = (key: string, change: FundingLineChange) =>
    update(({ lines }) => ({ lines: lines.map((line) => (line.key === key ? { ...line, ...change } : line)) }))

  const addLine = () =>
    update(({ lines, amount }) =>
      lines.length >= MAX_FUNDING_LINES ? {} : { lines: [...lines, newLine('', remainingAmount(lines, positiveAmount(amount)))] },
    )

  const removeLine = (key: string) => update(({ lines }) => ({ lines: lines.filter((line) => line.key !== key) }))

  const submit = () => {
    if (!form || form.isSaving || !potsAccount || mismatch) return
    const errorFields = validate(form.values)
    if (Object.keys(errorFields).length > 0) {
      setForm({ ...form, error: null, errorFields })
      return
    }

    setForm({ ...form, isSaving: true, error: null, errorFields: {} })
    const request = toRequest(form.values)
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
    setInto,
    setAmount,
    setHasNoBank,
    changeLine,
    addLine,
    removeLine,
    submit,
  }
}

export type PotDepositFormModel = ReturnType<typeof usePotDepositForm>
