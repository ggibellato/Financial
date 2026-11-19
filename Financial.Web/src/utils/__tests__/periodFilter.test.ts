import { afterEach, describe, expect, it } from 'vitest'
import { PERIOD_FILTER_OPTIONS, getPeriodFilterStartDate, isOnOrAfterPeriodStart } from '../periodFilter'

describe('getPeriodFilterStartDate', () => {
  const referenceDate = new Date(2026, 6, 15) // 2026-07-15

  it('getPeriodFilterStartDate_ThisMonth_ReturnsFirstDayOfCurrentMonth', () => {
    const result = getPeriodFilterStartDate('this-month', referenceDate)
    expect(result).toEqual(new Date(2026, 6, 1))
  })

  it('getPeriodFilterStartDate_Last3Months_ReturnsRollingWindow', () => {
    const result = getPeriodFilterStartDate('last-3-months', referenceDate)
    expect(result).toEqual(new Date(2026, 4, 1))
  })

  it('getPeriodFilterStartDate_Last6Months_ReturnsRollingWindow', () => {
    const result = getPeriodFilterStartDate('last-6-months', referenceDate)
    expect(result).toEqual(new Date(2026, 1, 1))
  })

  it('getPeriodFilterStartDate_Last12Months_ReturnsRollingWindow', () => {
    const result = getPeriodFilterStartDate('last-12-months', referenceDate)
    expect(result).toEqual(new Date(2025, 7, 1))
  })

  it('getPeriodFilterStartDate_Ytd_ReturnsJanuaryFirstOfCurrentYear', () => {
    const result = getPeriodFilterStartDate('ytd', referenceDate)
    expect(result).toEqual(new Date(2026, 0, 1))
  })

  it('getPeriodFilterStartDate_Ytd_WhenReferenceDateIsJanuary_ReturnsSameMonth', () => {
    const result = getPeriodFilterStartDate('ytd', new Date(2026, 0, 15))
    expect(result).toEqual(new Date(2026, 0, 1))
  })

  it('getPeriodFilterStartDate_AllTime_ReturnsNull', () => {
    const result = getPeriodFilterStartDate('all-time', referenceDate)
    expect(result).toBeNull()
  })
})

describe('isOnOrAfterPeriodStart', () => {
  const testTimeZone = process.env.TZ

  afterEach(() => {
    process.env.TZ = testTimeZone
  })

  it.each(['Europe/London', 'America/Sao_Paulo', 'Asia/Tokyo'])('keeps a date on the first day of the period and drops the day before in %s', (timeZone) => {
    process.env.TZ = timeZone
    const start = new Date(2026, 6, 1)

    expect(isOnOrAfterPeriodStart('2026-07-01', start)).toBe(true)
    expect(isOnOrAfterPeriodStart('2026-07-01T00:00:00', start)).toBe(true)
    expect(isOnOrAfterPeriodStart('2026-06-30', start)).toBe(false)
    expect(isOnOrAfterPeriodStart('2026-06-30T23:59:59', start)).toBe(false)
  })

  it('keeps every date when there is no period start', () => {
    expect(isOnOrAfterPeriodStart('1990-01-01', null)).toBe(true)
  })
})

describe('PERIOD_FILTER_OPTIONS', () => {
  it('PERIOD_FILTER_OPTIONS_HasExactlySixOptionsInOrder', () => {
    expect(PERIOD_FILTER_OPTIONS.map((o) => o.value)).toEqual([
      'this-month',
      'last-3-months',
      'last-6-months',
      'last-12-months',
      'ytd',
      'all-time',
    ])
  })
})
