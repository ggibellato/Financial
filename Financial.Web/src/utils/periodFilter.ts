export type PeriodFilterOption =
  | 'this-month'
  | 'last-3-months'
  | 'last-6-months'
  | 'last-12-months'
  | 'ytd'
  | 'all-time'

export const DEFAULT_FILTER: PeriodFilterOption = 'last-12-months'

export const PERIOD_FILTER_OPTIONS: { value: PeriodFilterOption; label: string }[] = [
  { value: 'this-month', label: 'This month' },
  { value: 'last-3-months', label: 'Last 3 months' },
  { value: 'last-6-months', label: 'Last 6 months' },
  { value: 'last-12-months', label: 'Last 12 months' },
  { value: 'ytd', label: 'YTD' },
  { value: 'all-time', label: 'All time' },
]

export function getPeriodFilterStartDate(
  filter: PeriodFilterOption,
  referenceDate: Date = new Date(),
): Date | null {
  if (filter === 'all-time') return null

  const startOfCurrentMonth = new Date(referenceDate.getFullYear(), referenceDate.getMonth(), 1)
  switch (filter) {
    case 'this-month':
      return startOfCurrentMonth
    case 'last-3-months':
      return new Date(startOfCurrentMonth.getFullYear(), startOfCurrentMonth.getMonth() - 2, 1)
    case 'last-6-months':
      return new Date(startOfCurrentMonth.getFullYear(), startOfCurrentMonth.getMonth() - 5, 1)
    case 'last-12-months':
      return new Date(startOfCurrentMonth.getFullYear(), startOfCurrentMonth.getMonth() - 11, 1)
    case 'ytd':
      return new Date(referenceDate.getFullYear(), 0, 1)
    default:
      return null
  }
}
