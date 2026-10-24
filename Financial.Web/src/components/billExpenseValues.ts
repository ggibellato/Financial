import type { RecurringBillDto } from '../api/types'
import type { ExpensePromptValues } from '../hooks/useMensais'

export interface BillExpenseValues {
  date: string
  description: string
  value: string
  bankId: string
  categoryId: string
}

export type BillExpenseField = keyof BillExpenseValues

export function billExpenseValuesFor(bill: RecurringBillDto, date: string, bankId = ''): BillExpenseValues {
  return { date, description: bill.description, value: String(bill.value), bankId, categoryId: '' }
}

export function parseBillExpense(values: BillExpenseValues): ExpensePromptValues | null {
  const value = Number(values.value)
  const description = values.description.trim()
  const isValid =
    description.length > 0 && !Number.isNaN(value) && value > 0 && values.bankId !== '' && values.categoryId !== ''
  return isValid ? { description, value, date: values.date, bankId: values.bankId, categoryId: values.categoryId } : null
}
