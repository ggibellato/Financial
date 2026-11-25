import { formatMonthYear } from './formatters'

export function parseCreditMonth(yearMonth: string): Date {
  const [year, month] = yearMonth.split('-').map(Number)
  return new Date(year, month - 1, 1)
}

export function formatCreditMonth(yearMonth: string): string {
  return formatMonthYear(parseCreditMonth(yearMonth))
}
