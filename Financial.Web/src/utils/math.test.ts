import { describe, expect, it } from 'vitest'
import { average } from './math'

describe('average', () => {
  it('divides by the count of non-zero months when monthsElapsed is not provided', () => {
    // 400 spread over 4 non-zero months = 100, not 400 / 12 = 33.33
    const values = [100, 100, 100, 100, 0, 0, 0, 0, 0, 0, 0, 0]
    expect(average(values)).toBe(100)
  })

  it('divides by 12 when all 12 months are non-zero (matches the old fixed-12 behavior)', () => {
    const values = [100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 210]
    expect(average(values)).toBe(155)
  })

  it('returns 0 when every month is 0 and monthsElapsed is not provided', () => {
    expect(average(new Array(12).fill(0))).toBe(0)
  })

  it('divides by monthsElapsed and only sums the completed months when provided', () => {
    // Only the first 3 entries (completed months) count; the rest (in-progress/future) are ignored
    // even though they carry a non-zero value.
    const values = [100, 0, 200, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999]
    expect(average(values, 3)).toBe(100)
  })

  it('counts a zero completed month in the divisor when monthsElapsed is provided (unlike the past-year fallback)', () => {
    const values = [100, 0, 200, 0, 0, 0, 0, 0, 0, 0, 0, 0]
    // (100 + 0 + 200) / 3 completed months = 100, not / 2 non-zero months = 150
    expect(average(values, 3)).toBe(100)
  })

  it('returns 0 when monthsElapsed is 0 (no completed months yet)', () => {
    const values = [9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999, 9999]
    expect(average(values, 0)).toBe(0)
  })
})
