export type UpcomingIncomeWindowDays = 30 | 90 | 180

export const DEFAULT_UPCOMING_INCOME_WINDOW: UpcomingIncomeWindowDays = 90

export const UPCOMING_INCOME_WINDOW_OPTIONS: { value: UpcomingIncomeWindowDays; label: string }[] = [
  { value: 30, label: '30 days' },
  { value: 90, label: '90 days' },
  { value: 180, label: '180 days' },
]

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate())
}

// A projection already in the past stays visible in every window: the backend never
// drops it, and hiding it would silently lose an overdue payment.
export function isWithinUpcomingIncomeWindow(
  projectedNextDate: string,
  windowDays: UpcomingIncomeWindowDays,
  referenceDate: Date = new Date(),
): boolean {
  const projected = new Date(projectedNextDate)
  if (Number.isNaN(projected.getTime())) return false

  const limit = startOfDay(referenceDate)
  limit.setDate(limit.getDate() + windowDays)

  return startOfDay(projected).getTime() <= limit.getTime()
}
