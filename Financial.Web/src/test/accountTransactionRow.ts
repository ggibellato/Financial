import type { AccountTransactionDto } from '../api/types'

type RequiredRowFields = Pick<AccountTransactionDto, 'id' | 'kind' | 'date' | 'amount'>

export function accountTransactionRow(fields: RequiredRowFields & Partial<AccountTransactionDto>): AccountTransactionDto {
  return {
    currency: 'GBP',
    description: null,
    categoryName: null,
    counterpart: null,
    roundUp: null,
    cashback: null,
    canEdit: true,
    expense: null,
    income: null,
    isInterest: false,
    transfer: null,
    adjustment: null,
    externalPayment: null,
    statement: null,
    operation: null,
    movement: null,
    settlement: null,
    ...fields,
  }
}
