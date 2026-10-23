export type AccountFormAction = 'AddExpense' | 'AddIncome' | 'TransferTo' | 'TransferFrom' | 'Reconcile'

export const ACTION_LABELS: Record<AccountFormAction, string> = {
  AddExpense: 'Add expense',
  AddIncome: 'Add income',
  TransferTo: 'Transfer to',
  TransferFrom: 'Transfer from',
  Reconcile: 'Reconcile',
}

const RECORD_NOUNS: Record<string, string> = {
  Expense: 'expense',
  CardCharge: 'card charge',
  Income: 'income',
  TransferIn: 'transfer',
  TransferOut: 'transfer',
  Adjustment: 'adjustment',
}

export function isFormAction(action: string): action is AccountFormAction {
  return Object.hasOwn(ACTION_LABELS, action)
}

export function formActionsOf(allowedActions: readonly string[]): AccountFormAction[] {
  return allowedActions.filter(isFormAction)
}

export function recordNoun(kind: string): string {
  return Object.hasOwn(RECORD_NOUNS, kind) ? RECORD_NOUNS[kind] : 'record'
}
