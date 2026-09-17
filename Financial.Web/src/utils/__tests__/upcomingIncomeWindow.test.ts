import { describe, expect, it } from 'vitest'
import {
  DEFAULT_UPCOMING_INCOME_WINDOW,
  UPCOMING_INCOME_WINDOW_OPTIONS,
  isWithinUpcomingIncomeWindow,
} from '../upcomingIncomeWindow'
import type { UpcomingIncomeWindowDays } from '../upcomingIncomeWindow'

const REFERENCE = new Date(2026, 8, 17)

function daysFromReference(days: number): string {
  const date = new Date(REFERENCE)
  date.setDate(date.getDate() + days)
  return date.toISOString()
}

describe('upcomingIncomeWindow', () => {
  it('offers_the_three_day_count_windows_with_ninety_days_as_the_default', () => {
    expect(UPCOMING_INCOME_WINDOW_OPTIONS).toEqual([
      { value: 30, label: '30 days' },
      { value: 90, label: '90 days' },
      { value: 180, label: '180 days' },
    ])
    expect(DEFAULT_UPCOMING_INCOME_WINDOW).toBe(90)
  })

  it.each<UpcomingIncomeWindowDays>([30, 90, 180])(
    'includes_a_projection_exactly_on_the_%s_day_boundary',
    (windowDays) => {
      expect(isWithinUpcomingIncomeWindow(daysFromReference(windowDays), windowDays, REFERENCE)).toBe(true)
    },
  )

  it.each<UpcomingIncomeWindowDays>([30, 90, 180])(
    'excludes_a_projection_one_day_past_the_%s_day_boundary',
    (windowDays) => {
      expect(isWithinUpcomingIncomeWindow(daysFromReference(windowDays + 1), windowDays, REFERENCE)).toBe(false)
    },
  )

  it.each<UpcomingIncomeWindowDays>([30, 90, 180])('includes_a_past_projection_in_the_%s_day_window', (windowDays) => {
    expect(isWithinUpcomingIncomeWindow(daysFromReference(-45), windowDays, REFERENCE)).toBe(true)
  })

  it('excludes_a_projection_the_shortest_window_misses_but_a_longer_one_keeps', () => {
    const inNinetyDays = daysFromReference(60)

    expect(isWithinUpcomingIncomeWindow(inNinetyDays, 30, REFERENCE)).toBe(false)
    expect(isWithinUpcomingIncomeWindow(inNinetyDays, 90, REFERENCE)).toBe(true)
    expect(isWithinUpcomingIncomeWindow(inNinetyDays, 180, REFERENCE)).toBe(true)
  })

  it('excludes_an_unparseable_date_instead_of_throwing', () => {
    expect(isWithinUpcomingIncomeWindow('not-a-date', 90, REFERENCE)).toBe(false)
  })
})
