export const OWED_LABEL = 'Owed'

export function shownBalance(balance: number, balanceIsOwed: boolean): number {
  return balanceIsOwed && balance !== 0 ? -balance : balance
}
