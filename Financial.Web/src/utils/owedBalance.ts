import { formatN2 } from './formatters'

const OWED_LABEL = 'Owed'

export function balanceLabel(balanceIsOwed: boolean): string {
  return balanceIsOwed ? OWED_LABEL : 'Balance'
}

export function formatShownBalance(balance: number, balanceIsOwed: boolean): string {
  return formatN2(balanceIsOwed && balance !== 0 ? -balance : balance)
}

export function formatLabelledBalance(balance: number, balanceIsOwed: boolean): string {
  const amount = formatShownBalance(balance, balanceIsOwed)
  return balanceIsOwed ? `${OWED_LABEL} ${amount}` : amount
}
