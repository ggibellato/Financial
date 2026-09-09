/** ISO 6166 shape: 2-letter country code + 9 alphanumeric + 1 check digit. */
const ISIN_PATTERN = /^[A-Z]{2}[A-Z0-9]{9}[0-9]$/

export function isValidIsin(value: string): boolean {
  return ISIN_PATTERN.test(value)
}
