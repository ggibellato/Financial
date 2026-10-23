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
const signedN2Formatter = new Intl.NumberFormat(undefined, {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
  signDisplay: 'exceptZero',
})
const percent1Formatter = new Intl.NumberFormat(undefined, { minimumFractionDigits: 1, maximumFractionDigits: 1 })

export function formatN2(value: number): string {
  return n2Formatter.format(value)
}

export function formatSignedN2(value: number): string {
  return signedN2Formatter.format(value)
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

const MISSING_VALUE_PLACEHOLDER = '—'

function formatOrDash(value: number | null | undefined, format: (value: number) => string): string {
  return value == null ? MISSING_VALUE_PLACEHOLDER : format(value)
}

export function formatN2OrDash(value: number | null | undefined): string {
  return formatOrDash(value, formatN2)
}

export function formatPercentFractionOrDash(value: number | null | undefined): string {
  return formatOrDash(value, formatPercentFraction)
}

export function formatPercent1OrDash(value: number | null | undefined): string {
  return formatOrDash(value, formatPercent1)
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

const MONTH_INPUT_PATTERN = /^(\d{4})-(0[1-9]|1[0-2])$/

export function parseMonthInputValue(value: string): { year: number; month: number } | null {
  const match = MONTH_INPUT_PATTERN.exec(value)
  return match ? { year: Number(match[1]), month: Number(match[2]) } : null
}

export function previousYearJanuaryFirst(): string {
  return `${new Date().getFullYear() - 1}-01-01`
}

export function todayIsoDate(): string {
  const now = new Date()
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

export function formatMonthYear(date: Date): string {
  return date.toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })
}

export function formatLongMonthYear(year: number, month: number): string {
  return new Date(year, month - 1, 1).toLocaleDateString('en-GB', { month: 'long', year: 'numeric' })
}

// Monthly chart axis label, shared by every "value by month" chart
// (Transactions/Credits) — docs/ui/forms-data-and-visualisations.md's
// "Month-axis labels" rule: MM/yyyy everywhere, not a short month name.
export function formatMonthKey(date: Date): string {
  return `${pad(date.getMonth() + 1)}/${date.getFullYear()}`
}

export function signClass(value: number, basePrefix: string): string {
  return `${basePrefix}--${value >= 0 ? 'green' : 'red'}`
}

export function getErrorMessage(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback
}

export const NETWORK_SAVE_ERROR = 'Could not save. Check the connection and try again.'

export function getSaveErrorMessage(err: unknown, fallback: string): string {
  return err instanceof TypeError ? NETWORK_SAVE_ERROR : getErrorMessage(err, fallback)
}

export function parseValidatedNumber(value: string, options: { min?: number; max?: number } = {}): number | null {
  const parsed = Number(value)
  if (!value.trim() || !isFinite(parsed)) return null
  if (options.min !== undefined && parsed < options.min) return null
  if (options.max !== undefined && parsed > options.max) return null
  return parsed
}
