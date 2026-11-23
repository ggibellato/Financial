export type LinkedAccountSource = 'PotsAccountTotal' | 'SavingsAccountBalance'

interface LinkedAccount {
  idField: 'potsAccountId' | 'savingsAccountId'
  capability: string
  label: string
  snapshotValue: string
}

export const LINKED_ACCOUNT_SOURCES: Record<LinkedAccountSource, LinkedAccount> = {
  PotsAccountTotal: { idField: 'potsAccountId', capability: 'HasPots', label: 'Pots account', snapshotValue: 'total' },
  SavingsAccountBalance: {
    idField: 'savingsAccountId',
    capability: 'EarnsInterest',
    label: 'Savings account',
    snapshotValue: 'balance',
  },
}

export function isLinkedAccountSource(source: string): source is LinkedAccountSource {
  return Object.hasOwn(LINKED_ACCOUNT_SOURCES, source)
}
