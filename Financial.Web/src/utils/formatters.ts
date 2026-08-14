export function pad(value: number): string {
  return String(value).padStart(2, '0')
}

const n2Formatter = new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const n8Formatter = new Intl.NumberFormat(undefined, { minimumFractionDigits: 8, maximumFractionDigits: 8 })
const percentFractionFormatter = new Intl.NumberFormat(undefined, {
  style: 'percent',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})
const percent1Formatter = new Intl.NumberFormat(undefined, { minimumFractionDigits: 1, maximumFractionDigits: 1 })

export function formatN2(value: number): string {
  return n2Formatter.format(value)
}

export function formatN8(value: number): string {
  return n8Formatter.format(value)
}

export function formatPercentFraction(value: number): string {
  return percentFractionFormatter.format(value)
}

export function formatPercent1(value: number): string {
  return `${percent1Formatter.format(value)}%`
}

export function formatShortDate(isoString: string | null | undefined): string {
  if (!isoString) return ''
  const d = new Date(isoString)
  if (Number.isNaN(d.getTime())) return isoString
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`
}

/** Like {@link formatShortDate}, but reads UTC date parts (safe for date-only strings). */
export function formatShortDateUtc(isoString: string | null | undefined): string {
  if (!isoString) return ''
  const d = new Date(isoString)
  if (Number.isNaN(d.getTime())) return isoString
  return `${pad(d.getUTCDate())}/${pad(d.getUTCMonth() + 1)}/${d.getUTCFullYear()}`
}

/** Like {@link formatShortDate}, but also includes the local time as `HH:mm`. */
export function formatDateTime(isoString: string | null | undefined): string {
  if (!isoString) return ''
  const d = new Date(isoString)
  if (Number.isNaN(d.getTime())) return isoString
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`
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

export function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10)
}

export function formatMonthYear(date: Date): string {
  return date.toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })
}

/** CSS modifier class for a signed value, e.g. signClass(-5, 'asset-summary__value') -> 'asset-summary__value--red'. */
export function signClass(value: number, basePrefix: string): string {
  return `${basePrefix}--${value >= 0 ? 'green' : 'red'}`
}

/** Extracts a caught value's message, falling back to a caller-supplied default for a non-Error throw (e.g. a rejected fetch). */
export function getErrorMessage(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback
}
