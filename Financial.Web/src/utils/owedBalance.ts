import { formatN2 } from './formatters'

const OWED_LABEL = 'Owed'

export const UNPAID_STATEMENT_STATUS = 'Unpaid'

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

export function formatOwedOrCredit(totalOwed: number): string {
  if (totalOwed < 0) return `Credit ${formatN2(-totalOwed)}`
  return formatN2(totalOwed === 0 ? 0 : totalOwed)
}

export function nothingDueText(creditCarried: number): string {
  return creditCarried > 0 ? `Nothing due · ${formatN2(creditCarried)} credit carried` : 'Nothing due'
}
