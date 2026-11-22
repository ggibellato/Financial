import { describe, expect, it } from 'vitest'
import { hasMonthValues } from '../monthTotals'

interface Row {
  month: string
  dividend: number
  total: number
}

const COLUMNS = [
  { label: 'Dividend', value: (row: Row) => row.dividend },
  { label: 'Total', value: (row: Row) => row.total },
]

describe('hasMonthValues', () => {
  it('is true when any column of any month is non-zero, negative values included', () => {
    expect(hasMonthValues([{ month: '01/2026', dividend: 0, total: 0 }, { month: '02/2026', dividend: 0, total: -3 }], COLUMNS)).toBe(true)
  })

  it('is false when every month is zero in every column, or there are no months', () => {
    expect(hasMonthValues([{ month: '01/2026', dividend: 0, total: 0 }], COLUMNS)).toBe(false)
    expect(hasMonthValues([], COLUMNS)).toBe(false)
  })
})
