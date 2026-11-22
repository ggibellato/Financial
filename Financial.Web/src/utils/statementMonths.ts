import type { AccountStatementDto } from '../api/types'
import { formatLongMonthYear, formatMonthInputValue, formatStatementMonthLabel, parseMonthInputValue } from './formatters'
import { isClosedStatement } from './owedBalance'

export interface StatementMonthOption {
  value: string
  label: string
}

export interface UnpaidStatementMonth extends StatementMonthOption {
  isStored: boolean
}

export function unpaidStatementMonths(
  start: { year: number; month: number },
  firstOffset: number,
  count: number,
  statements: readonly AccountStatementDto[],
): UnpaidStatementMonth[] {
  const months: UnpaidStatementMonth[] = []
  for (let offset = firstOffset; offset < firstOffset + count; offset++) {
    const monthIndex = start.month - 1 + offset
    const year = start.year + Math.floor(monthIndex / 12)
    const month = (monthIndex % 12) + 1
    const statement = statements.find((candidate) => candidate.year === year && candidate.month === month)
    if (statement && isClosedStatement(statement)) continue
    months.push({ value: formatMonthInputValue(year, month), label: formatLongMonthYear(year, month), isStored: statement?.id != null })
  }
  return months
}

const MONTHS_OFFERED = 4

export function statementMonthOptions(date: string, statements: readonly AccountStatementDto[], keptMonth: string): StatementMonthOption[] {
  const start = parseMonthInputValue(date.slice(0, 7))
  if (!start) return []
  const months: StatementMonthOption[] = unpaidStatementMonths(start, 0, MONTHS_OFFERED, statements).map(({ value, label }) => ({ value, label }))
  if (!keptMonth || months.some((month) => month.value === keptMonth)) return months
  return [...months, { value: keptMonth, label: formatStatementMonthLabel(keptMonth) }].sort((a, b) => a.value.localeCompare(b.value))
}
