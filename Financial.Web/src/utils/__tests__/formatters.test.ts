import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  currentYearMonth,
  formatDateTime,
  formatMonthInputValue,
  formatN2,
  formatN8,
  formatPercent1,
  formatPercentFraction,
  formatShortDate,
  pad,
  parseMonthInputValue,
  previousYearJanuaryFirst,
  toInputDate,
} from '../formatters'

describe('pad', () => {
  it('pad_SingleDigit_PadsWithLeadingZero', () => {
    expect(pad(5)).toBe('05')
  })

  it('pad_TwoDigits_ReturnsUnchanged', () => {
    expect(pad(15)).toBe('15')
  })
})

describe('formatN2', () => {
  it('formatN2_FormatsWithTwoDecimalsAndGrouping', () => {
    expect(formatN2(1234.5)).toBe('1,234.50')
  })
})

describe('formatN8', () => {
  it('formatN8_FormatsWithEightDecimals', () => {
    expect(formatN8(1.23456789)).toBe('1.23456789')
  })
})

describe('formatPercentFraction', () => {
  it('formatPercentFraction_MultipliesByOneHundredAndAppendsPercentSign', () => {
    expect(formatPercentFraction(0.1234)).toBe('12.34%')
  })
})

describe('formatPercent1', () => {
  it('formatPercent1_FormatsRawValueWithOneDecimalAndAppendsPercentSign', () => {
    expect(formatPercent1(12.345)).toBe('12.3%')
  })
})

describe('formatShortDate', () => {
  it('formatShortDate_NullValue_ReturnsEmptyString', () => {
    expect(formatShortDate(null)).toBe('')
  })

  it('formatShortDate_UndefinedValue_ReturnsEmptyString', () => {
    expect(formatShortDate(undefined)).toBe('')
  })

  it('formatShortDate_InvalidDateString_ReturnsOriginalString', () => {
    expect(formatShortDate('not-a-date')).toBe('not-a-date')
  })

  it('formatShortDate_ValidIsoString_ReturnsDdMmYyyy', () => {
    expect(formatShortDate('2026-07-05T12:00:00Z')).toBe('05/07/2026')
  })
})

describe('formatDateTime', () => {
  it('formatDateTime_NullValue_ReturnsEmptyString', () => {
    expect(formatDateTime(null)).toBe('')
  })

  it('formatDateTime_UndefinedValue_ReturnsEmptyString', () => {
    expect(formatDateTime(undefined)).toBe('')
  })

  it('formatDateTime_InvalidDateString_ReturnsOriginalString', () => {
    expect(formatDateTime('not-a-date')).toBe('not-a-date')
  })

  it('formatDateTime_ValidIsoString_ReturnsDdMmYyyyHhMm', () => {
    // Built from local components and round-tripped through toISOString so the
    // expectation is independent of the test runner's timezone offset.
    const localInstant = new Date(2026, 6, 5, 14, 30)

    expect(formatDateTime(localInstant.toISOString())).toBe('05/07/2026 14:30')
  })
})

describe('toInputDate', () => {
  it('toInputDate_IsoStringWithTime_ReturnsDatePortionOnly', () => {
    expect(toInputDate('2026-07-05T12:00:00Z')).toBe('2026-07-05')
  })
})

describe('currentYearMonth', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 6, 15)) // 2026-07-15
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('currentYearMonth_ReturnsCurrentYearAndOneIndexedMonth', () => {
    expect(currentYearMonth()).toEqual({ year: 2026, month: 7 })
  })
})

describe('formatMonthInputValue', () => {
  it('formatMonthInputValue_SingleDigitMonth_PadsToTwoDigits', () => {
    expect(formatMonthInputValue(2026, 7)).toBe('2026-07')
  })

  it('formatMonthInputValue_TwoDigitMonth_ReturnsUnchanged', () => {
    expect(formatMonthInputValue(2026, 12)).toBe('2026-12')
  })
})

describe('parseMonthInputValue', () => {
  it('parseMonthInputValue_ValidValue_ReturnsYearAndMonth', () => {
    expect(parseMonthInputValue('2026-07')).toEqual({ year: 2026, month: 7 })
  })

  it('parseMonthInputValue_MissingMonthPart_ReturnsNull', () => {
    expect(parseMonthInputValue('2026')).toBeNull()
  })

  it('parseMonthInputValue_NonNumericValue_ReturnsNull', () => {
    expect(parseMonthInputValue('not-a-value')).toBeNull()
  })
})

describe('previousYearJanuaryFirst', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 6, 15)) // 2026-07-15
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('previousYearJanuaryFirst_ReturnsJanuaryFirstOfPriorYear', () => {
    expect(previousYearJanuaryFirst()).toBe('2025-01-01')
  })
})
