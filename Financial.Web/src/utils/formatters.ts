export function pad(value: number): string {
  return String(value).padStart(2, '0')
}

export function formatN2(value: number): string {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

export function formatN8(value: number): string {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 8,
    maximumFractionDigits: 8,
  }).format(value)
}

export function formatPercentFraction(value: number): string {
  return new Intl.NumberFormat(undefined, {
    style: 'percent',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

export function formatPercent1(value: number): string {
  return `${new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  }).format(value)}%`
}

export function formatShortDate(isoString: string | null | undefined): string {
  if (!isoString) return ''
  const d = new Date(isoString)
  if (Number.isNaN(d.getTime())) return isoString
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`
}

export function toInputDate(isoString: string): string {
  return isoString.split('T')[0]
}

export function currentYearMonth(): { year: number; month: number } {
  const now = new Date()
  return { year: now.getFullYear(), month: now.getMonth() + 1 }
}

export function formatMonthInputValue(year: number, month: number): string {
  return `${year}-${pad(month)}`
}

export function parseMonthInputValue(value: string): { year: number; month: number } | null {
  const [yearStr, monthStr] = value.split('-')
  const year = Number(yearStr)
  const month = Number(monthStr)
  return Number.isFinite(year) && Number.isFinite(month) ? { year, month } : null
}

export function previousYearJanuaryFirst(): string {
  return `${new Date().getFullYear() - 1}-01-01`
}
