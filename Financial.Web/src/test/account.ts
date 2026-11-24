import type { AccountDetailDto, AccountDto } from '../api/types'

type RequiredAccountFields = Pick<AccountDto, 'id' | 'name'>

type RequiredAccountDetailFields = Pick<AccountDetailDto, 'id' | 'name'>

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
    cashbackEnabled: false,
    cashbackRatePercent: null,
    roundUpDestination: null,
    cashbackDestination: null,
    categoryId: null,
    savings: null,
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
    cashbackEnabled: null,
    cashbackRatePercent: null,
    roundUpDestination: null,
    cashbackDestination: null,
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
    cashbackEnabled: null,
    cashbackRatePercent: null,
    roundUpDestination: null,
    cashbackDestination: null,
    capabilities: ['HasPots', 'ReceivesIncomeSplit', 'CanWithdraw'],
    ...fields,
  })
}

export const SAVINGS_CAPABILITIES = [
  'HasOpeningBalance',
  'ReceivesIncome',
  'CanSendTransfers',
  'CanReceiveTransfers',
  'CanReconcile',
  'EarnsInterest',
]

export function savingsAccount(fields: RequiredAccountFields & Partial<AccountDto>): AccountDto {
  return account({
    templateName: 'Savings',
    roundUpEnabled: null,
    cashbackEnabled: null,
    cashbackRatePercent: null,
    roundUpDestination: null,
    cashbackDestination: null,
    savings: {
      interestFrequency: 'Monthly',
      paymentDay: 5,
      isTaxFree: false,
      roundUpOnly: false,
      rates: [{ annualPercent: 4.5, effectiveFrom: '2026-09-01' }],
    },
    capabilities: SAVINGS_CAPABILITIES,
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
    cashbackEnabled: null,
    cashbackRatePercent: null,
    roundUpDestination: null,
    cashbackDestination: null,
    capabilities: ['HasParticipants', 'MultiCurrency', 'CanSettle'],
    ...fields,
  })
}

export function accountDetail(fields: RequiredAccountDetailFields & Partial<AccountDetailDto>): AccountDetailDto {
  return {
    templateName: 'Bank',
    currency: 'GBP',
    isActive: true,
    year: 2026,
    month: 10,
    balance: 0,
    balanceIsOwed: false,
    hasStatements: false,
    totalOwed: null,
    viewedStatement: null,
    allowedActions: [],
    disabledActions: {},
    currentBalances: [],
    hasPlannedEntries: false,
    otherParticipant: null,
    savings: null,
    roundUpDestination: null,
    cashbackDestination: null,
    ...fields,
  }
}
