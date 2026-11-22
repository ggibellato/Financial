export interface MonthTotalsColumn<T> {
  label: string
  value: (row: T) => number
  isTotal?: boolean
}

export function hasMonthValue<T>(row: T, columns: MonthTotalsColumn<T>[]): boolean {
  return columns.some((column) => column.value(row) !== 0)
}

export function hasMonthValues<T>(rows: T[], columns: MonthTotalsColumn<T>[]): boolean {
  return rows.some((row) => hasMonthValue(row, columns))
}
