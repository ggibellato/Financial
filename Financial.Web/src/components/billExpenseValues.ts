import type { RecurringBillDto, RecurringBillPaymentExpenseDto } from '../api/types'

export interface BillExpenseValues {
  date: string
  description: string
  value: string
  bankId: string
  categoryId: string
}

export interface BillExpense {
  date: string
  description: string
  value: number
  bankId: string
  categoryId: string
}

export type BillExpenseField = keyof BillExpenseValues

export type BillExpenseErrors = Partial<Record<BillExpenseField, string>>

const SERVER_MESSAGE_FIELDS: [RegExp, BillExpenseField][] = [
  [/\bdate\b/i, 'date'],
  [/\bdescription\b/i, 'description'],
  [/\bvalue\b/i, 'value'],
  [/\bcategory\b/i, 'categoryId'],
]

export function billExpenseValuesFor(bill: RecurringBillDto, date: string, bankId = '', categoryId = ''): BillExpenseValues {
  return { date, description: bill.description, value: String(bill.value), bankId, categoryId }
}

export function validateBillExpense(values: BillExpenseValues): BillExpenseErrors {
  const errors: BillExpenseErrors = {}
  const value = Number(values.value)
  if (!values.date) errors.date = 'Enter a payment date.'
  if (!values.description.trim()) errors.description = 'Enter a description.'
  if (!values.value.trim() || Number.isNaN(value) || value <= 0) errors.value = 'Enter a value greater than 0.'
  if (!values.bankId) errors.bankId = 'Choose a bank.'
  if (!values.categoryId) errors.categoryId = 'Choose a category.'
  return errors
}

export function parseBillExpense(values: BillExpenseValues): BillExpense | null {
  const errors = validateBillExpense(values)
  delete errors.date
  if (Object.keys(errors).length > 0) return null
  return {
    date: values.date,
    description: values.description.trim(),
    value: Number(values.value),
    bankId: values.bankId,
    categoryId: values.categoryId,
  }
}

export function toBillPaymentExpense({ bankId, ...expense }: BillExpense): RecurringBillPaymentExpenseDto {
  return { ...expense, paymentSourceBankId: bankId }
}

export function mapBillExpenseErrorToField(message: string): BillExpenseField | null {
  return SERVER_MESSAGE_FIELDS.find(([pattern]) => pattern.test(message))?.[1] ?? null
}
