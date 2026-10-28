import type { AccountDto } from '../api/types'

type RequiredAccountFields = Pick<AccountDto, 'id' | 'name'>

const BANK_CAPABILITIES = [
  'HasOpeningBalance',
  'ReceivesIncome',
  'HasCategories',
  'CanSendTransfers',
  'CanReceiveTransfers',
  'CanReconcile',
  'CanPayBills',
  'SupportsRoundUp',
]

export function account(fields: RequiredAccountFields & Partial<AccountDto>): AccountDto {
  return {
    templateName: 'Bank',
    currency: 'GBP',
    isActive: true,
    hasRecords: false,
    openingBalance: 0,
    balanceStartDate: '2026-01-01',
    roundUpEnabled: false,
    categoryId: null,
    capabilities: BANK_CAPABILITIES,
    ...fields,
  }
}

export function cardAccount(fields: RequiredAccountFields & Partial<AccountDto>): AccountDto {
  return account({
    templateName: 'CreditCard',
    openingBalance: null,
    balanceStartDate: null,
    roundUpEnabled: null,
    capabilities: ['HasCategories', 'HasStatements', 'ReceivesPayments'],
    ...fields,
  })
}

export function potsAccount(fields: RequiredAccountFields & Partial<AccountDto>): AccountDto {
  return account({
    templateName: 'Pots',
    openingBalance: null,
    balanceStartDate: null,
    roundUpEnabled: null,
    capabilities: ['HasPots', 'ReceivesIncomeSplit', 'CanWithdraw'],
    ...fields,
  })
}
