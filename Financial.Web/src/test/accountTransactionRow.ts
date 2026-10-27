import type { AccountTransactionDto } from '../api/types'

type RequiredRowFields = Pick<AccountTransactionDto, 'id' | 'kind' | 'date' | 'amount'>

export function accountTransactionRow(fields: RequiredRowFields & Partial<AccountTransactionDto>): AccountTransactionDto {
  return {
    description: null,
    categoryName: null,
    counterpart: null,
    roundUp: null,
    canEdit: true,
    expense: null,
    income: null,
    transfer: null,
    adjustment: null,
    statement: null,
    ...fields,
  }
}
