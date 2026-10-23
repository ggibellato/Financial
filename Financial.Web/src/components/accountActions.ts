export const ACTION_LABELS: Record<string, string> = {
  AddExpense: 'Add expense',
  AddIncome: 'Add income',
}

export function formActionsOf(allowedActions: readonly string[]): string[] {
  return allowedActions.filter((action) => action in ACTION_LABELS)
}
