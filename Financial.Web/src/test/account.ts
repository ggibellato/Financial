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
    currencies: [fields.currency ?? 'GBP'],
    otherParticipant: null,
    isActive: true,
    hasRecords: false,
    openingBalance: 0,
    balanceStartDate: '2026-01-01',
    roundUpEnabled: false,
    categoryId: null,
    dueDate: null,
    replacesPage: null,
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

export function settlementAccount(fields: RequiredAccountFields & Partial<AccountDto>): AccountDto {
  return account({
    templateName: 'Settlement',
    currency: 'BRL',
    currencies: ['BRL', 'GBP'],
    otherParticipant: fields.name,
    openingBalance: null,
    balanceStartDate: null,
    roundUpEnabled: null,
    capabilities: ['HasParticipants', 'MultiCurrency', 'CanSettle'],
    ...fields,
  })
}
