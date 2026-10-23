export type AccountFormAction = 'AddExpense' | 'AddIncome'

export const ACTION_LABELS: Record<AccountFormAction, string> = {
  AddExpense: 'Add expense',
  AddIncome: 'Add income',
}

export function isFormAction(action: string): action is AccountFormAction {
  return Object.hasOwn(ACTION_LABELS, action)
}

export function formActionsOf(allowedActions: readonly string[]): AccountFormAction[] {
  return allowedActions.filter(isFormAction)
}
