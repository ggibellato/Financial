import type { AccountDto } from '../api/types'

type RequiredAccountFields = Pick<AccountDto, 'id' | 'name'>

export function account(fields: RequiredAccountFields & Partial<AccountDto>): AccountDto {
  return {
    templateName: 'Bank',
    currency: 'GBP',
    isActive: true,
    hasRecords: false,
    openingBalance: 0,
    balanceStartDate: '2026-01-01',
    roundUpEnabled: false,
    ...fields,
  }
}
