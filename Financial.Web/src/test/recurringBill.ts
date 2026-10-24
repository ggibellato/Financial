import type { RecurringBillDto } from '../api/types'

type RequiredBillFields = Pick<RecurringBillDto, 'id' | 'dueDay' | 'description' | 'value'>

export function recurringBill(fields: RequiredBillFields & Partial<RecurringBillDto>): RecurringBillDto {
  return { area: 'UK', note: '', status: 'Unset', minimumWageValue: null, nitNumber: null, ...fields }
}
