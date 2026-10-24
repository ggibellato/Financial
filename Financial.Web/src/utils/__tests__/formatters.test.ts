import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  currentYearMonth,
  formatDateTime,
  formatLongMonthYear,
  formatMonthInputValue,
  formatN2,
  formatN2OrDash,
  formatN8,
  formatPercent1,
  formatPercent1OrDash,
  formatPercentFraction,
  formatPercentFractionOrDash,
  formatShortDate,
  formatSignedN2,
  formatCurrency,
  getDeleteErrorMessage,
  getSaveErrorMessage,
  lastDayOfMonthIsoDate,
  pad,
  parseMonthInputValue,
  previousYearJanuaryFirst,
  toInputDate,
  todayIsoDate,
} from '../formatters'
import { ApiError } from '../../api/apiError'
import { pinDate } from '../../test-utils/pinDate'

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

describe('formatSignedN2', () => {
  it('formatSignedN2_Positive_PrefixesPlusSign', () => {
    expect(formatSignedN2(1234.5)).toBe('+1,234.50')
  })

  it('formatSignedN2_Negative_PrefixesMinusSign', () => {
    expect(formatSignedN2(-42.1)).toBe('-42.10')
  })

  it('formatSignedN2_Zero_HasNoSign', () => {
    expect(formatSignedN2(0)).toBe('0.00')
  })
})

describe('formatLongMonthYear', () => {
  it('formatLongMonthYear_YearAndMonth_ReturnsFullMonthNameAndYear', () => {
    expect(formatLongMonthYear(2026, 10)).toBe('October 2026')
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

describe('formatN2OrDash', () => {
  it('formatN2OrDash_Number_FormatsWithTwoDecimals', () => {
    expect(formatN2OrDash(1234.5)).toBe('1,234.50')
  })

  it('formatN2OrDash_Zero_FormatsRatherThanDashes', () => {
    expect(formatN2OrDash(0)).toBe('0.00')
  })

  it('formatN2OrDash_Null_ReturnsEmDash', () => {
    expect(formatN2OrDash(null)).toBe('—')
  })

  it('formatN2OrDash_Undefined_ReturnsEmDash', () => {
    expect(formatN2OrDash(undefined)).toBe('—')
  })
})

describe('formatPercentFractionOrDash', () => {
  it('formatPercentFractionOrDash_Number_FormatsAsPercent', () => {
    expect(formatPercentFractionOrDash(0.1234)).toBe('12.34%')
  })

  it('formatPercentFractionOrDash_Null_ReturnsEmDash', () => {
    expect(formatPercentFractionOrDash(null)).toBe('—')
  })

  it('formatPercentFractionOrDash_Undefined_ReturnsEmDash', () => {
    expect(formatPercentFractionOrDash(undefined)).toBe('—')
  })
})

describe('formatPercent1OrDash', () => {
  it('formatPercent1OrDash_Number_FormatsWithOneDecimalAndPercentSign', () => {
    expect(formatPercent1OrDash(12.345)).toBe('12.3%')
  })

  it('formatPercent1OrDash_Null_ReturnsEmDash', () => {
    expect(formatPercent1OrDash(null)).toBe('—')
  })

  it('formatPercent1OrDash_Undefined_ReturnsEmDash', () => {
    expect(formatPercent1OrDash(undefined)).toBe('—')
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

describe('todayIsoDate', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it.each([
    ['2026-07-01T00:30:00+01:00', '2026-07-01'],
    ['2026-01-31T23:59:00Z', '2026-01-31'],
    ['2026-03-01T00:30:00Z', '2026-03-01'],
  ])('todayIsoDate_At%s_ReturnsTheLocalCalendarDate', (instant, expected) => {
    pinDate(instant)

    expect(todayIsoDate()).toBe(expected)
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

  it('parseMonthInputValue_MonthOutOfRange_ReturnsNull', () => {
    expect(parseMonthInputValue('2026-13')).toBeNull()
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

describe('number formatters follow the host locale', () => {
  async function importFormattersFor(locale: string) {
    vi.resetModules()
    const RealNumberFormat = Intl.NumberFormat
    vi.spyOn(Intl, 'NumberFormat').mockImplementation(function (
      _locale?: string | string[],
      options?: Intl.NumberFormatOptions,
    ) {
      return new RealNumberFormat(locale, options)
    } as unknown as typeof Intl.NumberFormat)
    return import('../formatters')
  }

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('formatN2_EnGb_UsesCommaGroupingAndDotDecimal', async () => {
    const { formatN2 } = await importFormattersFor('en-GB')
    expect(formatN2(1234.5)).toBe('1,234.50')
  })

  it('formatN2_PtBr_UsesDotGroupingAndCommaDecimal', async () => {
    const { formatN2 } = await importFormattersFor('pt-BR')
    expect(formatN2(1234.5)).toBe('1.234,50')
  })

  it('formatPercent1_PtBr_UsesCommaDecimal', async () => {
    const { formatPercent1 } = await importFormattersFor('pt-BR')
    expect(formatPercent1(12.5)).toBe('12,5%')
  })
})

describe('getSaveErrorMessage', () => {
  it('getSaveErrorMessage_NetworkFailure_ReturnsConnectionMessage', () => {
    expect(getSaveErrorMessage(new ApiError('Failed to fetch', 0), 'Fallback')).toBe('Could not save. Check the connection and try again.')
  })

  it('getSaveErrorMessage_TypeErrorFromCode_ReturnsItsMessage', () => {
    expect(getSaveErrorMessage(new TypeError('x is undefined'), 'Fallback')).toBe('x is undefined')
  })

  it('getSaveErrorMessage_ServerError_ReturnsItsMessage', () => {
    expect(getSaveErrorMessage(new Error('Category is inactive'), 'Fallback')).toBe('Category is inactive')
  })

  it('getSaveErrorMessage_NonError_ReturnsFallback', () => {
    expect(getSaveErrorMessage('boom', 'Fallback')).toBe('Fallback')
  })
})

describe('getDeleteErrorMessage', () => {
  it('names the connection for a network failure and keeps the server message otherwise', () => {
    expect(getDeleteErrorMessage(new ApiError('Failed to fetch', 0), 'Failed to delete')).toBe(
      'Could not delete. Check the connection and try again.',
    )
    expect(getDeleteErrorMessage(new ApiError('Income not found.', 404), 'Failed to delete')).toBe('Income not found.')
    expect(getDeleteErrorMessage('boom', 'Failed to delete')).toBe('Failed to delete')
  })
})

describe('formatCurrency', () => {
  it('formats an amount in its own currency, with the sign before the symbol', () => {
    expect(formatCurrency(2344.37, 'GBP')).toBe('£2,344.37')
    expect(formatCurrency(-10.25, 'GBP')).toBe('-£10.25')
    expect(formatCurrency(5, 'EUR')).toBe('€5.00')
  })
})

describe('lastDayOfMonthIsoDate', () => {
  it.each([
    [2026, 9, '2026-09-30'],
    [2024, 2, '2024-02-29'],
    [2025, 12, '2025-12-31'],
  ])('gives the last day of %i-%i', (year, month, expected) => {
    expect(lastDayOfMonthIsoDate(year, month)).toBe(expected)
  })
})
