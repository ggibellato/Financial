import type { AccountTransactionDto } from '../api/types'

export type AccountFormAction = 'AddExpense' | 'AddIncome' | 'TransferTo' | 'TransferFrom' | 'Reconcile'

export const ACTION_LABELS: Record<AccountFormAction, string> = {
  AddExpense: 'Add expense',
  AddIncome: 'Add income',
  TransferTo: 'Transfer to',
  TransferFrom: 'Transfer from',
  Reconcile: 'Adjust balance',
}

const TRANSFER_ACTIONS: readonly AccountFormAction[] = ['TransferTo', 'TransferFrom']

const KIND_LABELS: Record<string, string> = {
  CardCharge: 'Card charge',
  TransferIn: 'Transfer in',
  TransferOut: 'Transfer out',
  Adjustment: 'Balance adjustment',
}

const RECORD_NOUNS: Record<string, string> = {
  Expense: 'expense',
  CardCharge: 'card charge',
  Income: 'income',
  TransferIn: 'transfer',
  TransferOut: 'transfer',
  Adjustment: 'balance adjustment',
}

export function isFormAction(action: string): action is AccountFormAction {
  return Object.hasOwn(ACTION_LABELS, action)
}

export function formActionsOf(allowedActions: readonly string[]): AccountFormAction[] {
  return allowedActions.filter(isFormAction)
}

export function isTransferAction(action: AccountFormAction): boolean {
  return TRANSFER_ACTIONS.includes(action)
}

export function canTransfer(allowedActions: readonly string[]): boolean {
  return formActionsOf(allowedActions).some(isTransferAction)
}

export function kindLabel(kind: string): string {
  return Object.hasOwn(KIND_LABELS, kind) ? KIND_LABELS[kind] : kind
}

export function recordNoun(kind: string): string {
  return Object.hasOwn(RECORD_NOUNS, kind) ? RECORD_NOUNS[kind] : 'record'
}

export function recordTitle(kind: string): string {
  const noun = recordNoun(kind)
  return noun.charAt(0).toUpperCase() + noun.slice(1)
}

export function rowKey(row: AccountTransactionDto): string {
  return `${row.kind}-${row.id}`
}
