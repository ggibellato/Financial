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

export function formatShortDate(isoString: string | null | undefined): string {
  if (!isoString) return ''
  const d = new Date(isoString)
  if (Number.isNaN(d.getTime())) return isoString
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`
}

export function toInputDate(isoString: string): string {
  return isoString.split('T')[0]
}
