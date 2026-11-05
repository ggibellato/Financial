import type { AccountStatementDto } from '../api/types'
import { formatDayMonthYearUtc, formatN2 } from './formatters'

const OWED_LABEL = 'Owed'

export const UNPAID_STATEMENT_STATUS = 'Unpaid'
const NOTHING_DUE_STATEMENT_STATUS = 'NothingDue'

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

export function statementStatusText(statement: AccountStatementDto): string {
  if (statement.status === UNPAID_STATEMENT_STATUS) return 'Unpaid'
  if (statement.status === NOTHING_DUE_STATEMENT_STATUS) return nothingDueText(statement.creditCarried)
  return statement.paidOn ? `Paid ${formatDayMonthYearUtc(statement.paidOn)}` : 'Paid'
}
